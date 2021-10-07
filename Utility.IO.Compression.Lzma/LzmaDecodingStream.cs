using System;
using Utility.IO.Compression.Lzma.RangeCoder;

namespace Utility.IO.Compression.Lzma
{
    public class LzmaDecodingStream
        : IBasicInputByteStream, IOutputBuffer
    {
        private const bool _solid = false;

        private Lz.OutWindow _outWindow;
        private RangeCoder.RangeDecoder _rangeDecoder;

        private BitDecoder[] _isMatchDecoders;
        private BitDecoder[] _isRepDecoders;
        private BitDecoder[] _isRepG0Decoders;
        private BitDecoder[] _isRepG1Decoders;
        private BitDecoder[] _isRepG2Decoders;
        private BitDecoder[] _isRep0LongDecoders;

        private BitTreeDecoder[] _posSlotDecoder;
        private BitDecoder[] _posDecoders;

        private BitTreeDecoder _posAlignDecoder;

        private LzmaLenDecoder _lenDecoder;
        private LzmaLenDecoder _repLenDecoder;

        private LzmaLiteralDecoder _literalDecoder;

        private UInt32 _dictionarySize;
        private UInt32 _dictionarySizeCheck;

        private UInt32 _posStateMask;

        private LzmaCoder.State _block_state;
        private UInt32 _block_rep0;
        private UInt32 _block_rep1;
        private UInt32 _block_rep2;
        private UInt32 _block_rep3;
        private UInt64 _block_nowPos64;

        private bool _isDisposed;
        private IBasicInputByteStream _baseSream;
        private UInt64 _size;
        private ICodingProgressReportable _progressReporter;
        private bool _leaveOpen;
        private bool _isFirstBlock;
        private byte[] _outputBuffer;
        private int _outputBufferSize;
        private int _outputBufferIndex;
        private bool _isEndOfDecoding;
        private bool _isEndOfStream;

        public LzmaDecodingStream(IBasicInputByteStream baseSream, IReadOnlyArray<byte> properties, UInt64 size, bool leaveOpen)
            : this(baseSream, properties, size, null, leaveOpen)
        {
        }

        public LzmaDecodingStream(IBasicInputByteStream baseSream, IReadOnlyArray<byte> properties, UInt64 size, ICodingProgressReportable progressReporter, bool leaveOpen)
        {
            _outWindow = new Lz.OutWindow();
            _rangeDecoder = new RangeCoder.RangeDecoder();
            _isMatchDecoders = new BitDecoder[LzmaCoder.kNumStates << LzmaCoder.kNumPosStatesBitsMax];
            _isRepDecoders = new BitDecoder[LzmaCoder.kNumStates];
            _isRepG0Decoders = new BitDecoder[LzmaCoder.kNumStates];
            _isRepG1Decoders = new BitDecoder[LzmaCoder.kNumStates];
            _isRepG2Decoders = new BitDecoder[LzmaCoder.kNumStates];
            _isRep0LongDecoders = new BitDecoder[LzmaCoder.kNumStates << LzmaCoder.kNumPosStatesBitsMax];
            _posSlotDecoder = new BitTreeDecoder[LzmaCoder.kNumLenToPosStates];
            _posDecoders = new BitDecoder[LzmaCoder.kNumFullDistances - LzmaCoder.kEndPosModelIndex];
            _posAlignDecoder = new BitTreeDecoder(LzmaCoder.kNumAlignBits);
            _lenDecoder = new LzmaLenDecoder();
            _repLenDecoder = new LzmaLenDecoder();
            _literalDecoder = new LzmaLiteralDecoder();
            _dictionarySizeCheck = 0;
            _posStateMask = 0;
            _dictionarySize = UInt32.MaxValue;
            for (var i = 0; i < LzmaCoder.kNumLenToPosStates; i++)
                _posSlotDecoder[i] = new BitTreeDecoder(LzmaCoder.kNumPosSlotBits);

            _isDisposed = false;
            _baseSream = baseSream;
            _size = size;
            _progressReporter = progressReporter;
            _leaveOpen = leaveOpen;
            _isFirstBlock = true;
            _outputBuffer = null;
            _outputBufferSize = 0;
            _outputBufferIndex = 0;
            _isEndOfDecoding = false;
            _isEndOfStream = false;
            SetDecoderProperties(properties);
            Init(baseSream, this);
            _block_state = new LzmaCoder.State();
            _block_state.Init();
            _block_rep0 = 0;
            _block_rep1 = 0;
            _block_rep2 = 0;
            _block_rep3 = 0;
            _block_nowPos64 = 0;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > buffer.Length)
                throw new ArgumentException();

            if (_isEndOfStream)
                return 0;
            if (_isFirstBlock)
            {
                _outputBuffer = new byte[_outWindow.BlockSize];
                _outputBufferSize = 0;
                _outputBufferIndex = 0;
                if (_block_nowPos64 < _size)
                {
                    if (_isMatchDecoders[_block_state.Index << LzmaCoder.kNumPosStatesBitsMax].Decode(_rangeDecoder))
                        throw new DataErrorException();
                    _block_state.UpdateChar();
                    var b = _literalDecoder.DecodeNormal(_rangeDecoder, 0, 0);
                    _outWindow.PutByte(b);
                    _block_nowPos64++;
                }
                _isFirstBlock = false;
            }
            if (_outputBufferIndex >= _outputBufferSize)
                DecodeBlock();
            if (_outputBufferIndex >= _outputBufferSize)
            {
                _isEndOfStream = true;
                return 0;
            }
            var actualCount = count.Minimum(_outputBufferSize - _outputBufferIndex);
            _outputBuffer.CopyTo(_outputBufferIndex, buffer, offset, actualCount);
            _outputBufferIndex += actualCount;
            if (_outputBufferIndex >= _outputBufferSize)
            {
                _outputBufferIndex = 0;
                _outputBufferSize = 0;
            }
            return actualCount;
        }

        void IOutputBuffer.Write(IReadOnlyArray<byte> buffer, int offset, int count)
        {
            var requiredBufferSize = _outputBufferSize + count;
            if (requiredBufferSize > _outputBuffer.Length)
                Array.Resize(ref _outputBuffer, requiredBufferSize);
            buffer.CopyTo(offset, _outputBuffer, _outputBufferSize, count);
            _outputBufferSize += count;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_baseSream != null)
                    {
                        if (_leaveOpen == false)
                            _baseSream.Dispose();
                        _baseSream = null;
                    }
                }
                _isDisposed = true;
            }
        }

        private void SetDecoderProperties(IReadOnlyArray<byte> properties)
        {
            if (properties.Length < 5)
                throw new ArgumentException();
            var lc = properties[0] % 9;
            var remainder = properties[0] / 9;
            var lp = remainder % 5;
            var pb = remainder / 5;
            if (pb > LzmaCoder.kNumPosStatesBitsMax)
                throw new ArgumentException();
            var dictionarySize = properties.ToUInt32LE(1);
            SetDictionarySize(dictionarySize);
            SetLiteralProperties(lp, lc);
            SetPosBitsProperties(pb);
        }

        private void DecodeBlock()
        {
            while (!_isEndOfDecoding && _outputBufferIndex >= _outputBufferSize)
            {
                if (!Decode())
                {
                    _outWindow.Flush();
                    _outWindow.ReleaseStream();
                    _rangeDecoder.ReleaseStream();
                    _isEndOfDecoding = true;
                }
            }
            if (_progressReporter != null && _outputBufferIndex < _outputBufferSize)
            {
                try
                {
                    _progressReporter.SetProgress((UInt32)_outputBufferSize - (UInt32)_outputBufferIndex);
                }
                catch (Exception)
                {
                }
            }
        }

        private bool Decode()
        {
            if (_block_nowPos64 >= _size)
                return false;
            var posState = (UInt32)_block_nowPos64 & _posStateMask;
            if (_isMatchDecoders[(_block_state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].Decode(_rangeDecoder))
            {
                UInt32 len;
                if (DecodeRep(posState, out len))
                    return false;
                _outWindow.CopyBlock(_block_rep0, len);
                _block_nowPos64 += len;
            }
            else
            {
                var prevByte = _outWindow.GetByte(0);
                var b =
                    _block_state.IsCharState()
                    ? _literalDecoder.DecodeNormal(_rangeDecoder, (UInt32)_block_nowPos64, prevByte)
                    : _literalDecoder.DecodeWithMatchByte(_rangeDecoder, (UInt32)_block_nowPos64, prevByte, _outWindow.GetByte(_block_rep0));
                _outWindow.PutByte(b);
                _block_state.UpdateChar();
                _block_nowPos64++;
            }
            return true;
        }

        private bool DecodeRep(uint posState, out uint len)
        {
            if (_isRepDecoders[_block_state.Index].Decode(_rangeDecoder))
            {
                if (!_isRepG0Decoders[_block_state.Index].Decode(_rangeDecoder))
                {
                    if (!_isRep0LongDecoders[(_block_state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].Decode(_rangeDecoder))
                    {
                        len = 1;
                        _block_state.UpdateShortRep();
                        return false;
                    }
                }
                else
                {
                    if (!_isRepG1Decoders[_block_state.Index].Decode(_rangeDecoder))
                    {
                        var temp = _block_rep1;
                        _block_rep1 = _block_rep0;
                        _block_rep0 = temp;
                    }
                    else if (!_isRepG2Decoders[_block_state.Index].Decode(_rangeDecoder))
                    {
                        var temp = _block_rep2;
                        _block_rep2 = _block_rep1;
                        _block_rep1 = _block_rep0;
                        _block_rep0 = temp;
                    }
                    else
                    {
                        var temp = _block_rep3;
                        _block_rep3 = _block_rep2;
                        _block_rep2 = _block_rep1;
                        _block_rep1 = _block_rep0;
                        _block_rep0 = temp;
                    }
                }
                len = _repLenDecoder.Decode(_rangeDecoder, posState) + LzmaCoder.kMatchMinLen;
                _block_state.UpdateRep();
            }
            else
            {
                _block_rep3 = _block_rep2;
                _block_rep2 = _block_rep1;
                _block_rep1 = _block_rep0;
                len = LzmaCoder.kMatchMinLen + _lenDecoder.Decode(_rangeDecoder, posState);
                _block_state.UpdateMatch();
                UInt32 posSlot = _posSlotDecoder[LzmaCoder.GetLenToPosState(len)].Decode(_rangeDecoder);
                if (posSlot >= LzmaCoder.kStartPosModelIndex)
                {
                    int numDirectBits = (int)((posSlot >> 1) - 1);
                    _block_rep0 = ((2 | (posSlot & 1)) << numDirectBits);
                    if (posSlot < LzmaCoder.kEndPosModelIndex)
                        _block_rep0 += BitTreeDecoder.ReverseDecode(_posDecoders,
                                _block_rep0 - posSlot - 1, _rangeDecoder, numDirectBits);
                    else
                    {
                        _block_rep0 += (_rangeDecoder.DecodeDirectBits(
                            numDirectBits - LzmaCoder.kNumAlignBits) << LzmaCoder.kNumAlignBits);
                        _block_rep0 += _posAlignDecoder.ReverseDecode(_rangeDecoder);
                    }
                }
                else
                    _block_rep0 = posSlot;
            }
            return IsEndOfDecoding(_block_rep0, _block_nowPos64);
        }

        private bool IsEndOfDecoding(uint rep0, ulong nowPos64)
        {
            if (rep0 < nowPos64 && rep0 < _dictionarySizeCheck)
                return false;
            else if (rep0 == UInt32.MaxValue)
                return true;
            else
                throw new DataErrorException();
        }

        private void SetDictionarySize(UInt32 dictionarySize)
        {
            if (_dictionarySize != dictionarySize)
            {
                _dictionarySize = dictionarySize;
                _dictionarySizeCheck = _dictionarySize.Maximum(1U);
                var blockSize = _dictionarySizeCheck.Maximum(1U << 12);
                _outWindow.Create(blockSize);
            }
        }

        private void SetLiteralProperties(int lp, int lc)
        {
            if (lp > 8)
                throw new ArgumentException();
            if (lc > 8)
                throw new ArgumentException();
            _literalDecoder.Create(lp, lc);
        }

        private void SetPosBitsProperties(int pb)
        {
            if (pb > LzmaCoder.kNumPosStatesBitsMax)
                throw new ArgumentException();
            var numPosStates = 1U << pb;
            _lenDecoder.Create(numPosStates);
            _repLenDecoder.Create(numPosStates);
            _posStateMask = numPosStates - 1;
        }

        private void Init(IBasicInputByteStream inStream, IOutputBuffer outStream)
        {
            _rangeDecoder.Init(inStream);
            _outWindow.Init(outStream, _solid);

            for (var i = 0U; i < LzmaCoder.kNumStates; i++)
            {
                for (var j = 0U; j <= _posStateMask; j++)
                {
                    var index = (i << LzmaCoder.kNumPosStatesBitsMax) + j;
                    _isMatchDecoders[index].Init();
                    _isRep0LongDecoders[index].Init();
                }
                _isRepDecoders[i].Init();
                _isRepG0Decoders[i].Init();
                _isRepG1Decoders[i].Init();
                _isRepG2Decoders[i].Init();
            }

            _literalDecoder.Init();
            for (var i = 0U; i < LzmaCoder.kNumLenToPosStates; i++)
                _posSlotDecoder[i].Init();
            for (var i = 0U; i < LzmaCoder.kNumFullDistances - LzmaCoder.kEndPosModelIndex; i++)
                _posDecoders[i].Init();
            _lenDecoder.Init();
            _repLenDecoder.Init();
            _posAlignDecoder.Init();
        }
    }
}
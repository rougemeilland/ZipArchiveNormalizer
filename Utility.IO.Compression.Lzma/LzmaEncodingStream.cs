using System;
using System.Linq;
using Utility.IO.Compression.Lzma.RangeCoder;

namespace Utility.IO.Compression.Lzma
{
    public class LzmaEncodingStream
        : IBasicOutputByteStream, IInputBuffer
    {
        public const UInt16 PROPERTY_SIZE = 5;

        private const UInt32 _kIfinityPrice = 0xFFFFFFF;
        private const int _kDefaultDictionaryLogSize = 22;
        private const UInt32 _kNumFastBytesDefault = 0x20;
        private const UInt32 _kNumLenSpecSymbols = LzmaCoder.kNumLowLenSymbols + LzmaCoder.kNumMidLenSymbols;
        private const UInt32 _kNumOpts = 1 << 12;
        private static Byte[] _fastPos;

        private LzmaCoder.State _state;
        private Byte _previousByte;
        private UInt32[] _repDistances;
        private LzmaOptimal[] _optimum;
        private Lz.IMatchFinder _matchFinder;
        private RangeCoder.RangeEncoder _rangeEncoder;
        private RangeCoder.BitEncoder[] _isMatch;
        private RangeCoder.BitEncoder[] _isRep;
        private RangeCoder.BitEncoder[] _isRepG0;
        private RangeCoder.BitEncoder[] _isRepG1;
        private RangeCoder.BitEncoder[] _isRepG2;
        private RangeCoder.BitEncoder[] _isRep0Long;
        private RangeCoder.BitTreeEncoder[] _posSlotEncoder;
        private RangeCoder.BitEncoder[] _posEncoders;
        private RangeCoder.BitTreeEncoder _posAlignEncoder;
        private LzmaLenPriceTableEncoder _lenEncoder;
        private LzmaLenPriceTableEncoder _repMatchLenEncoder;
        private LzmaLiteralEncoder _literalEncoder;
        private UInt32[] _matchDistances;
        private UInt32 _numFastBytes;
        private UInt32 _longestMatchLength;
        private UInt32 _numDistancePairs;
        private UInt32 _additionalOffset;
        private UInt32 _optimumEndIndex;
        private UInt32 _optimumCurrentIndex;
        private bool _longestMatchWasFound;
        private UInt32[] _posSlotPrices;
        private UInt32[] _distancesPrices;
        private UInt32[] _alignPrices;
        private UInt32 _alignPriceCount;
        private UInt32 _distTableSize;
        private int _posStateBits;
        private UInt32 _posStateMask;
        private int _numLiteralPosStateBits;
        private int _numLiteralContextBits;
        private UInt32 _dictionarySize;
        private UInt32 _dictionarySizePrev;
        private UInt32 _numFastBytesPrev;
        private UInt64 _nowPos64;
        private bool _finishedEncoding;
        private MatchFinderType _matchFinderType;
        private bool _writeEndMark;
        private bool _needReleaseMFStream;
        private UInt32[] _reps;
        private UInt32[] _repLens;
        private Byte[] _properties;
        private UInt32[] _tempPrices;
        private UInt32 _matchPriceCount;

        private bool _isDisposed;
        private IBasicOutputByteStream _baseStream;
        private IO.ICodingProgressReportable _progressReporter;
        private bool _leaveOpen;
        private bool _isMatchFinderInitialized;
        private byte[] _inputBuffer;
        private UInt32 _sizeOfDataInInputBuffer;
        private UInt32 _startOfDataInInputBuffer;
        private bool _isClosedSourceStream;

        static LzmaEncodingStream()
        {
            const Byte kFastSlots = 22;

            _fastPos = new Byte[1 << 11];
            var c = 0;
            _fastPos[c++] = 0;
            _fastPos[c++] = 1;
            for (Byte slotFast = 2; slotFast < kFastSlots; slotFast++)
            {
                var k = 1U << ((slotFast >> 1) - 1);
                for (var j = 0; j < k; j++, c++)
                    _fastPos[c] = slotFast;
            }
        }

        public LzmaEncodingStream(IBasicOutputByteStream baseStream, CoderProperties properties, bool leaveOpen = false)
            : this(baseStream, properties, null, leaveOpen)
        {
        }

        public LzmaEncodingStream(IBasicOutputByteStream baseStream, CoderProperties properties, ICodingProgressReportable progressReporter, bool leaveOpen = false)
        {
            _state = new LzmaCoder.State();
            _previousByte = 0;
            _repDistances = new UInt32[LzmaCoder.kNumRepDistances];
            _optimum = new LzmaOptimal[_kNumOpts];
            _matchFinder = null;
            _rangeEncoder = new RangeCoder.RangeEncoder();
            _isMatch = new RangeCoder.BitEncoder[LzmaCoder.kNumStates << LzmaCoder.kNumPosStatesBitsMax];
            _isRep = new RangeCoder.BitEncoder[LzmaCoder.kNumStates];
            _isRepG0 = new RangeCoder.BitEncoder[LzmaCoder.kNumStates];
            _isRepG1 = new RangeCoder.BitEncoder[LzmaCoder.kNumStates];
            _isRepG2 = new RangeCoder.BitEncoder[LzmaCoder.kNumStates];
            _isRep0Long = new RangeCoder.BitEncoder[LzmaCoder.kNumStates << LzmaCoder.kNumPosStatesBitsMax];
            _posSlotEncoder = new RangeCoder.BitTreeEncoder[LzmaCoder.kNumLenToPosStates];
            _posEncoders = new RangeCoder.BitEncoder[LzmaCoder.kNumFullDistances - LzmaCoder.kEndPosModelIndex];
            _posAlignEncoder = new RangeCoder.BitTreeEncoder(LzmaCoder.kNumAlignBits);
            _lenEncoder = new LzmaLenPriceTableEncoder();
            _repMatchLenEncoder = new LzmaLenPriceTableEncoder();
            _literalEncoder = new LzmaLiteralEncoder();
            _matchDistances = new UInt32[LzmaCoder.kMatchMaxLen * 2 + 2];
            _numFastBytes = _kNumFastBytesDefault;
            _longestMatchLength = 0;
            _numDistancePairs = 0;
            _additionalOffset = 0;
            _optimumEndIndex = 0;
            _optimumCurrentIndex = 0;
            _longestMatchWasFound = false;
            _posSlotPrices = new UInt32[1 << (LzmaCoder.kNumPosSlotBits + LzmaCoder.kNumLenToPosStatesBits)];
            _distancesPrices = new UInt32[LzmaCoder.kNumFullDistances << LzmaCoder.kNumLenToPosStatesBits];
            _alignPrices = new UInt32[LzmaCoder.kAlignTableSize];
            _alignPriceCount = 0;
            _distTableSize = (_kDefaultDictionaryLogSize * 2);
            _posStateBits = 2;
            _posStateMask = 4 - 1;
            _numLiteralPosStateBits = 0;
            _numLiteralContextBits = 3;
            _dictionarySize = (1 << _kDefaultDictionaryLogSize);
            _dictionarySizePrev = UInt32.MaxValue;
            _numFastBytesPrev = UInt32.MaxValue;
            _nowPos64 = 0;
            _finishedEncoding = false;
            _matchFinderType = MatchFinderType.BT4;
            _writeEndMark = false;
            _needReleaseMFStream = false;
            _reps = new UInt32[LzmaCoder.kNumRepDistances];
            _repLens = new UInt32[LzmaCoder.kNumRepDistances];
            _properties = new Byte[PROPERTY_SIZE];
            _tempPrices = new UInt32[LzmaCoder.kNumFullDistances];
            _matchPriceCount = 0;
            for (int i = 0; i < _kNumOpts; i++)
                _optimum[i] = new LzmaOptimal();
            for (int i = 0; i < LzmaCoder.kNumLenToPosStates; i++)
                _posSlotEncoder[i] = new RangeCoder.BitTreeEncoder(LzmaCoder.kNumPosSlotBits);

            SetCoderProperties(properties);

            _isDisposed = false;
            _baseStream = baseStream;
            _progressReporter = progressReporter;
            _leaveOpen = leaveOpen;
            _isMatchFinderInitialized = true;
            _isClosedSourceStream = false;

            SetStreams(this, baseStream);
            _inputBuffer = new byte[_matchFinder.BlockSize];
        }

        public void WriteCoderProperties()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _properties[0] = (Byte)((_posStateBits * 5 + _numLiteralPosStateBits) * 9 + _numLiteralContextBits);
            _properties.CopyValueLE(1, _dictionarySize);
            _baseStream.WriteBytes(_properties.AsReadOnly(), 0, PROPERTY_SIZE);
        }

        public int Write(IReadOnlyArray<byte> buffer, int offset, int count)
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

#if DEBUG
            if (_startOfDataInInputBuffer < 0)
                throw new Exception();
            if (_startOfDataInInputBuffer >= _inputBuffer.Length)
                throw new Exception();
            if (_startOfDataInInputBuffer > int.MaxValue)
                throw new Exception();
            if (_sizeOfDataInInputBuffer < 0)
                throw new Exception();
            if (_sizeOfDataInInputBuffer > _inputBuffer.Length)
                throw new Exception();
            if (_sizeOfDataInInputBuffer > int.MaxValue)
                throw new Exception();
#endif
            if (_sizeOfDataInInputBuffer >= _inputBuffer.Length)
                EncodeBlock();

            var actualCount =
                ((UInt32)count).Minimum(
                    _sizeOfDataInInputBuffer >= (UInt32)_inputBuffer.Length - _startOfDataInInputBuffer
                    ? (UInt32)_inputBuffer.Length - _sizeOfDataInInputBuffer
                    : (UInt32)_inputBuffer.Length - _sizeOfDataInInputBuffer - _startOfDataInInputBuffer);
            var offsetInInputBuffer =
                _sizeOfDataInInputBuffer >= _inputBuffer.Length - _startOfDataInInputBuffer
                ? _startOfDataInInputBuffer - (_inputBuffer.Length - _sizeOfDataInInputBuffer)
                : _startOfDataInInputBuffer + _sizeOfDataInInputBuffer;

            buffer.CopyTo(offset, _inputBuffer, (int)offsetInInputBuffer, (int)actualCount);
            _sizeOfDataInInputBuffer += actualCount;
            return (int)actualCount;
        }

        public void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        public void Close()
        {
            EncodeLastBlock();
        }

        int IInputBuffer.Read(byte[] buffer, int offset, int count)
        {
#if DEBUG
            if (_startOfDataInInputBuffer < 0)
                throw new Exception();
            if (_startOfDataInInputBuffer >= _inputBuffer.Length)
                throw new Exception();
            if (_startOfDataInInputBuffer > int.MaxValue)
                throw new Exception();
            if (_sizeOfDataInInputBuffer < 0)
                throw new Exception();
            if (_sizeOfDataInInputBuffer > _inputBuffer.Length)
                throw new Exception();
            if (_sizeOfDataInInputBuffer > int.MaxValue)
                throw new Exception();
#endif
            var actualCount =
                ((uint)count).Minimum(_sizeOfDataInInputBuffer).Minimum((uint)_inputBuffer.Length - _startOfDataInInputBuffer);
            if (actualCount <= 0)
            {
                if (count > 0 && !_isClosedSourceStream)
                    throw
                        new DataErrorException(
                            string.Format(
                                "An unexplained exception. The read request size from '_matchFinder' is larger than expected. : expected <= 0x{0:x8}, actual >= 0x{1:x8}",
                                _inputBuffer.Length,
                                (long)_inputBuffer.Length + count));
                return 0;
            }
            _inputBuffer.CopyTo((int)_startOfDataInInputBuffer, buffer, offset, (int)actualCount);
            _startOfDataInInputBuffer += actualCount;
            _sizeOfDataInInputBuffer -= actualCount;
            if (_startOfDataInInputBuffer >= _inputBuffer.Length)
                _startOfDataInInputBuffer = 0;
            return (int)actualCount;
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
                    if (_baseStream != null)
                    {
                        EncodeLastBlock();
                        if (_leaveOpen == false)
                            _baseStream.Dispose();
                        _baseStream = null;
                    }
                }
                _isDisposed = true;
            }
        }

        private void SetCoderProperties(CoderProperties properties)
        {
            _numFastBytes = properties.NumFastBytes;
            if (_numFastBytes.IsBetween(5U, LzmaCoder.kMatchMaxLen) == false)
                throw new ArgumentException();

            var matchFinderIndexPrev = _matchFinderType;
            var key = properties.MatchFinder;
            _matchFinderType =
                Enum.GetValues(typeof(MatchFinderType))
                .Cast<MatchFinderType>()
                .Where(value => string.Equals(value.ToString(), key, StringComparison.OrdinalIgnoreCase))
                .Concat(new[] { (MatchFinderType)Int32.MinValue })
                .First();
            if (_matchFinderType == (MatchFinderType)Int32.MinValue)
                throw new ArgumentException();
            if (_matchFinder != null && matchFinderIndexPrev != _matchFinderType)
            {
                _dictionarySizePrev = UInt32.MaxValue;
                _matchFinder = null;
            }

            const int kDicLogSizeMaxCompress = 30;
            _dictionarySize = properties.DictionarySize;
            if (_dictionarySize.IsBetween(1U << LzmaCoder.kDicLogSizeMin, 1U << kDicLogSizeMaxCompress) == false)
                throw new ArgumentException();
            var dicLogSize = 0;
            while (dicLogSize < kDicLogSizeMaxCompress)
            {
                if ((1U << dicLogSize) >= _dictionarySize)
                    break;
                ++dicLogSize;
            }
            _distTableSize = (UInt32)dicLogSize * 2;

            _posStateBits = properties.PosStateBits;
            if (_posStateBits.IsBetween(0, LzmaCoder.kNumPosStatesBitsEncodingMax) == false)
                throw new ArgumentException();
            _posStateMask = (1U << _posStateBits) - 1;

            _numLiteralPosStateBits = properties.LitPosBits;
            if (_numLiteralPosStateBits.IsBetween(0, LzmaCoder.kNumLitPosStatesBitsEncodingMax) == false)
                throw new ArgumentException();

            _numLiteralContextBits = properties.LitContextBits;
            if (_numLiteralContextBits.IsBetween(0, LzmaCoder.kNumLitContextBitsMax) == false)
                throw new ArgumentException();

            SetWriteEndMarkerMode(properties.EndMarker);
        }

        private void SetWriteEndMarkerMode(bool writeEndMarker)
        {
            _writeEndMark = writeEndMarker;
        }

        private void EncodeLastBlock()
        {
            try
            {
                _isClosedSourceStream = true;
                while (EncodeBlock()) ;
            }
            catch (Exception)
            {
            }
        }

        private bool EncodeBlock()
        {
            if (_finishedEncoding)
                return false;

            if (_isMatchFinderInitialized)
            {
                _matchFinder.Init();
                _isMatchFinderInitialized = false;
            }

            UInt64 inSize;
            UInt64 outSize;
            var doContinute = CodeOneBlock(out inSize, out outSize);
            if (!doContinute)
            {
                ReleaseStreams();
                _finishedEncoding = true;
            }
            if (_progressReporter != null)
            {
                try
                {
                    _progressReporter.SetProgress(inSize);
                }
                catch (Exception)
                {
                }
            }
            return doContinute;
        }

        private bool CodeOneBlock(out UInt64 inSize, out UInt64 outSize)
        {
            inSize = 0;
            outSize = 0;
            var previousInSize = _nowPos64;
            var previousOutSize = _rangeEncoder.GetProcessedSizeAdd();

            var progressPosValuePrev = _nowPos64;
            if (_nowPos64 == 0)
            {
                if (_matchFinder.GetNumAvailableBytes() == 0)
                {
                    Flush((UInt32)_nowPos64);
                    return false;
                }
                {
                    UInt32 len;
                    UInt32 numDistancePairs;
                    ReadMatchDistances(out len, out numDistancePairs);
                }
                var posState = (UInt32)_nowPos64 & _posStateMask;
                _isMatch[(_state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].Encode(_rangeEncoder, false);
                _state.UpdateChar();
                var curByte = _matchFinder.GetIndexByte((Int32)(0 - _additionalOffset));
                _literalEncoder.GetSubCoder((UInt32)(_nowPos64), _previousByte).Encode(_rangeEncoder, curByte);
                _previousByte = curByte;
                _additionalOffset--;
                _nowPos64++;
            }
            if (_matchFinder.GetNumAvailableBytes() == 0)
            {
                Flush((UInt32)_nowPos64);
                return false;
            }
            while (true)
            {
                UInt32 pos;
                var len = GetOptimum((UInt32)_nowPos64, out pos);

                var posState = ((UInt32)_nowPos64) & _posStateMask;
                var complexState = (_state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState;
                if (len == 1 && pos == UInt32.MaxValue)
                {
                    _isMatch[complexState].Encode(_rangeEncoder, false);
                    var curByte = _matchFinder.GetIndexByte(-(Int32)_additionalOffset);
                    var subCoder = _literalEncoder.GetSubCoder((UInt32)_nowPos64, _previousByte);
                    if (!_state.IsCharState())
                    {
                        var matchByte = _matchFinder.GetIndexByte(-(Int32)(_repDistances[0] + 1 + _additionalOffset));
                        subCoder.EncodeMatched(_rangeEncoder, matchByte, curByte);
                    }
                    else
                        subCoder.Encode(_rangeEncoder, curByte);
                    _previousByte = curByte;
                    _state.UpdateChar();
                }
                else
                {
                    _isMatch[complexState].Encode(_rangeEncoder, true);
                    if (pos < LzmaCoder.kNumRepDistances)
                    {
                        _isRep[_state.Index].Encode(_rangeEncoder, true);
                        if (pos == 0)
                        {
                            _isRepG0[_state.Index].Encode(_rangeEncoder, false);
                            if (len == 1)
                                _isRep0Long[complexState].Encode(_rangeEncoder, false);
                            else
                                _isRep0Long[complexState].Encode(_rangeEncoder, true);
                        }
                        else
                        {
                            _isRepG0[_state.Index].Encode(_rangeEncoder, true);
                            if (pos == 1)
                                _isRepG1[_state.Index].Encode(_rangeEncoder, false);
                            else
                            {
                                _isRepG1[_state.Index].Encode(_rangeEncoder, true);
                                _isRepG2[_state.Index].Encode(_rangeEncoder, pos > 2);
                            }
                        }
                        if (len == 1)
                            _state.UpdateShortRep();
                        else
                        {
                            _repMatchLenEncoder.Encode(_rangeEncoder, len - LzmaCoder.kMatchMinLen, posState);
                            _state.UpdateRep();
                        }
                        var distance = _repDistances[pos];
                        if (pos != 0)
                        {
                            for (UInt32 i = pos; i >= 1; i--)
                                _repDistances[i] = _repDistances[i - 1];
                            _repDistances[0] = distance;
                        }
                    }
                    else
                    {
                        _isRep[_state.Index].Encode(_rangeEncoder, false);
                        _state.UpdateMatch();
                        _lenEncoder.Encode(_rangeEncoder, len - LzmaCoder.kMatchMinLen, posState);
                        pos -= LzmaCoder.kNumRepDistances;
                        var posSlot = GetPosSlot(pos);
                        var lenToPosState = LzmaCoder.GetLenToPosState(len);
                        _posSlotEncoder[lenToPosState].Encode(_rangeEncoder, posSlot);

                        if (posSlot >= LzmaCoder.kStartPosModelIndex)
                        {
                            int footerBits = (int)((posSlot >> 1) - 1);
                            var baseVal = (2 | (posSlot & 1)) << footerBits;
                            var posReduced = pos - baseVal;

                            if (posSlot < LzmaCoder.kEndPosModelIndex)
                                RangeCoder.BitTreeEncoder.ReverseEncode(_posEncoders,
                                        baseVal - posSlot - 1, _rangeEncoder, footerBits, posReduced);
                            else
                            {
                                _rangeEncoder.EncodeDirectBits(posReduced >> LzmaCoder.kNumAlignBits, footerBits - LzmaCoder.kNumAlignBits);
                                _posAlignEncoder.ReverseEncode(_rangeEncoder, posReduced & LzmaCoder.kAlignMask);
                                _alignPriceCount++;
                            }
                        }
                        var distance = pos;
                        for (var i = LzmaCoder.kNumRepDistances - 1; i >= 1; i--)
                            _repDistances[i] = _repDistances[i - 1];
                        _repDistances[0] = distance;
                        _matchPriceCount++;
                    }
                    _previousByte = _matchFinder.GetIndexByte((Int32)(len - 1 - _additionalOffset));
                }
                _additionalOffset -= len;
                _nowPos64 += len;
                if (_additionalOffset == 0)
                {
                    // if (!_fastMode)
                    if (_matchPriceCount >= (1 << 7))
                        FillDistancesPrices();
                    if (_alignPriceCount >= LzmaCoder.kAlignTableSize)
                        FillAlignPrices();
                    inSize = _nowPos64 - previousInSize;
                    outSize = _rangeEncoder.GetProcessedSizeAdd() - previousOutSize;
                    if (_matchFinder.GetNumAvailableBytes() == 0)
                    {
                        Flush((UInt32)_nowPos64);
                        return false;
                    }

                    if (_nowPos64 - progressPosValuePrev >= (1 << 12))
                        return true;
                }
            }
        }

        private void SetStreams(IInputBuffer inStream, IBasicOutputByteStream outStream)
        {
            SetOutStream(outStream);
            SetMFStream(inStream);
        }

        private void ReleaseStreams()
        {
            ReleaseMFStream();
            ReleaseOutStream();
        }

        private void SetOutStream(IBasicOutputByteStream outStream)
        {
            _finishedEncoding = false;
            Create();
            _rangeEncoder.SetStream(outStream);
            Init();

            FillDistancesPrices();
            FillAlignPrices();

            _lenEncoder.SetTableSize(_numFastBytes + 1 - LzmaCoder.kMatchMinLen);
            _lenEncoder.UpdateTables(1U << _posStateBits);
            _repMatchLenEncoder.SetTableSize(_numFastBytes + 1 - LzmaCoder.kMatchMinLen);
            _repMatchLenEncoder.UpdateTables(1U << _posStateBits);

            _nowPos64 = 0;
        }

        private void FillDistancesPrices()
        {
            for (var i = LzmaCoder.kStartPosModelIndex; i < LzmaCoder.kNumFullDistances; i++)
            {
                UInt32 posSlot = GetPosSlot(i);
                int footerBits = (int)((posSlot >> 1) - 1);
                UInt32 baseVal = ((2 | (posSlot & 1)) << footerBits);
                _tempPrices[i] = BitTreeEncoder.ReverseGetPrice(_posEncoders,
                    baseVal - posSlot - 1, footerBits, i - baseVal);
            }

            for (var lenToPosState = 0U; lenToPosState < LzmaCoder.kNumLenToPosStates; lenToPosState++)
            {
                RangeCoder.BitTreeEncoder encoder = _posSlotEncoder[lenToPosState];

                var st = (lenToPosState << LzmaCoder.kNumPosSlotBits);
                for (var posSlot = 0U; posSlot < _distTableSize; posSlot++)
                    _posSlotPrices[st + posSlot] = encoder.GetPrice(posSlot);
                for (var posSlot = LzmaCoder.kEndPosModelIndex; posSlot < _distTableSize; posSlot++)
                    _posSlotPrices[st + posSlot] += ((((posSlot >> 1) - 1) - LzmaCoder.kNumAlignBits) << RangeCoder.BitEncoder.kNumBitPriceShiftBits);

                var st2 = lenToPosState * LzmaCoder.kNumFullDistances;
                var i = 0U;
                for (; i < LzmaCoder.kStartPosModelIndex; i++)
                    _distancesPrices[st2 + i] = _posSlotPrices[st + i];
                for (; i < LzmaCoder.kNumFullDistances; i++)
                    _distancesPrices[st2 + i] = _posSlotPrices[st + GetPosSlot(i)] + _tempPrices[i];
            }
            _matchPriceCount = 0;
        }

        private void FillAlignPrices()
        {
            for (var i = 0U; i < LzmaCoder.kAlignTableSize; i++)
                _alignPrices[i] = _posAlignEncoder.ReverseGetPrice(i);
            _alignPriceCount = 0;
        }

        private void ReleaseOutStream()
        {
            _rangeEncoder.ReleaseStream();
        }

        private void Create()
        {
            if (_matchFinder == null)
            {
                Lz.BinTree bt = new Lz.BinTree();
                int numHashBytes = 4;
                if (_matchFinderType == MatchFinderType.BT2)
                    numHashBytes = 2;
                bt.SetType(numHashBytes);
                _matchFinder = bt;
            }
            _literalEncoder.Create(_numLiteralPosStateBits, _numLiteralContextBits);

            if (_dictionarySize == _dictionarySizePrev && _numFastBytesPrev == _numFastBytes)
                return;
            _matchFinder.Create(_dictionarySize, _kNumOpts, _numFastBytes, LzmaCoder.kMatchMaxLen + 1);
            _dictionarySizePrev = _dictionarySize;
            _numFastBytesPrev = _numFastBytes;
        }

        private void Init()
        {
            BaseInit();
            _rangeEncoder.Init();

            for (var i = 0U; i < LzmaCoder.kNumStates; i++)
            {
                for (var j = 0U; j <= _posStateMask; j++)
                {
                    var complexState = (i << LzmaCoder.kNumPosStatesBitsMax) + j;
                    _isMatch[complexState].Init();
                    _isRep0Long[complexState].Init();
                }
                _isRep[i].Init();
                _isRepG0[i].Init();
                _isRepG1[i].Init();
                _isRepG2[i].Init();
            }
            _literalEncoder.Init();
            for (var i = 0; i < LzmaCoder.kNumLenToPosStates; i++)
                _posSlotEncoder[i].Init();
            for (var i = 0; i < LzmaCoder.kNumFullDistances - LzmaCoder.kEndPosModelIndex; i++)
                _posEncoders[i].Init();

            _lenEncoder.Init(1U << _posStateBits);
            _repMatchLenEncoder.Init(1U << _posStateBits);

            _posAlignEncoder.Init();

            _longestMatchWasFound = false;
            _optimumEndIndex = 0;
            _optimumCurrentIndex = 0;
            _additionalOffset = 0;
        }

        private void BaseInit()
        {
            _state.Init();
            _previousByte = 0;
            for (var i = 0; i < LzmaCoder.kNumRepDistances; i++)
                _repDistances[i] = 0;
        }

        private void SetMFStream(IInputBuffer inStream)
        {
            ReleaseMFStream();
            _matchFinder.SetStream(inStream);
            _needReleaseMFStream = true;
        }

        private void ReleaseMFStream()
        {
            if (_matchFinder != null && _needReleaseMFStream)
            {
                _matchFinder.ReleaseStream();
                _needReleaseMFStream = false;
            }
        }

        private UInt32 GetOptimum(UInt32 position, out UInt32 backRes)
        {
            if (_optimumEndIndex != _optimumCurrentIndex)
            {
                var lenRes = _optimum[_optimumCurrentIndex].PosPrev - _optimumCurrentIndex;
                backRes = _optimum[_optimumCurrentIndex].BackPrev;
                _optimumCurrentIndex = _optimum[_optimumCurrentIndex].PosPrev;
                return lenRes;
            }
            _optimumCurrentIndex = _optimumEndIndex = 0;

            UInt32 lenMain;
            UInt32 numDistancePairs;
            if (!_longestMatchWasFound)
            {
                ReadMatchDistances(out lenMain, out numDistancePairs);
            }
            else
            {
                lenMain = _longestMatchLength;
                numDistancePairs = _numDistancePairs;
                _longestMatchWasFound = false;
            }

            var numAvailableBytes = _matchFinder.GetNumAvailableBytes() + 1;
            if (numAvailableBytes < 2)
            {
                backRes = UInt32.MaxValue;
                return 1;
            }
            if (numAvailableBytes > LzmaCoder.kMatchMaxLen)
                numAvailableBytes = LzmaCoder.kMatchMaxLen;

            var repMaxIndex = 0U;
            for (var i = 0U; i < LzmaCoder.kNumRepDistances; i++)
            {
                _reps[i] = _repDistances[i];
                _repLens[i] = _matchFinder.GetMatchLen(0 - 1, _reps[i], LzmaCoder.kMatchMaxLen);
                if (_repLens[i] > _repLens[repMaxIndex])
                    repMaxIndex = i;
            }
            if (_repLens[repMaxIndex] >= _numFastBytes)
            {
                backRes = repMaxIndex;
                var lenRes = _repLens[repMaxIndex];
                MovePos(lenRes - 1);
                return lenRes;
            }

            if (lenMain >= _numFastBytes)
            {
                backRes = _matchDistances[numDistancePairs - 1] + LzmaCoder.kNumRepDistances;
                MovePos(lenMain - 1);
                return lenMain;
            }

            var currentByte = _matchFinder.GetIndexByte(0 - 1);
            var matchByte = _matchFinder.GetIndexByte((Int32)(0 - _repDistances[0] - 1 - 1));

            if (lenMain < 2 && currentByte != matchByte && _repLens[repMaxIndex] < 2)
            {
                backRes = UInt32.MaxValue;
                return 1;
            }

            _optimum[0].State = _state;

            var posState = position & _posStateMask;

            _optimum[1].Price = _isMatch[(_state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].GetPrice0() +
                    _literalEncoder.GetSubCoder(position, _previousByte).GetPrice(!_state.IsCharState(), matchByte, currentByte);
            _optimum[1].MakeAsChar();

            var matchPrice = _isMatch[(_state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].GetPrice1();
            var repMatchPrice = matchPrice + _isRep[_state.Index].GetPrice1();

            if (matchByte == currentByte)
            {
                var shortRepPrice = repMatchPrice + GetRepLen1Price(_state, posState);
                if (shortRepPrice < _optimum[1].Price)
                {
                    _optimum[1].Price = shortRepPrice;
                    _optimum[1].MakeAsShortRep();
                }
            }

            var lenEnd = ((lenMain >= _repLens[repMaxIndex]) ? lenMain : _repLens[repMaxIndex]);

            if (lenEnd < 2)
            {
                backRes = _optimum[1].BackPrev;
                return 1;
            }

            _optimum[1].PosPrev = 0;

            _optimum[0].Backs0 = _reps[0];
            _optimum[0].Backs1 = _reps[1];
            _optimum[0].Backs2 = _reps[2];
            _optimum[0].Backs3 = _reps[3];

            var len = lenEnd;
            do
                _optimum[len--].Price = _kIfinityPrice;
            while (len >= 2);

            for (var i = 0U; i < LzmaCoder.kNumRepDistances; i++)
            {
                var repLen = _repLens[i];
                if (repLen < 2)
                    continue;
                var price = repMatchPrice + GetPureRepPrice(i, _state, posState);
                do
                {
                    var curAndLenPrice = price + _repMatchLenEncoder.GetPrice(repLen - 2, posState);
                    var optimum = _optimum[repLen];
                    if (curAndLenPrice < optimum.Price)
                    {
                        optimum.Price = curAndLenPrice;
                        optimum.PosPrev = 0;
                        optimum.BackPrev = i;
                        optimum.Prev1IsChar = false;
                    }
                }
                while (--repLen >= 2);
            }

            var normalMatchPrice = matchPrice + _isRep[_state.Index].GetPrice0();

            len = ((_repLens[0] >= 2) ? _repLens[0] + 1 : 2);
            if (len <= lenMain)
            {
                var offs = 0U;
                while (len > _matchDistances[offs])
                    offs += 2;
                for (; ; len++)
                {
                    var distance = _matchDistances[offs + 1];
                    var curAndLenPrice = normalMatchPrice + GetPosLenPrice(distance, len, posState);
                    var optimum = _optimum[len];
                    if (curAndLenPrice < optimum.Price)
                    {
                        optimum.Price = curAndLenPrice;
                        optimum.PosPrev = 0;
                        optimum.BackPrev = distance + LzmaCoder.kNumRepDistances;
                        optimum.Prev1IsChar = false;
                    }
                    if (len == _matchDistances[offs])
                    {
                        offs += 2;
                        if (offs == numDistancePairs)
                            break;
                    }
                }
            }

            var cur = 0U;

            while (true)
            {
                cur++;
                if (cur == lenEnd)
                    return Backward(out backRes, cur);
                UInt32 newLen;
                ReadMatchDistances(out newLen, out numDistancePairs);
                if (newLen >= _numFastBytes)
                {
                    _numDistancePairs = numDistancePairs;
                    _longestMatchLength = newLen;
                    _longestMatchWasFound = true;
                    return Backward(out backRes, cur);
                }
                position++;
                var posPrev = _optimum[cur].PosPrev;
                LzmaCoder.State state;
                if (_optimum[cur].Prev1IsChar)
                {
                    posPrev--;
                    if (_optimum[cur].Prev2)
                    {
                        state = _optimum[_optimum[cur].PosPrev2].State;
                        if (_optimum[cur].BackPrev2 < LzmaCoder.kNumRepDistances)
                            state.UpdateRep();
                        else
                            state.UpdateMatch();
                    }
                    else
                        state = _optimum[posPrev].State;
                    state.UpdateChar();
                }
                else
                    state = _optimum[posPrev].State;
                if (posPrev == cur - 1)
                {
                    if (_optimum[cur].IsShortRep())
                        state.UpdateShortRep();
                    else
                        state.UpdateChar();
                }
                else
                {
                    UInt32 pos;
                    if (_optimum[cur].Prev1IsChar && _optimum[cur].Prev2)
                    {
                        posPrev = _optimum[cur].PosPrev2;
                        pos = _optimum[cur].BackPrev2;
                        state.UpdateRep();
                    }
                    else
                    {
                        pos = _optimum[cur].BackPrev;
                        if (pos < LzmaCoder.kNumRepDistances)
                            state.UpdateRep();
                        else
                            state.UpdateMatch();
                    }
                    var opt = _optimum[posPrev];
                    if (pos < LzmaCoder.kNumRepDistances)
                    {
                        if (pos == 0)
                        {
                            _reps[0] = opt.Backs0;
                            _reps[1] = opt.Backs1;
                            _reps[2] = opt.Backs2;
                            _reps[3] = opt.Backs3;
                        }
                        else if (pos == 1)
                        {
                            _reps[0] = opt.Backs1;
                            _reps[1] = opt.Backs0;
                            _reps[2] = opt.Backs2;
                            _reps[3] = opt.Backs3;
                        }
                        else if (pos == 2)
                        {
                            _reps[0] = opt.Backs2;
                            _reps[1] = opt.Backs0;
                            _reps[2] = opt.Backs1;
                            _reps[3] = opt.Backs3;
                        }
                        else
                        {
                            _reps[0] = opt.Backs3;
                            _reps[1] = opt.Backs0;
                            _reps[2] = opt.Backs1;
                            _reps[3] = opt.Backs2;
                        }
                    }
                    else
                    {
                        _reps[0] = (pos - LzmaCoder.kNumRepDistances);
                        _reps[1] = opt.Backs0;
                        _reps[2] = opt.Backs1;
                        _reps[3] = opt.Backs2;
                    }
                }
                _optimum[cur].State = state;
                _optimum[cur].Backs0 = _reps[0];
                _optimum[cur].Backs1 = _reps[1];
                _optimum[cur].Backs2 = _reps[2];
                _optimum[cur].Backs3 = _reps[3];
                var curPrice = _optimum[cur].Price;

                currentByte = _matchFinder.GetIndexByte(0 - 1);
                matchByte = _matchFinder.GetIndexByte((Int32)(0 - _reps[0] - 1 - 1));

                posState = (position & _posStateMask);

                var curAnd1Price = curPrice +
                    _isMatch[(state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].GetPrice0() +
                    _literalEncoder.GetSubCoder(position, _matchFinder.GetIndexByte(0 - 2)).
                    GetPrice(!state.IsCharState(), matchByte, currentByte);

                var nextOptimum = _optimum[cur + 1];

                var nextIsChar = false;
                if (curAnd1Price < nextOptimum.Price)
                {
                    nextOptimum.Price = curAnd1Price;
                    nextOptimum.PosPrev = cur;
                    nextOptimum.MakeAsChar();
                    nextIsChar = true;
                }

                matchPrice = curPrice + _isMatch[(state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].GetPrice1();
                repMatchPrice = matchPrice + _isRep[state.Index].GetPrice1();

                if (matchByte == currentByte &&
                    !(nextOptimum.PosPrev < cur && nextOptimum.BackPrev == 0))
                {
                    var shortRepPrice = repMatchPrice + GetRepLen1Price(state, posState);
                    if (shortRepPrice <= nextOptimum.Price)
                    {
                        nextOptimum.Price = shortRepPrice;
                        nextOptimum.PosPrev = cur;
                        nextOptimum.MakeAsShortRep();
                        nextIsChar = true;
                    }
                }

                var numAvailableBytesFull = _matchFinder.GetNumAvailableBytes() + 1;
                numAvailableBytesFull = Math.Min(_kNumOpts - 1 - cur, numAvailableBytesFull);
                numAvailableBytes = numAvailableBytesFull;

                if (numAvailableBytes < 2)
                    continue;
                if (numAvailableBytes > _numFastBytes)
                    numAvailableBytes = _numFastBytes;
                if (!nextIsChar && matchByte != currentByte)
                {
                    // try Literal + rep0
                    var t = Math.Min(numAvailableBytesFull - 1, _numFastBytes);
                    var lenTest2 = _matchFinder.GetMatchLen(0, _reps[0], t);
                    if (lenTest2 >= 2)
                    {
                        LzmaCoder.State state2 = state;
                        state2.UpdateChar();
                        var posStateNext = (position + 1) & _posStateMask;
                        var nextRepMatchPrice = curAnd1Price +
                            _isMatch[(state2.Index << LzmaCoder.kNumPosStatesBitsMax) + posStateNext].GetPrice1() +
                            _isRep[state2.Index].GetPrice1();

                        var offset = cur + 1 + lenTest2;
                        while (lenEnd < offset)
                            _optimum[++lenEnd].Price = _kIfinityPrice;
                        var curAndLenPrice = nextRepMatchPrice + GetRepPrice(0, lenTest2, state2, posStateNext);
                        var optimum = _optimum[offset];
                        if (curAndLenPrice < optimum.Price)
                        {
                            optimum.Price = curAndLenPrice;
                            optimum.PosPrev = cur + 1;
                            optimum.BackPrev = 0;
                            optimum.Prev1IsChar = true;
                            optimum.Prev2 = false;
                        }
                    }
                }

                var startLen = 2U; // speed optimization 

                for (var repIndex = 0U; repIndex < LzmaCoder.kNumRepDistances; repIndex++)
                {
                    var lenTest = _matchFinder.GetMatchLen(0 - 1, _reps[repIndex], numAvailableBytes);
                    if (lenTest >= 2)
                    {
                        var lenTestTemp = lenTest;
                        do
                        {
                            while (lenEnd < cur + lenTest)
                                _optimum[++lenEnd].Price = _kIfinityPrice;
                            UInt32 curAndLenPrice = repMatchPrice + GetRepPrice(repIndex, lenTest, state, posState);
                            var optimum = _optimum[cur + lenTest];
                            if (curAndLenPrice < optimum.Price)
                            {
                                optimum.Price = curAndLenPrice;
                                optimum.PosPrev = cur;
                                optimum.BackPrev = repIndex;
                                optimum.Prev1IsChar = false;
                            }
                        }
                        while (--lenTest >= 2);
                        lenTest = lenTestTemp;

                        if (repIndex == 0)
                            startLen = lenTest + 1;

                        // if (_maxMode)
                        if (lenTest < numAvailableBytesFull)
                        {
                            var t = Math.Min(numAvailableBytesFull - 1 - lenTest, _numFastBytes);
                            var lenTest2 = _matchFinder.GetMatchLen((Int32)lenTest, _reps[repIndex], t);
                            if (lenTest2 >= 2)
                            {
                                LzmaCoder.State state2 = state;
                                state2.UpdateRep();
                                var posStateNext = (position + lenTest) & _posStateMask;
                                var curAndLenCharPrice =
                                        repMatchPrice + GetRepPrice(repIndex, lenTest, state, posState) +
                                        _isMatch[(state2.Index << LzmaCoder.kNumPosStatesBitsMax) + posStateNext].GetPrice0() +
                                        _literalEncoder.GetSubCoder(
                                            position + lenTest,
                                            _matchFinder.GetIndexByte((Int32)lenTest - 1 - 1)).GetPrice(true,
                                            _matchFinder.GetIndexByte((Int32)((Int32)lenTest - 1 - (Int32)(_reps[repIndex] + 1))),
                                            _matchFinder.GetIndexByte((Int32)lenTest - 1));
                                state2.UpdateChar();
                                posStateNext = (position + lenTest + 1) & _posStateMask;
                                var nextMatchPrice = curAndLenCharPrice + _isMatch[(state2.Index << LzmaCoder.kNumPosStatesBitsMax) + posStateNext].GetPrice1();
                                var nextRepMatchPrice = nextMatchPrice + _isRep[state2.Index].GetPrice1();

                                // for(; lenTest2 >= 2; lenTest2--)
                                {
                                    var offset = lenTest + 1 + lenTest2;
                                    while (lenEnd < cur + offset)
                                        _optimum[++lenEnd].Price = _kIfinityPrice;
                                    var curAndLenPrice = nextRepMatchPrice + GetRepPrice(0, lenTest2, state2, posStateNext);
                                    var optimum = _optimum[cur + offset];
                                    if (curAndLenPrice < optimum.Price)
                                    {
                                        optimum.Price = curAndLenPrice;
                                        optimum.PosPrev = cur + lenTest + 1;
                                        optimum.BackPrev = 0;
                                        optimum.Prev1IsChar = true;
                                        optimum.Prev2 = true;
                                        optimum.PosPrev2 = cur;
                                        optimum.BackPrev2 = repIndex;
                                    }
                                }
                            }
                        }
                    }
                }

                if (newLen > numAvailableBytes)
                {
                    newLen = numAvailableBytes;
                    for (numDistancePairs = 0; newLen > _matchDistances[numDistancePairs]; numDistancePairs += 2) ;
                    _matchDistances[numDistancePairs] = newLen;
                    numDistancePairs += 2;
                }
                if (newLen >= startLen)
                {
                    normalMatchPrice = matchPrice + _isRep[state.Index].GetPrice0();
                    while (lenEnd < cur + newLen)
                        _optimum[++lenEnd].Price = _kIfinityPrice;

                    var offs = 0U;
                    while (startLen > _matchDistances[offs])
                        offs += 2;

                    for (var lenTest = startLen; ; lenTest++)
                    {
                        var curBack = _matchDistances[offs + 1];
                        var curAndLenPrice = normalMatchPrice + GetPosLenPrice(curBack, lenTest, posState);
                        var optimum = _optimum[cur + lenTest];
                        if (curAndLenPrice < optimum.Price)
                        {
                            optimum.Price = curAndLenPrice;
                            optimum.PosPrev = cur;
                            optimum.BackPrev = curBack + LzmaCoder.kNumRepDistances;
                            optimum.Prev1IsChar = false;
                        }

                        if (lenTest == _matchDistances[offs])
                        {
                            if (lenTest < numAvailableBytesFull)
                            {
                                var t = (numAvailableBytesFull - 1 - lenTest).Minimum(_numFastBytes);
                                var lenTest2 = _matchFinder.GetMatchLen((Int32)lenTest, curBack, t);
                                if (lenTest2 >= 2)
                                {
                                    LzmaCoder.State state2 = state;
                                    state2.UpdateMatch();
                                    var posStateNext = (position + lenTest) & _posStateMask;
                                    var curAndLenCharPrice =
                                        curAndLenPrice +
                                        _isMatch[(state2.Index << LzmaCoder.kNumPosStatesBitsMax) + posStateNext].GetPrice0() +
                                        _literalEncoder.GetSubCoder(
                                            position + lenTest,
                                            _matchFinder.GetIndexByte((Int32)lenTest - 1 - 1)).
                                            GetPrice(
                                                true,
                                                _matchFinder.GetIndexByte((Int32)lenTest - (Int32)(curBack + 1) - 1),
                                                _matchFinder.GetIndexByte((Int32)lenTest - 1));
                                    state2.UpdateChar();
                                    posStateNext = (position + lenTest + 1) & _posStateMask;
                                    var nextMatchPrice = curAndLenCharPrice + _isMatch[(state2.Index << LzmaCoder.kNumPosStatesBitsMax) + posStateNext].GetPrice1();
                                    var nextRepMatchPrice = nextMatchPrice + _isRep[state2.Index].GetPrice1();

                                    var offset = lenTest + 1 + lenTest2;
                                    while (lenEnd < cur + offset)
                                        _optimum[++lenEnd].Price = _kIfinityPrice;
                                    curAndLenPrice = nextRepMatchPrice + GetRepPrice(0, lenTest2, state2, posStateNext);
                                    optimum = _optimum[cur + offset];
                                    if (curAndLenPrice < optimum.Price)
                                    {
                                        optimum.Price = curAndLenPrice;
                                        optimum.PosPrev = cur + lenTest + 1;
                                        optimum.BackPrev = 0;
                                        optimum.Prev1IsChar = true;
                                        optimum.Prev2 = true;
                                        optimum.PosPrev2 = cur;
                                        optimum.BackPrev2 = curBack + LzmaCoder.kNumRepDistances;
                                    }
                                }
                            }
                            offs += 2;
                            if (offs == numDistancePairs)
                                break;
                        }
                    }
                }
            }
        }

        private void ReadMatchDistances(out UInt32 lenRes, out UInt32 numDistancePairs)
        {
            lenRes = 0;
            numDistancePairs = _matchFinder.GetMatches(_matchDistances);
            if (numDistancePairs > 0)
            {
                lenRes = _matchDistances[numDistancePairs - 2];
                if (lenRes == _numFastBytes)
                    lenRes +=
                        _matchFinder.GetMatchLen(
                            (int)lenRes - 1,
                            _matchDistances[numDistancePairs - 1],
                            LzmaCoder.kMatchMaxLen - lenRes);
            }
            _additionalOffset++;
        }

        private void MovePos(UInt32 num)
        {
            if (num > 0)
            {
                _matchFinder.Skip(num);
                _additionalOffset += num;
            }
        }

        private UInt32 GetRepPrice(UInt32 repIndex, UInt32 len, LzmaCoder.State state, UInt32 posState)
        {
            return
                _repMatchLenEncoder.GetPrice(len - LzmaCoder.kMatchMinLen, posState) +
                GetPureRepPrice(repIndex, state, posState);
        }

        private UInt32 GetRepLen1Price(LzmaCoder.State state, UInt32 posState)
        {
            return
                _isRepG0[state.Index].GetPrice0() +
                _isRep0Long[(state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].GetPrice0();
        }

        private UInt32 GetPureRepPrice(UInt32 repIndex, LzmaCoder.State state, UInt32 posState)
        {
            switch (repIndex)
            {
                case 0:
                    return
                        _isRepG0[state.Index].GetPrice0() +
                        _isRep0Long[(state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].GetPrice1();
                case 1:
                    return
                        _isRepG0[state.Index].GetPrice1() +
                        _isRepG1[state.Index].GetPrice0();
                case 2:
                    return
                        _isRepG0[state.Index].GetPrice1() +
                        _isRepG1[state.Index].GetPrice1() +
                        _isRepG2[state.Index].GetPrice(false);
                default:
                    return
                        _isRepG0[state.Index].GetPrice1() +
                        _isRepG1[state.Index].GetPrice1() +
                        _isRepG2[state.Index].GetPrice(true);
            }
        }

        private UInt32 GetPosLenPrice(UInt32 pos, UInt32 len, UInt32 posState)
        {
            var lenToPosState = LzmaCoder.GetLenToPosState(len);
            if (pos < LzmaCoder.kNumFullDistances)
            {
                return
                    _distancesPrices[(lenToPosState * LzmaCoder.kNumFullDistances) + pos] +
                    _lenEncoder.GetPrice(len - LzmaCoder.kMatchMinLen, posState);
            }
            else
            {
                return
                    _posSlotPrices[(lenToPosState << LzmaCoder.kNumPosSlotBits) + GetPosSlot2(pos)] +
                    _alignPrices[pos & LzmaCoder.kAlignMask] +
                    _lenEncoder.GetPrice(len - LzmaCoder.kMatchMinLen, posState);
            }
        }

        private UInt32 Backward(out UInt32 backRes, UInt32 cur)
        {
            _optimumEndIndex = cur;
            var posMem = _optimum[cur].PosPrev;
            var backMem = _optimum[cur].BackPrev;
            do
            {
                if (_optimum[cur].Prev1IsChar)
                {
                    _optimum[posMem].MakeAsChar();
                    _optimum[posMem].PosPrev = posMem - 1;
                    if (_optimum[cur].Prev2)
                    {
                        _optimum[posMem - 1].Prev1IsChar = false;
                        _optimum[posMem - 1].PosPrev = _optimum[cur].PosPrev2;
                        _optimum[posMem - 1].BackPrev = _optimum[cur].BackPrev2;
                    }
                }
                var posPrev = posMem;
                var backCur = backMem;

                backMem = _optimum[posPrev].BackPrev;
                posMem = _optimum[posPrev].PosPrev;

                _optimum[posPrev].BackPrev = backCur;
                _optimum[posPrev].PosPrev = cur;
                cur = posPrev;
            }
            while (cur > 0);
            backRes = _optimum[0].BackPrev;
            _optimumCurrentIndex = _optimum[0].PosPrev;
            return _optimumCurrentIndex;
        }

        private static UInt32 GetPosSlot(UInt32 pos)
        {
            if (pos < (1 << 11))
                return _fastPos[pos];
            if (pos < (1 << 21))
                return _fastPos[pos >> 10] + 20U;
            return _fastPos[pos >> 20] + 40U;
        }

        private static UInt32 GetPosSlot2(UInt32 pos)
        {
            if (pos < (1 << 17))
                return _fastPos[pos >> 6] + 12U;
            if (pos < (1 << 27))
                return _fastPos[pos >> 16] + 32U;
            return _fastPos[pos >> 26] + 52U;
        }

        private void Flush(UInt32 nowPos)
        {
            ReleaseMFStream();
            WriteEndMarker(nowPos & _posStateMask);
            _rangeEncoder.FlushData();
        }

        private void WriteEndMarker(UInt32 posState)
        {
            if (!_writeEndMark)
                return;

            _isMatch[(_state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].Encode(_rangeEncoder, true);
            _isRep[_state.Index].Encode(_rangeEncoder, false);
            _state.UpdateMatch();
            var len = LzmaCoder.kMatchMinLen;
            _lenEncoder.Encode(_rangeEncoder, len - LzmaCoder.kMatchMinLen, posState);
            var posSlot = (1U << LzmaCoder.kNumPosSlotBits) - 1;
            var lenToPosState = LzmaCoder.GetLenToPosState(len);
            _posSlotEncoder[lenToPosState].Encode(_rangeEncoder, posSlot);
            int footerBits = 30;
            var posReduced = (1U << footerBits) - 1;
            _rangeEncoder.EncodeDirectBits(posReduced >> LzmaCoder.kNumAlignBits, footerBits - LzmaCoder.kNumAlignBits);
            _posAlignEncoder.ReverseEncode(_rangeEncoder, posReduced & LzmaCoder.kAlignMask);
        }
    }
}

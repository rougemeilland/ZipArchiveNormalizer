// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using SevenZip.Compression.Lzma.Decoder.RangeDecoder;
using System;
using System.Runtime.CompilerServices;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Decoder
{
    public class LzmaDecoder
    {
        public const Int32 PROPERTY_SIZE = 5;

        private const Int32 _LZMA_DIC_MIN = 1 << 12;
        private const UInt64 _MINIMUM_PROGRESS_STEP = 1024UL * 1024UL;

        private readonly OutWindow _outWindow;
        private readonly RangeDecoder.Decoder _rangeDecoder;
        private readonly BitDecoder[] _isMatchDecoders;
        private readonly BitDecoder[] _isRepDecoders;
        private readonly BitDecoder[] _isRepG0Decoders;
        private readonly BitDecoder[] _isRepG1Decoders;
        private readonly BitDecoder[] _isRepG2Decoders;
        private readonly BitDecoder[] _isRep0LongDecoders;
        private readonly BitTreeDecoder[] _posSlotDecoder;
        private readonly BitDecoder[] _posDecoders;
        private readonly BitTreeDecoder _posAlignDecoder;
        private readonly LenDecoder _lenDecoder;
        private readonly LenDecoder _repLenDecoder;
        private readonly LiteralDecoder _literalDecoder;
        private readonly UInt32 _dictionarySize;
        private readonly UInt32 _dictionarySizeCheck;
        private readonly UInt32 _posStateMask;

        public LzmaDecoder(ReadOnlySpan<Byte> properties)
        {
            if (properties.Length < PROPERTY_SIZE)
                throw new ArgumentException("Too short properties length", nameof(properties));
            if (properties[0] >= (9 * 5 * 5))
                throw new ArgumentException("Illegal property values", nameof(properties));

            _dictionarySize = UInt32.MaxValue;
            {
                var lc = properties[0] % 9;
                var remainder = properties[0] / 9;
                var lp = remainder % 5;
                var pb = remainder / 5;
                if (pb > LzmaConstants.kNumPosStatesBitsMax)
                    throw new ArgumentException("Illegal property values", nameof(properties));
                _dictionarySize = properties[1..].ToUInt32LE();
                _dictionarySizeCheck = _dictionarySize.Maximum(1U);
                {
                    var blockSize = _dictionarySizeCheck.Maximum(1U << 12);
                    _outWindow = new OutWindow(blockSize);
                }
                _literalDecoder = new LiteralDecoder(lp, lc);
                {
                    var numPosStates = 1U << pb;
                    _lenDecoder = new LenDecoder(numPosStates);
                    _repLenDecoder = new LenDecoder(numPosStates);
                    _posStateMask = numPosStates - 1;
                }
            }
            _rangeDecoder = new RangeDecoder.Decoder();
            _isMatchDecoders = new BitDecoder[LzmaConstants.kNumStates << LzmaConstants.kNumPosStatesBitsMax];
            _isMatchDecoders.FillArray(_ => new BitDecoder());
            _isRepDecoders = new BitDecoder[LzmaConstants.kNumStates];
            _isRepDecoders.FillArray(_ => new BitDecoder());
            _isRepG0Decoders = new BitDecoder[LzmaConstants.kNumStates];
            _isRepG0Decoders.FillArray(_ => new BitDecoder());
            _isRepG1Decoders = new BitDecoder[LzmaConstants.kNumStates];
            _isRepG1Decoders.FillArray(_ => new BitDecoder());
            _isRepG2Decoders = new BitDecoder[LzmaConstants.kNumStates];
            _isRepG2Decoders.FillArray(_ => new BitDecoder());
            _isRep0LongDecoders = new BitDecoder[LzmaConstants.kNumStates << LzmaConstants.kNumPosStatesBitsMax];
            _isRep0LongDecoders.FillArray(_ => new BitDecoder());
            _posSlotDecoder = new BitTreeDecoder[LzmaConstants.kNumLenToPosStates];
            _posSlotDecoder.FillArray(_ => new BitTreeDecoder(LzmaConstants.kNumPosSlotBits));
            _posDecoders = new BitDecoder[LzmaConstants.kNumFullDistances - LzmaConstants.kEndPosModelIndex];
            _posDecoders.FillArray(_ => new BitDecoder());
            _posAlignDecoder = new BitTreeDecoder(LzmaConstants.kNumAlignBits);
        }

        public void DeCode(IBasicInputByteStream inStream, IBasicOutputByteStream outStream, UInt64 outSize, IProgress<UInt64>? progress)
        {
            _rangeDecoder.Init(inStream);
            var state = new State();
            var rep0 = 0U;
            var rep1 = 0U;
            var rep2 = 0U;
            var rep3 = 0U;
            var nowPos64 = 0UL;
            var outSize64 = outSize;
            ReportProgress(progress, nowPos64);
            if (nowPos64 < outSize64)
            {
                if (_isMatchDecoders[state.Index << LzmaConstants.kNumPosStatesBitsMax].Decode(inStream, _rangeDecoder) != 0)
                    throw new SevenZipDataErrorException();
                state.UpdateChar();
                var b = _literalDecoder.DecodeNormal(inStream, _rangeDecoder, 0, 0);
                _outWindow.PutByte(outStream, b);
                nowPos64++;
                ReportProgress(progress, nowPos64);
            }
            while (nowPos64 < outSize64)
            {
                var posState = (UInt32)nowPos64 & _posStateMask;
                if (_isMatchDecoders[(state.Index << LzmaConstants.kNumPosStatesBitsMax) + posState].Decode(inStream, _rangeDecoder) == 0)
                {
                    var prevByte = _outWindow.GetByte(0);
                    var b = 
                        state.IsCharState()
                            ? _literalDecoder.DecodeNormal(
                                inStream,
                                _rangeDecoder,
                                (UInt32)nowPos64,
                                prevByte)
                            : _literalDecoder.DecodeWithMatchByte(
                                inStream,
                                _rangeDecoder,
                                (UInt32)nowPos64,
                                prevByte,
                                _outWindow.GetByte(rep0));
                    _outWindow.PutByte(outStream, b);
                    state.UpdateChar();
                    nowPos64++;
                }
                else if (_isRepDecoders[state.Index].Decode(inStream, _rangeDecoder) == 0)
                {
                    rep3 = rep2;
                    rep2 = rep1;
                    rep1 = rep0;
                    var len = LzmaConstants.kMatchMinLen + _lenDecoder.Decode(inStream, _rangeDecoder, posState);
                    state.UpdateMatch();
                    var posSlot = _posSlotDecoder[len.GetLenToPosState()].Decode(inStream, _rangeDecoder);
                    if (posSlot < LzmaConstants.kStartPosModelIndex)
                        rep0 = posSlot;
                    else if (posSlot < LzmaConstants.kEndPosModelIndex)
                    {
                        var numDirectBits = (Int32)((posSlot >> 1) - 1);
                        rep0 = (2U | (posSlot & 1)) << numDirectBits;
                        rep0 +=
                            BitTreeDecoder.ReverseDecode(
                                inStream,
                                _posDecoders,
                                rep0 - posSlot - 1,
                                _rangeDecoder,
                                numDirectBits);
                    }
                    else
                    {
                        var numDirectBits = (Int32)((posSlot >> 1) - 1);
                        rep0 = (2U | (posSlot & 1)) << numDirectBits;
                        rep0 += _rangeDecoder.DecodeDirectBits(inStream, numDirectBits - LzmaConstants.kNumAlignBits) << LzmaConstants.kNumAlignBits;
                        rep0 += _posAlignDecoder.ReverseDecode(inStream, _rangeDecoder);
                    }
                    if (rep0 >= nowPos64 || rep0 >= _dictionarySizeCheck)
                    {
                        if (rep0 != UInt32.MaxValue)
                            throw new SevenZipDataErrorException();
                        break;
                    }
                    _outWindow.CopyBlock(outStream, rep0, len);
                    nowPos64 += len;
                }
                else if (_isRepG0Decoders[state.Index].Decode(inStream, _rangeDecoder) == 0)
                {
                    if (_isRep0LongDecoders[(state.Index << LzmaConstants.kNumPosStatesBitsMax) + posState].Decode(inStream, _rangeDecoder) != 0)
                    {
                        var len = _repLenDecoder.Decode(inStream, _rangeDecoder, posState) + LzmaConstants.kMatchMinLen;
                        state.UpdateRep();
                        if (rep0 >= nowPos64 || rep0 >= _dictionarySizeCheck)
                        {
                            if (rep0 != UInt32.MaxValue)
                                throw new SevenZipDataErrorException();
                            break;
                        }
                        _outWindow.CopyBlock(outStream, rep0, len);
                        nowPos64 += len;
                    }
                    else
                    {
                        state.UpdateShortRep();
                        _outWindow.PutByte(outStream, _outWindow.GetByte(rep0));
                        nowPos64++;
                    }
                }
                else
                {
                    if (_isRepG1Decoders[state.Index].Decode(inStream, _rangeDecoder) == 0)
                    {
                        var temp = rep1;
                        rep1 = rep0;
                        rep0 = temp;
                    }
                    else if (_isRepG2Decoders[state.Index].Decode(inStream, _rangeDecoder) == 0)
                    {
                        var temp = rep2;
                        rep2 = rep1;
                        rep1 = rep0;
                        rep0 = temp;
                    }
                    else
                    {
                        var temp = rep3;
                        rep3 = rep2;
                        rep2 = rep1;
                        rep1 = rep0;
                        rep0 = temp;
                    }
                    var len = _repLenDecoder.Decode(inStream, _rangeDecoder, posState) + LzmaConstants.kMatchMinLen;
                    state.UpdateRep();
                    if (rep0 >= nowPos64 || rep0 >= _dictionarySizeCheck)
                    {
                        if (rep0 != UInt32.MaxValue)
                            throw new SevenZipDataErrorException();
                        break;
                    }
                    _outWindow.CopyBlock(outStream, rep0, len);
                    nowPos64 += len;
                }
                ReportProgress(progress, nowPos64);
            }
            _outWindow.Flush(outStream);
            ReportProgress(progress, nowPos64);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReportProgress(IProgress<UInt64>? progress, UInt64 processedCount)
        {
            try
            {
                progress?.Report(processedCount);
            }
            catch (Exception)
            {
            }
        }
    }
}

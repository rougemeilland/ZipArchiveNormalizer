// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using SevenZip.Compression.Lz;
using SevenZip.Compression.Lzma.Encoder.RangeEncoder;
using System;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Encoder
{
    public class LzmaEncoder
    {
        public const Int32 PROPERTY_SIZE = 5;

        private const UInt32 kIfinityPrice = 0x0FFFFFFF;
        private const Int32 kDefaultDictionaryLogSize = 22;
        private const UInt32 kNumFastBytesDefault = 0x20;
        private const UInt32 kNumLenSpecSymbols = LzmaConstants.kNumLowLenSymbols + LzmaConstants.kNumMidLenSymbols;
        private const UInt32 kNumOpts = 1 << 12;

        private static readonly Byte[] _fastPos;

        private readonly UInt32[] _repDistances;
        private readonly Optimal[] _optimum;
        private readonly IMatchFinder _matchFinder;
        private readonly RangeEncoder.Encoder _rangeEncoder;
        private readonly BitEncoder[] _isMatch;
        private readonly BitEncoder[] _isRep;
        private readonly BitEncoder[] _isRepG0;
        private readonly BitEncoder[] _isRepG1;
        private readonly BitEncoder[] _isRepG2;
        private readonly BitEncoder[] _isRep0Long;
        private readonly BitTreeEncoder[] _posSlotEncoder;
        private readonly BitEncoder[] _posEncoders;
        private readonly BitTreeEncoder _posAlignEncoder;
        private readonly LenPriceTableEncoder _lenEncoder;
        private readonly LenPriceTableEncoder _repMatchLenEncoder;
        private readonly LiteralEncoder _literalEncoder;
        private readonly UInt32[] _matchDistances;
        private readonly UInt32 _numFastBytes;
        private readonly UInt32[] _posSlotPrices;
        private readonly UInt32[] _distancesPrices;
        private readonly UInt32[] _alignPrices;
        private readonly UInt32[] _tempPrices;
        private readonly UInt32[] _reps;
        private readonly UInt32[] _repLens;
        private readonly UInt32 _distTableSize;
        private readonly Int32 _posStateBits;
        private readonly UInt32 _posStateMask;
        private readonly Int32 _numLiteralPosStateBits;
        private readonly Int32 _numLiteralContextBits;
        private readonly UInt32 _dictionarySize;
        private readonly bool _writeEndMark;
        //private readonly bool _fastMode;

        private State _state;
        private Byte _previousByte;
        private (UInt32 longestMatchLength, UInt32 numDistancePairs)? _longestMatch;
        private UInt32 _additionalOffset;
        private UInt32 _optimumEndIndex;
        private UInt32 _optimumCurrentIndex;
        private UInt32 _alignPriceCount;
        private UInt64 _nowPos64;
        private UInt32 _matchPriceCount;

        static LzmaEncoder()
        {
            _fastPos = new Byte[1 << 11];
            const Byte kFastSlots = 22;
            _fastPos[0] = 0;
            _fastPos[1] = 1;
            var c = 2;
            for (Byte slotFast = 2; slotFast < kFastSlots; ++slotFast)
            {
                var k = 1 << ((slotFast >> 1) - 1);
                for (var j = 0; j < k; ++j)
                    _fastPos[c++] = slotFast;
            }
        }

        public LzmaEncoder(LzmaEncoderProperties properties)
        {
            if (!properties.NumFastBytes.IsBetween(5, (Int32)LzmaConstants.kMatchMaxLen))
                throw new ArgumentException($"Illegal value: {nameof(properties.NumFastBytes)}", nameof(properties));
            _numFastBytes = (UInt32)properties.NumFastBytes;
            {
                const Int32 kDicLogSizeMaxCompress = 30;
                var dictionarySize = properties.DictionarySize;
                if (!dictionarySize.IsBetween(1 << LzmaConstants.kDicLogSizeMin, 1 << kDicLogSizeMaxCompress))
                    throw new ArgumentException($"Illegal value: {nameof(properties.DictionarySize)}", nameof(properties));
                _dictionarySize = (UInt32)dictionarySize;
                var dicLogSize = 0;
                while (dicLogSize < (UInt32)kDicLogSizeMaxCompress)
                {
                    if (dictionarySize <= (1U << dicLogSize))
                        break;
                    ++dicLogSize;
                }
                _distTableSize = (UInt32)dicLogSize * 2;
            }
            if (!properties.PosStateBits.IsBetween(0, LzmaConstants.kNumPosStatesBitsEncodingMax))
                throw new ArgumentException($"Illegal value: {nameof(properties.PosStateBits)}", nameof(properties));
            _posStateBits = properties.PosStateBits;
            _posStateMask = (1U << _posStateBits) - 1;
            if (!properties.LitPosBits.IsBetween(0, LzmaConstants.kNumLitPosStatesBitsEncodingMax))
                throw new ArgumentException($"Illegal value: {nameof(properties.LitPosBits)}", nameof(properties));
            _numLiteralPosStateBits = properties.LitPosBits;
            if (!properties.LitContextBits.IsBetween(0, LzmaConstants.kNumLitContextBitsMax))
                throw new ArgumentException($"Illegal value: {nameof(properties.LitContextBits)}", nameof(properties));
            _numLiteralContextBits = properties.LitContextBits;
            //_fastMode = !properties.Algorithm;
            _writeEndMark = properties.EndMarker;
            _state = new State();
            _repDistances = new UInt32[LzmaConstants.kNumRepDistances];
            _repDistances.ClearArray();
            _optimum = new Optimal[kNumOpts];
            _optimum.FillArray(_ => new Optimal());
            _rangeEncoder = new RangeEncoder.Encoder();
            _isMatch = new BitEncoder[LzmaConstants.kNumStates << LzmaConstants.kNumPosStatesBitsMax];
            _isMatch.FillArray(_ => new BitEncoder());
            _isRep = new BitEncoder[LzmaConstants.kNumStates];
            _isRep.FillArray(_ => new BitEncoder());
            _isRepG0 = new BitEncoder[LzmaConstants.kNumStates];
            _isRepG0.FillArray(_ => new BitEncoder());
            _isRepG1 = new BitEncoder[LzmaConstants.kNumStates];
            _isRepG1.FillArray(_ => new BitEncoder());
            _isRepG2 = new BitEncoder[LzmaConstants.kNumStates];
            _isRepG2.FillArray(_ => new BitEncoder());
            _isRep0Long = new BitEncoder[LzmaConstants.kNumStates << LzmaConstants.kNumPosStatesBitsMax];
            _isRep0Long.FillArray(_ => new BitEncoder());
            _posSlotEncoder = new BitTreeEncoder[LzmaConstants.kNumLenToPosStates];
            _posSlotEncoder.FillArray(_ => new BitTreeEncoder(LzmaConstants.kNumPosSlotBits));
            _posEncoders = new BitEncoder[LzmaConstants.kNumFullDistances - LzmaConstants.kEndPosModelIndex];
            _posEncoders.FillArray(_ => new BitEncoder());
            _posAlignEncoder = new BitTreeEncoder(LzmaConstants.kNumAlignBits);
            _matchDistances = new UInt32[LzmaConstants.kMatchMaxLen * 2 + 2];
            _numFastBytes = kNumFastBytesDefault;
            _posSlotPrices = new UInt32[1 << (LzmaConstants.kNumPosSlotBits + LzmaConstants.kNumLenToPosStatesBits)];
            _distancesPrices = new UInt32[LzmaConstants.kNumFullDistances << LzmaConstants.kNumLenToPosStatesBits];
            _alignPrices = new UInt32[LzmaConstants.kAlignTableSize];
            _distTableSize = kDefaultDictionaryLogSize * 2;
            _tempPrices = new UInt32[LzmaConstants.kNumFullDistances];
            _reps = new UInt32[LzmaConstants.kNumRepDistances];
            _repLens = new UInt32[LzmaConstants.kNumRepDistances];
            _writeEndMark = false;
            _literalEncoder = new LiteralEncoder(_numLiteralPosStateBits, _numLiteralContextBits);
            _matchFinder =
                CreateMatchFinderObject(
                    properties.MatchFinder,
                    _dictionarySize,
                    kNumOpts,
                    _numFastBytes,
                    LzmaConstants.kMatchMaxLen + 1,
                    properties.MatchFinderCycles);
            _previousByte = 0;
            _lenEncoder = new LenPriceTableEncoder(1U << _posStateBits, _numFastBytes + 1 - LzmaConstants.kMatchMinLen);
            _repMatchLenEncoder = new LenPriceTableEncoder(1U << _posStateBits, _numFastBytes + 1 - LzmaConstants.kMatchMinLen);
            _longestMatch = null;
            _optimumEndIndex = 0;
            _optimumCurrentIndex = 0;
            _additionalOffset = 0;
            _alignPriceCount = 0;
            _nowPos64 = 0;
            _matchPriceCount = 0;
        }

        public void WriteCoderProperties(IBasicOutputByteStream outStream)
        {
            outStream.WriteByte((Byte)((_posStateBits * 5 + _numLiteralPosStateBits) * 9 + _numLiteralContextBits));
            outStream.WriteUInt32LE(_dictionarySize);
        }

        public void Encode(IBasicInputByteStream inStream, IBasicOutputByteStream outStream, IProgress<UInt64>? progress)
        {
            // if (_fastMode)
            {
                FillDistancesPrices();
                FillAlignPrices();
            }
            _lenEncoder.UpdateTables(1U << _posStateBits);
            _repMatchLenEncoder.UpdateTables(1U << _posStateBits);
            _nowPos64 = 0;
            while (true)
            {
                if (CodeOneBlock(inStream, outStream, out _, out UInt64 processedOutSize))
                    break;
                try
                {
                    progress?.Report(processedOutSize);
                }
                catch (Exception)
                {
                }
            }
        }

        private static IMatchFinder CreateMatchFinderObject(MatchFinderType matchFinderType, UInt32 historySize, UInt32 keepAddBufferBefore, UInt32 matchMaxLen, UInt32 keepAddBufferAfter, UInt32 matchFinderCycles)
        {
            return
                matchFinderType switch
                {
                    MatchFinderType.BT2 => new Bt2MatchFinder(historySize, keepAddBufferBefore, matchMaxLen, keepAddBufferAfter, matchFinderCycles),
                    // MatchFinderType.BT3 => new Bt3MatchFinder(historySize, keepAddBufferBefore, matchMaxLen, keepAddBufferAfter, matchFinderCycles), // not used as only BT2 and BT4 are supported
                    MatchFinderType.BT4 => new Bt4MatchFinder(historySize, keepAddBufferBefore, matchMaxLen, keepAddBufferAfter, matchFinderCycles),
                    // MatchFinderType.BT5 => new Bt5MatchFinder(historySize, keepAddBufferBefore, matchMaxLen, keepAddBufferAfter, matchFinderCycles), // not used as only BT2 and BT4 are supported
                    // MatchFinderType.HC4 => new Hc4MatchFinder(historySize, keepAddBufferBefore, matchMaxLen, keepAddBufferAfter, matchFinderCycles), // not used as only BT2 and BT4 are supported
                    // MatchFinderType.HC5 => new Hc5MatchFinder(historySize, keepAddBufferBefore, matchMaxLen, keepAddBufferAfter, matchFinderCycles), // not used as only BT2 and BT4 are supported
                    _ => throw new ArgumentException($"{nameof(MatchFinderType)}.{matchFinderType} is not supported", nameof(matchFinderType)),
                };
        }

        private void FillDistancesPrices()
        {
            for (var i = LzmaConstants.kStartPosModelIndex; i < LzmaConstants.kNumFullDistances; i++)
            {
                var posSlot = GetPosSlot(i);
                var footerBits = (Int32)((posSlot >> 1) - 1);
                var baseVal = (2 | (posSlot & 1)) << footerBits;
                _tempPrices[i] =
                    BitTreeEncoder.ReverseGetPrice(
                        _posEncoders,
                        baseVal - posSlot - 1,
                        footerBits,
                        i - baseVal);
            }
            for (var lenToPosState = 0U; lenToPosState < LzmaConstants.kNumLenToPosStates; lenToPosState++)
            {
                var encoder = _posSlotEncoder[lenToPosState];
                var st = lenToPosState << LzmaConstants.kNumPosSlotBits;
                for (var posSlot = 0U; posSlot < _distTableSize; posSlot++)
                    _posSlotPrices[st + posSlot] = encoder.GetPrice(posSlot);
                for (var posSlot = LzmaConstants.kEndPosModelIndex; posSlot < _distTableSize; posSlot++)
                    _posSlotPrices[st + posSlot] += ((posSlot >> 1) - 1 - LzmaConstants.kNumAlignBits) << BitEncoder.kNumBitPriceShiftBits;
                var st2 = lenToPosState * LzmaConstants.kNumFullDistances;
                var i = 0U;
                while (i < LzmaConstants.kStartPosModelIndex)
                {
                    _distancesPrices[st2 + i] = _posSlotPrices[st + i];
                    ++i;
                }
                while (i < LzmaConstants.kNumFullDistances)
                {
                    _distancesPrices[st2 + i] = _posSlotPrices[st + GetPosSlot(i)] + _tempPrices[i];
                    ++i;
                }
            }
            _matchPriceCount = 0;
        }

        private void FillAlignPrices()
        {
            for (var i = 0U; i < LzmaConstants.kAlignTableSize; i++)
                _alignPrices[i] = _posAlignEncoder.ReverseGetPrice(i);
            _alignPriceCount = 0;
        }

        private bool CodeOneBlock(IBasicInputByteStream inStream, IBasicOutputByteStream outStream, out UInt64 inSize, out UInt64 outSize)
        {
            inSize = 0;
            outSize = 0;
            var progressPosValuePrev = _nowPos64;
            if (_nowPos64 == 0)
            {
                if (_matchFinder.NumAvailableBytes == 0)
                {
                    Flush(outStream, (UInt32)_nowPos64);
                    return true;
                }
                ReadMatchDistances(inStream);
                var posState = (UInt32)_nowPos64 & _posStateMask;
                _isMatch[(_state.Index << LzmaConstants.kNumPosStatesBitsMax) + posState].Encode(outStream, _rangeEncoder, 0);
                _state.UpdateChar();
                var curByte = _matchFinder.CurrentPos[(Int32)(0 - _additionalOffset)];
                _literalEncoder.GetSubCoder((UInt32)_nowPos64, _previousByte).Encode(outStream, _rangeEncoder, curByte);
                _previousByte = curByte;
                _additionalOffset--;
                _nowPos64++;
            }
            if (_matchFinder.NumAvailableBytes == 0)
            {
                Flush(outStream, (UInt32)_nowPos64);
                return true;
            }
            while (true)
            {
#if true
                var (len, pos) = GetOptimum(inStream, (UInt32)_nowPos64);
#else
                var (len, pos) = _fastMode ? GetOptimum(inStream, (UInt32)_nowPos64) : GetOptimumFast(inStream);
#endif
                var posState = ((UInt32)_nowPos64) & _posStateMask;
                var complexState = (_state.Index << LzmaConstants.kNumPosStatesBitsMax) + posState;
                if (len == 1 && pos == UInt32.MaxValue)
                {
                    _isMatch[complexState].Encode(outStream, _rangeEncoder, 0);
                    var curByte = _matchFinder.CurrentPos[(Int32)(0 - _additionalOffset)];
                    var subCoder = _literalEncoder.GetSubCoder((UInt32)_nowPos64, _previousByte);
                    if (!_state.IsCharState())
                    {
                        var matchByte = _matchFinder.CurrentPos[(Int32)(0 - _repDistances[0] - 1 - _additionalOffset)];
                        subCoder.EncodeMatched(outStream, _rangeEncoder, matchByte, curByte);
                    }
                    else
                        subCoder.Encode(outStream, _rangeEncoder, curByte);
                    _previousByte = curByte;
                    _state.UpdateChar();
                }
                else
                {
                    _isMatch[complexState].Encode(outStream, _rangeEncoder, 1);
                    if (pos < LzmaConstants.kNumRepDistances)
                    {
                        _isRep[_state.Index].Encode(outStream, _rangeEncoder, 1);
                        if (pos == 0)
                        {
                            _isRepG0[_state.Index].Encode(outStream, _rangeEncoder, 0);
                            if (len == 1)
                                _isRep0Long[complexState].Encode(outStream, _rangeEncoder, 0);
                            else
                                _isRep0Long[complexState].Encode(outStream, _rangeEncoder, 1);
                        }
                        else
                        {
                            _isRepG0[_state.Index].Encode(outStream, _rangeEncoder, 1);
                            if (pos == 1)
                                _isRepG1[_state.Index].Encode(outStream, _rangeEncoder, 0);
                            else
                            {
                                _isRepG1[_state.Index].Encode(outStream, _rangeEncoder, 1);
                                _isRepG2[_state.Index].Encode(outStream, _rangeEncoder, pos - 2);
                            }
                        }
                        if (len == 1)
                            _state.UpdateShortRep();
                        else
                        {
                            _repMatchLenEncoder.Encode(outStream, _rangeEncoder, len - LzmaConstants.kMatchMinLen, posState);
                            _state.UpdateRep();
                        }
                        if (pos != 0)
                        {
                            var distance = _repDistances[pos];
                            for (var i = pos; i >= 1; i--)
                                _repDistances[i] = _repDistances[i - 1];
                            _repDistances[0] = distance;
                        }
                    }
                    else
                    {
                        _isRep[_state.Index].Encode(outStream, _rangeEncoder, 0);
                        _state.UpdateMatch();
                        _lenEncoder.Encode(outStream, _rangeEncoder, len - LzmaConstants.kMatchMinLen, posState);
                        pos -= LzmaConstants.kNumRepDistances;
                        var posSlot = GetPosSlot(pos);
                        var lenToPosState = len.GetLenToPosState(); ;
                        _posSlotEncoder[lenToPosState].Encode(outStream, _rangeEncoder, posSlot);
                        if (posSlot >= LzmaConstants.kStartPosModelIndex)
                        {
                            var footerBits = (Int32)((posSlot >> 1) - 1);
                            var baseVal = (2 | (posSlot & 1)) << footerBits;
                            var posReduced = pos - baseVal;
                            if (posSlot < LzmaConstants.kEndPosModelIndex)
                                BitTreeEncoder.ReverseEncode(outStream, _posEncoders, baseVal - posSlot - 1, _rangeEncoder, footerBits, posReduced);
                            else
                            {
                                _rangeEncoder.EncodeDirectBits(outStream, posReduced >> LzmaConstants.kNumAlignBits, footerBits - LzmaConstants.kNumAlignBits);
                                _posAlignEncoder.ReverseEncode(outStream, _rangeEncoder, posReduced & LzmaConstants.kAlignMask);
                                _alignPriceCount++;
                            }
                        }
                        var distance = pos;
                        for (var i = LzmaConstants.kNumRepDistances - 1; i >= 1; i--)
                            _repDistances[i] = _repDistances[i - 1];
                        _repDistances[0] = distance;
                        _matchPriceCount++;
                    }
                    _previousByte = _matchFinder.CurrentPos[(Int32)(len - 1 - _additionalOffset)];
                }
                _additionalOffset -= len;
                _nowPos64 += len;
                if (_additionalOffset == 0)
                {
                    // if (!_fastMode)
                    {
                        if (_matchPriceCount >= (1 << 7))
                            FillDistancesPrices();
                        if (_alignPriceCount >= LzmaConstants.kAlignTableSize)
                            FillAlignPrices();
                    }
                    inSize = _nowPos64;
                    outSize = _rangeEncoder.ProcessedSizeAdd;
                    if (_matchFinder.NumAvailableBytes == 0)
                    {
                        Flush(outStream, (UInt32)_nowPos64);
                        return true;
                    }
                    if (_nowPos64 - progressPosValuePrev >= (1 << 12))
                        return false;
                }
            }
        }

        private (UInt32 len, UInt32 numDistancePairs) ReadMatchDistances(IBasicInputByteStream inStream)
        {
            var lenRes = 0U;
            var numDistancePairs = _matchFinder.GetMatches(inStream, _matchDistances);
            if (numDistancePairs > 0)
            {
                lenRes = _matchDistances[numDistancePairs - 2];
                if (lenRes == _numFastBytes)
                {
                    lenRes +=
                        _matchFinder.GetMatchLen(
                            (Int32)lenRes - 1,
                            _matchDistances[numDistancePairs - 1],
                            LzmaConstants.kMatchMaxLen - lenRes);
                }
            }
            _additionalOffset++;
            return (lenRes, numDistancePairs);
        }

        private (UInt32 len, UInt32 back) GetOptimum(IBasicInputByteStream inStream, UInt32 position)
        {
            if (_optimumEndIndex != _optimumCurrentIndex)
            {
                var lenRes = _optimum[_optimumCurrentIndex].PosPrev - _optimumCurrentIndex;
                var backRes = _optimum[_optimumCurrentIndex].BackPrev;
                _optimumCurrentIndex = _optimum[_optimumCurrentIndex].PosPrev;
                return (lenRes, backRes);
            }
            _optimumCurrentIndex = 0;
            _optimumEndIndex = 0;
            UInt32 lenMain;
            UInt32 numDistancePairs;
            if (_longestMatch.HasValue)
            {
                (lenMain, numDistancePairs) = _longestMatch.Value;
                _longestMatch = null;
            }
            else
                (lenMain, numDistancePairs) = ReadMatchDistances(inStream);
            var numAvailableBytes = _matchFinder.NumAvailableBytes + 1;
            if (numAvailableBytes < 2)
                return (1, UInt32.MaxValue);
            var repMaxIndex = 0U;
            for (var i = 0U; i < LzmaConstants.kNumRepDistances; i++)
            {
                _reps[i] = _repDistances[i];
                _repLens[i] = _matchFinder.GetMatchLen(-1, _reps[i], LzmaConstants.kMatchMaxLen);
                if (_repLens[i] > _repLens[repMaxIndex])
                    repMaxIndex = i;
            }
            if (_repLens[repMaxIndex] >= _numFastBytes)
            {
                var backRes = repMaxIndex;
                var lenRes = _repLens[repMaxIndex];
                MovePos(inStream, lenRes - 1);
                return (lenRes, backRes);
            }
            if (lenMain >= _numFastBytes)
            {
                var backRes = _matchDistances[numDistancePairs - 1] + LzmaConstants.kNumRepDistances;
                MovePos(inStream, lenMain - 1);
                return (lenMain, backRes);
            }
            var currentByte = _matchFinder.CurrentPos[-1];
            var matchByte = _matchFinder.CurrentPos[(Int32)(0 - _repDistances[0] - 1 - 1)];
            if (lenMain < 2 && currentByte != matchByte && _repLens[repMaxIndex] < 2)
                return (1, UInt32.MaxValue);
            _optimum[0].State = _state;
            var posState = position & _posStateMask;
            _optimum[1].Price = _isMatch[(_state.Index << LzmaConstants.kNumPosStatesBitsMax) + posState].GetPrice0() +
                    _literalEncoder.GetSubCoder(position, _previousByte).GetPrice(!_state.IsCharState(), matchByte, currentByte);
            _optimum[1].MakeAsChar();
            var matchPrice = _isMatch[(_state.Index << LzmaConstants.kNumPosStatesBitsMax) + posState].GetPrice1();
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
            var lenEnd = (lenMain >= _repLens[repMaxIndex]) ? lenMain : _repLens[repMaxIndex];
            if (lenEnd < 2)
                return (1, _optimum[1].BackPrev);
            _optimum[1].PosPrev = 0;
            _optimum[0].Backs0 = _reps[0];
            _optimum[0].Backs1 = _reps[1];
            _optimum[0].Backs2 = _reps[2];
            _optimum[0].Backs3 = _reps[3];
            var len = lenEnd;
            do
                _optimum[len--].Price = kIfinityPrice;
            while (len >= 2);
            for (var i = 0U; i < LzmaConstants.kNumRepDistances; i++)
            {
                var repLen = _repLens[i];
                if (repLen >= 2)
                {
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
            }
            var normalMatchPrice = matchPrice + _isRep[_state.Index].GetPrice0();
            len = (_repLens[0] >= 2) ? _repLens[0] + 1 : 2;
            if (len <= lenMain)
            {
                var offs = 0U;
                while (len > _matchDistances[offs])
                    offs += 2;
                while (true)
                {
                    var distance = _matchDistances[offs + 1];
                    var curAndLenPrice = normalMatchPrice + GetPosLenPrice(distance, len, posState);
                    var optimum = _optimum[len];
                    if (curAndLenPrice < optimum.Price)
                    {
                        optimum.Price = curAndLenPrice;
                        optimum.PosPrev = 0;
                        optimum.BackPrev = distance + LzmaConstants.kNumRepDistances;
                        optimum.Prev1IsChar = false;
                    }
                    if (len == _matchDistances[offs])
                    {
                        offs += 2;
                        if (offs == numDistancePairs)
                            break;
                    }
                    ++len;
                }
            }
            var cur = 0U;
            while (true)
            {
                cur++;
                if (cur == lenEnd)
                    return Backward(cur);
                (var newLen, numDistancePairs) = ReadMatchDistances(inStream);
                if (newLen >= _numFastBytes)
                {
                    _longestMatch = (newLen, numDistancePairs);
                    return Backward(cur);
                }
                position++;
                var posPrev = _optimum[cur].PosPrev;
                State state;
                if (_optimum[cur].Prev1IsChar)
                {
                    posPrev--;
                    if (_optimum[cur].Prev2)
                    {
                        state = _optimum[_optimum[cur].PosPrev2].State;
                        if (_optimum[cur].BackPrev2 < LzmaConstants.kNumRepDistances)
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
                        if (pos < LzmaConstants.kNumRepDistances)
                            state.UpdateRep();
                        else
                            state.UpdateMatch();
                    }
                    var opt = _optimum[posPrev];
                    if (pos < LzmaConstants.kNumRepDistances)
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
                        _reps[0] = pos - LzmaConstants.kNumRepDistances;
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
                currentByte = _matchFinder.CurrentPos[-1];
                matchByte = _matchFinder.CurrentPos[(Int32)(0 - _reps[0] - 1 - 1)];
                posState = position & _posStateMask;
                var curAnd1Price =
                    curPrice
                    + _isMatch[(state.Index << LzmaConstants.kNumPosStatesBitsMax) + posState].GetPrice0()
                    + _literalEncoder
                        .GetSubCoder(position, _matchFinder.CurrentPos[-2])
                        .GetPrice(!state.IsCharState(), matchByte, currentByte);
                var nextOptimum = _optimum[cur + 1];
                var nextIsChar = false;
                if (curAnd1Price < nextOptimum.Price)
                {
                    nextOptimum.Price = curAnd1Price;
                    nextOptimum.PosPrev = cur;
                    nextOptimum.MakeAsChar();
                    nextIsChar = true;
                }
                matchPrice = curPrice + _isMatch[(state.Index << LzmaConstants.kNumPosStatesBitsMax) + posState].GetPrice1();
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
                var numAvailableBytesFull = _matchFinder.NumAvailableBytes + 1;
                numAvailableBytesFull = (kNumOpts - 1 - cur).Minimum(numAvailableBytesFull);
                numAvailableBytes = numAvailableBytesFull;
                if (numAvailableBytes >= 2)
                {
                    if (numAvailableBytes > _numFastBytes)
                        numAvailableBytes = _numFastBytes;
                    if (!nextIsChar && matchByte != currentByte)
                    {
                        // try Literal + rep0
                        var t = (numAvailableBytesFull - 1).Minimum(_numFastBytes);
                        var lenTest2 = _matchFinder.GetMatchLen(0, _reps[0], t);
                        if (lenTest2 >= 2)
                        {
                            var state2 = state;
                            state2.UpdateChar();
                            var posStateNext = (position + 1) & _posStateMask;
                            var nextRepMatchPrice =
                                curAnd1Price
                                + _isMatch[(state2.Index << LzmaConstants.kNumPosStatesBitsMax) + posStateNext].GetPrice1()
                                + _isRep[state2.Index].GetPrice1();
                            var offset = cur + 1 + lenTest2;
                            while (lenEnd < offset)
                                _optimum[++lenEnd].Price = kIfinityPrice;
                            var curAndLenPrice =
                                nextRepMatchPrice
                                + GetRepPrice(0, lenTest2, state2, posStateNext);
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
                    for (var repIndex = 0U; repIndex < LzmaConstants.kNumRepDistances; repIndex++)
                    {
                        var lenTest = _matchFinder.GetMatchLen(-1, _reps[repIndex], numAvailableBytes);
                        if (lenTest >= 2)
                        {
                            var lenTestTemp = lenTest;
                            do
                            {
                                while (lenEnd < cur + lenTest)
                                    _optimum[++lenEnd].Price = kIfinityPrice;
                                var curAndLenPrice =
                                    repMatchPrice
                                    + GetRepPrice(repIndex, lenTest, state, posState);
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
                            if (lenTest < numAvailableBytesFull)
                            {
                                var t = (numAvailableBytesFull - 1 - lenTest).Minimum(_numFastBytes);
                                var lenTest2 = _matchFinder.GetMatchLen((Int32)lenTest, _reps[repIndex], t);
                                if (lenTest2 >= 2)
                                {
                                    var state2 = state;
                                    state2.UpdateRep();
                                    var posStateNext = (position + lenTest) & _posStateMask;
                                    var curAndLenCharPrice =
                                        repMatchPrice
                                        + GetRepPrice(repIndex, lenTest, state, posState)
                                        + _isMatch[(state2.Index << LzmaConstants.kNumPosStatesBitsMax) + posStateNext].GetPrice0()
                                        + _literalEncoder
                                            .GetSubCoder(
                                                position + lenTest,
                                                _matchFinder.CurrentPos[(Int32)lenTest - 1 - 1])
                                            .GetPrice(
                                                true,
                                                _matchFinder.CurrentPos[(Int32)lenTest - 1 - (Int32)(_reps[repIndex] + 1)],
                                                _matchFinder.CurrentPos[(Int32)lenTest - 1]);
                                    state2.UpdateChar();
                                    posStateNext = (position + lenTest + 1) & _posStateMask;
                                    var nextMatchPrice = curAndLenCharPrice + _isMatch[(state2.Index << LzmaConstants.kNumPosStatesBitsMax) + posStateNext].GetPrice1();
                                    var nextRepMatchPrice = nextMatchPrice + _isRep[state2.Index].GetPrice1();
                                    var offset = lenTest + 1 + lenTest2;
                                    while (lenEnd < cur + offset)
                                        _optimum[++lenEnd].Price = kIfinityPrice;
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
                    if (newLen > numAvailableBytes)
                    {
                        newLen = numAvailableBytes;
                        numDistancePairs = 0;
                        while (_matchDistances[numDistancePairs] < newLen)
                            numDistancePairs += 2;
                        _matchDistances[numDistancePairs] = newLen;
                        numDistancePairs += 2;
                    }
                    if (newLen >= startLen)
                    {
                        normalMatchPrice = matchPrice + _isRep[state.Index].GetPrice0();
                        while (lenEnd < cur + newLen)
                            _optimum[++lenEnd].Price = kIfinityPrice;
                        var offs = 0U;
                        while (startLen > _matchDistances[offs])
                            offs += 2;
                        for (var lenTest = startLen; ; lenTest++)
                        {
                            var curBack = _matchDistances[offs + 1];
                            var curAndLenPrice =
                                normalMatchPrice
                                + GetPosLenPrice(curBack, lenTest, posState);
                            var optimum = _optimum[cur + lenTest];
                            if (curAndLenPrice < optimum.Price)
                            {
                                optimum.Price = curAndLenPrice;
                                optimum.PosPrev = cur;
                                optimum.BackPrev = curBack + LzmaConstants.kNumRepDistances;
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
                                        var state2 = state;
                                        state2.UpdateMatch();
                                        var posStateNext = (position + lenTest) & _posStateMask;
                                        var curAndLenCharPrice =
                                            curAndLenPrice
                                            + _isMatch[(state2.Index << LzmaConstants.kNumPosStatesBitsMax) + posStateNext].GetPrice0()
                                            + _literalEncoder
                                                .GetSubCoder(
                                                    position + lenTest,
                                                    _matchFinder.CurrentPos[(Int32)lenTest - 1 - 1])
                                                .GetPrice(
                                                    true,
                                                    _matchFinder.CurrentPos[(Int32)lenTest - (Int32)(curBack + 1) - 1],
                                                    _matchFinder.CurrentPos[(Int32)lenTest - 1]);
                                        state2.UpdateChar();
                                        posStateNext = (position + lenTest + 1) & _posStateMask;
                                        var nextMatchPrice =
                                            curAndLenCharPrice
                                            + _isMatch[(state2.Index << LzmaConstants.kNumPosStatesBitsMax) + posStateNext].GetPrice1();
                                        var nextRepMatchPrice = nextMatchPrice + _isRep[state2.Index].GetPrice1();
                                        var offset = lenTest + 1 + lenTest2;
                                        while (lenEnd < cur + offset)
                                            _optimum[++lenEnd].Price = kIfinityPrice;
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
                                            optimum.BackPrev2 = curBack + LzmaConstants.kNumRepDistances;
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
        }

        private UInt32 GetRepLen1Price(State state, UInt32 posState)
        {
            return
                _isRepG0[state.Index].GetPrice0()
                + _isRep0Long[(state.Index << LzmaConstants.kNumPosStatesBitsMax) + posState].GetPrice0();
        }

        private UInt32 GetRepPrice(UInt32 repIndex, UInt32 len, State state, UInt32 posState)
        {
            return
                _repMatchLenEncoder.GetPrice(len - LzmaConstants.kMatchMinLen, posState)
                + GetPureRepPrice(repIndex, state, posState);
        }

        private UInt32 GetPureRepPrice(UInt32 repIndex, State state, UInt32 posState)
        {
#if DEBUG
            if (!repIndex.IsBetween(0U, 3U))
                throw new Exception();
#endif
            if (repIndex == 0)
            {
                return
                    _isRepG0[state.Index].GetPrice0()
                    + _isRep0Long[(state.Index << LzmaConstants.kNumPosStatesBitsMax) + posState].GetPrice1();
            }
            else if (repIndex == 1)
            {
                return
                    _isRepG0[state.Index].GetPrice1()
                    + _isRepG1[state.Index].GetPrice0();
            }
            else
            {
                return
                    _isRepG0[state.Index].GetPrice1()
                    + _isRepG1[state.Index].GetPrice1()
                    + _isRepG2[state.Index].GetPrice(repIndex - 2);
            }
        }

        private UInt32 GetPosLenPrice(UInt32 pos, UInt32 len, UInt32 posState)
        {
            var lenToPosState = len.GetLenToPosState();
            if (pos < LzmaConstants.kNumFullDistances)
            {
                return
                    _distancesPrices[(lenToPosState * LzmaConstants.kNumFullDistances) + pos]
                    + _lenEncoder.GetPrice(len - LzmaConstants.kMatchMinLen, posState);
            }
            else
            {
                return
                    _posSlotPrices[(lenToPosState << LzmaConstants.kNumPosSlotBits) + GetPosSlot2(pos)]
                    + _alignPrices[pos & LzmaConstants.kAlignMask]
                    + _lenEncoder.GetPrice(len - LzmaConstants.kMatchMinLen, posState);
            }
        }

        private void MovePos(IBasicInputByteStream inStream, UInt32 num)
        {
            if (num > 0)
            {
                _matchFinder.Skip(inStream, num);
                _additionalOffset += num;
            }
        }

        private (UInt32 len, UInt32 back) Backward(UInt32 cur)
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
            var backRes = _optimum[0].BackPrev;
            _optimumCurrentIndex = _optimum[0].PosPrev;
            return (_optimumCurrentIndex, backRes);
        }

        private void Flush(IBasicOutputByteStream outStream, UInt32 nowPos)
        {
            WriteEndMarker(outStream, nowPos & _posStateMask);
            _rangeEncoder.FlushData(outStream);
        }

        private void WriteEndMarker(IBasicOutputByteStream outStream, UInt32 posState)
        {
            if (!_writeEndMark)
                return;
            _isMatch[(_state.Index << LzmaConstants.kNumPosStatesBitsMax) + posState].Encode(outStream, _rangeEncoder, 1);
            _isRep[_state.Index].Encode(outStream, _rangeEncoder, 0);
            _state.UpdateMatch();
            var len = LzmaConstants.kMatchMinLen;
            _lenEncoder.Encode(outStream, _rangeEncoder, len - LzmaConstants.kMatchMinLen, posState);
            var posSlot = (1U << LzmaConstants.kNumPosSlotBits) - 1;
            var lenToPosState = len.GetLenToPosState(); ;
            _posSlotEncoder[lenToPosState].Encode(outStream, _rangeEncoder, posSlot);
            var footerBits = 30;
            var posReduced = (1U << footerBits) - 1;
            _rangeEncoder.EncodeDirectBits(outStream, posReduced >> LzmaConstants.kNumAlignBits, footerBits - LzmaConstants.kNumAlignBits);
            _posAlignEncoder.ReverseEncode(outStream, _rangeEncoder, posReduced & LzmaConstants.kAlignMask);
        }

        private static UInt32 GetPosSlot(UInt32 pos)
        {
            if (pos < (1 << 11))
                return _fastPos[pos];
            if (pos < (1 << 21))
                return (UInt32)(_fastPos[pos >> 10] + 20);
            return (UInt32)(_fastPos[pos >> 20] + 40);
        }

        private static UInt32 GetPosSlot2(UInt32 pos)
        {
            if (pos < (1 << 17))
                return (UInt32)(_fastPos[pos >> 6] + 12);
            if (pos < (1 << 27))
                return (UInt32)(_fastPos[pos >> 16] + 32);
            return (UInt32)(_fastPos[pos >> 26] + 52);
        }
    }
}

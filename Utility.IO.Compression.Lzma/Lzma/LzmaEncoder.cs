using System;
using System.Linq;
using Utility.IO.Compression.RangeCoder;

namespace Utility.IO.Compression.Lzma
{
    public class LzmaEncoder
        : ISetCoderProperties, IWriteCoderProperties
    {
        private const UInt32 kIfinityPrice = 0xFFFFFFF;
        private const int kDefaultDictionaryLogSize = 22;
        private const UInt32 kNumFastBytesDefault = 0x20;
        private const UInt32 kNumLenSpecSymbols = LzmaCoder.kNumLowLenSymbols + LzmaCoder.kNumMidLenSymbols;
        private const UInt32 kNumOpts = 1 << 12;
        private const int kPropSize = 5;
        private static Byte[] g_FastPos;

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
        private Int64 nowPos64;
        private bool _finished;
        private IInputByteStream<UInt64> _inStream;
        private MatchFinderType _matchFinderType;
        private bool _writeEndMark;
        private bool _needReleaseMFStream;
        private UInt32[] reps;
        private UInt32[] repLens;
        private Byte[] properties;
        private UInt32[] tempPrices;
        private UInt32 _matchPriceCount;

        static LzmaEncoder()
        {
            g_FastPos = new Byte[1 << 11];
            const Byte kFastSlots = 22;
            int c = 2;
            g_FastPos[0] = 0;
            g_FastPos[1] = 1;
            for (Byte slotFast = 2; slotFast < kFastSlots; slotFast++)
            {
                UInt32 k = ((UInt32)1 << ((slotFast >> 1) - 1));
                for (UInt32 j = 0; j < k; j++, c++)
                    g_FastPos[c] = slotFast;
            }
        }

        public LzmaEncoder()
        {
            _state = new LzmaCoder.State();
            _repDistances = new UInt32[LzmaCoder.kNumRepDistances];
            _optimum = new LzmaOptimal[kNumOpts];
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
            _numFastBytes = kNumFastBytesDefault;
            _posSlotPrices = new UInt32[1 << (LzmaCoder.kNumPosSlotBits + LzmaCoder.kNumLenToPosStatesBits)];
            _distancesPrices = new UInt32[LzmaCoder.kNumFullDistances << LzmaCoder.kNumLenToPosStatesBits];
            _alignPrices = new UInt32[LzmaCoder.kAlignTableSize];
            _distTableSize = (kDefaultDictionaryLogSize * 2);
            _posStateBits = 2;
            _posStateMask = 4 - 1;
            _numLiteralPosStateBits = 0;
            _numLiteralContextBits = 3;
            _dictionarySize = (1 << kDefaultDictionaryLogSize);
            _dictionarySizePrev = UInt32.MaxValue;
            _numFastBytesPrev = UInt32.MaxValue;
            _matchFinderType = MatchFinderType.BT4;
            _writeEndMark = false;
            reps = new UInt32[LzmaCoder.kNumRepDistances];
            repLens = new UInt32[LzmaCoder.kNumRepDistances];
            properties = new Byte[kPropSize];
            tempPrices = new UInt32[LzmaCoder.kNumFullDistances];
            for (int i = 0; i < kNumOpts; i++)
                _optimum[i] = new LzmaOptimal();
            for (int i = 0; i < LzmaCoder.kNumLenToPosStates; i++)
                _posSlotEncoder[i] = new RangeCoder.BitTreeEncoder(LzmaCoder.kNumPosSlotBits);
        }

        public void SetCoderProperties(CoderProperties properties)
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

        public void WriteCoderProperties(IOutputByteStream<UInt64> outStream)
        {
            properties[0] = (Byte)((_posStateBits * 5 + _numLiteralPosStateBits) * 9 + _numLiteralContextBits);
            for (int i = 0; i < 4; i++)
                properties[1 + i] = (Byte)((_dictionarySize >> (8 * i)) & 0xFF);
            outStream.WriteBytes(properties.AsReadOnly(), 0, kPropSize);
        }

        public void Code(IInputByteStream<UInt64> inStream, IOutputByteStream<UInt64> outStream, ICodeProgress progress)
        {
            _needReleaseMFStream = false;
            SetStreams(inStream, outStream);
            while (true)
            {
                Int64 processedInSize;
                Int64 processedOutSize;
                var finished = !CodeOneBlock(out processedInSize, out processedOutSize);
                if (progress != null)
                    progress.SetProgress(processedInSize, processedOutSize);
                if (finished)
                    break;
            }
            ReleaseStreams();
            outStream.Close();
        }

        public bool CodeOneBlock(out Int64 inSize, out Int64 outSize)
        {
            inSize = 0;
            outSize = 0;
            var finished = true;

            if (_inStream != null)
            {
                _matchFinder.SetStream(_inStream);
                _matchFinder.Init();
                _needReleaseMFStream = true;
                _inStream = null;
            }

            if (_finished)
                return !finished;
            _finished = true;


            Int64 progressPosValuePrev = nowPos64;
            if (nowPos64 == 0)
            {
                if (_matchFinder.GetNumAvailableBytes() == 0)
                {
                    Flush((UInt32)nowPos64);
                    return !finished;
                }
                UInt32 len, numDistancePairs; // it's not used
                ReadMatchDistances(out len, out numDistancePairs);
                UInt32 posState = (UInt32)(nowPos64) & _posStateMask;
                _isMatch[(_state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].Encode(_rangeEncoder, false);
                _state.UpdateChar();
                Byte curByte = _matchFinder.GetIndexByte((Int32)(0 - _additionalOffset));
                _literalEncoder.GetSubCoder((UInt32)(nowPos64), _previousByte).Encode(_rangeEncoder, curByte);
                _previousByte = curByte;
                _additionalOffset--;
                nowPos64++;
            }
            if (_matchFinder.GetNumAvailableBytes() == 0)
            {
                Flush((UInt32)nowPos64);
                return !finished;
            }
            while (true)
            {
                UInt32 pos;
                UInt32 len = GetOptimum((UInt32)nowPos64, out pos);

                UInt32 posState = ((UInt32)nowPos64) & _posStateMask;
                UInt32 complexState = (_state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState;
                if (len == 1 && pos == UInt32.MaxValue)
                {
                    _isMatch[complexState].Encode(_rangeEncoder, false);
                    Byte curByte = _matchFinder.GetIndexByte((Int32)(0 - _additionalOffset));
                    var subCoder = _literalEncoder.GetSubCoder((UInt32)nowPos64, _previousByte);
                    if (!_state.IsCharState())
                    {
                        Byte matchByte = _matchFinder.GetIndexByte((Int32)(0 - _repDistances[0] - 1 - _additionalOffset));
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
                        UInt32 distance = _repDistances[pos];
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
                        UInt32 posSlot = GetPosSlot(pos);
                        UInt32 lenToPosState = LzmaCoder.GetLenToPosState(len);
                        _posSlotEncoder[lenToPosState].Encode(_rangeEncoder, posSlot);

                        if (posSlot >= LzmaCoder.kStartPosModelIndex)
                        {
                            int footerBits = (int)((posSlot >> 1) - 1);
                            UInt32 baseVal = ((2 | (posSlot & 1)) << footerBits);
                            UInt32 posReduced = pos - baseVal;

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
                        UInt32 distance = pos;
                        for (UInt32 i = LzmaCoder.kNumRepDistances - 1; i >= 1; i--)
                            _repDistances[i] = _repDistances[i - 1];
                        _repDistances[0] = distance;
                        _matchPriceCount++;
                    }
                    _previousByte = _matchFinder.GetIndexByte((Int32)(len - 1 - _additionalOffset));
                }
                _additionalOffset -= len;
                nowPos64 += len;
                if (_additionalOffset == 0)
                {
                    // if (!_fastMode)
                    if (_matchPriceCount >= (1 << 7))
                        FillDistancesPrices();
                    if (_alignPriceCount >= LzmaCoder.kAlignTableSize)
                        FillAlignPrices();
                    inSize = nowPos64;
                    outSize = _rangeEncoder.GetProcessedSizeAdd();
                    if (_matchFinder.GetNumAvailableBytes() == 0)
                    {
                        Flush((UInt32)nowPos64);
                        return !finished;
                    }

                    if (nowPos64 - progressPosValuePrev >= (1 << 12))
                    {
                        _finished = false;
                        finished = false;
                        return !finished;
                    }
                }
            }
        }

        private static UInt32 GetPosSlot(UInt32 pos)
        {
            if (pos < (1 << 11))
                return g_FastPos[pos];
            if (pos < (1 << 21))
                return (UInt32)(g_FastPos[pos >> 10] + 20);
            return (UInt32)(g_FastPos[pos >> 20] + 40);
        }

        private static UInt32 GetPosSlot2(UInt32 pos)
        {
            if (pos < (1 << 17))
                return (UInt32)(g_FastPos[pos >> 6] + 12);
            if (pos < (1 << 27))
                return (UInt32)(g_FastPos[pos >> 16] + 32);
            return (UInt32)(g_FastPos[pos >> 26] + 52);
        }

        private void BaseInit()
        {
            _state.Init();
            _previousByte = 0;
            for (UInt32 i = 0; i < LzmaCoder.kNumRepDistances; i++)
                _repDistances[i] = 0;
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
            _matchFinder.Create(_dictionarySize, kNumOpts, _numFastBytes, LzmaCoder.kMatchMaxLen + 1);
            _dictionarySizePrev = _dictionarySize;
            _numFastBytesPrev = _numFastBytes;
        }

        private void SetWriteEndMarkerMode(bool writeEndMarker)
        {
            _writeEndMark = writeEndMarker;
        }

        private void Init()
        {
            BaseInit();
            _rangeEncoder.Init();

            uint i;
            for (i = 0; i < LzmaCoder.kNumStates; i++)
            {
                for (uint j = 0; j <= _posStateMask; j++)
                {
                    uint complexState = (i << LzmaCoder.kNumPosStatesBitsMax) + j;
                    _isMatch[complexState].Init();
                    _isRep0Long[complexState].Init();
                }
                _isRep[i].Init();
                _isRepG0[i].Init();
                _isRepG1[i].Init();
                _isRepG2[i].Init();
            }
            _literalEncoder.Init();
            for (i = 0; i < LzmaCoder.kNumLenToPosStates; i++)
                _posSlotEncoder[i].Init();
            for (i = 0; i < LzmaCoder.kNumFullDistances - LzmaCoder.kEndPosModelIndex; i++)
                _posEncoders[i].Init();

            _lenEncoder.Init((UInt32)1 << _posStateBits);
            _repMatchLenEncoder.Init((UInt32)1 << _posStateBits);

            _posAlignEncoder.Init();

            _longestMatchWasFound = false;
            _optimumEndIndex = 0;
            _optimumCurrentIndex = 0;
            _additionalOffset = 0;
        }

        private void ReadMatchDistances(out UInt32 lenRes, out UInt32 numDistancePairs)
        {
            lenRes = 0;
            numDistancePairs = _matchFinder.GetMatches(_matchDistances);
            if (numDistancePairs > 0)
            {
                lenRes = _matchDistances[numDistancePairs - 2];
                if (lenRes == _numFastBytes)
                    lenRes += _matchFinder.GetMatchLen((int)lenRes - 1, _matchDistances[numDistancePairs - 1],
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

        private UInt32 GetRepLen1Price(LzmaCoder.State state, UInt32 posState)
        {
            return _isRepG0[state.Index].GetPrice0() +
                    _isRep0Long[(state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].GetPrice0();
        }

        private UInt32 GetPureRepPrice(UInt32 repIndex, LzmaCoder.State state, UInt32 posState)
        {
            UInt32 price;
            if (repIndex == 0)
            {
                price = _isRepG0[state.Index].GetPrice0();
                price += _isRep0Long[(state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].GetPrice1();
            }
            else
            {
                price = _isRepG0[state.Index].GetPrice1();
                if (repIndex == 1)
                    price += _isRepG1[state.Index].GetPrice0();
                else
                {
                    price += _isRepG1[state.Index].GetPrice1();
                    price += _isRepG2[state.Index].GetPrice(repIndex > 2);
                }
            }
            return price;
        }

        private UInt32 GetRepPrice(UInt32 repIndex, UInt32 len, LzmaCoder.State state, UInt32 posState)
        {
            UInt32 price = _repMatchLenEncoder.GetPrice(len - LzmaCoder.kMatchMinLen, posState);
            return price + GetPureRepPrice(repIndex, state, posState);
        }

        private UInt32 GetPosLenPrice(UInt32 pos, UInt32 len, UInt32 posState)
        {
            UInt32 price;
            UInt32 lenToPosState = LzmaCoder.GetLenToPosState(len);
            if (pos < LzmaCoder.kNumFullDistances)
                price = _distancesPrices[(lenToPosState * LzmaCoder.kNumFullDistances) + pos];
            else
                price = _posSlotPrices[(lenToPosState << LzmaCoder.kNumPosSlotBits) + GetPosSlot2(pos)] +
                    _alignPrices[pos & LzmaCoder.kAlignMask];
            return price + _lenEncoder.GetPrice(len - LzmaCoder.kMatchMinLen, posState);
        }

        private UInt32 Backward(out UInt32 backRes, UInt32 cur)
        {
            _optimumEndIndex = cur;
            UInt32 posMem = _optimum[cur].PosPrev;
            UInt32 backMem = _optimum[cur].BackPrev;
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
                UInt32 posPrev = posMem;
                UInt32 backCur = backMem;

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



        private UInt32 GetOptimum(UInt32 position, out UInt32 backRes)
        {
            if (_optimumEndIndex != _optimumCurrentIndex)
            {
                UInt32 lenRes = _optimum[_optimumCurrentIndex].PosPrev - _optimumCurrentIndex;
                backRes = _optimum[_optimumCurrentIndex].BackPrev;
                _optimumCurrentIndex = _optimum[_optimumCurrentIndex].PosPrev;
                return lenRes;
            }
            _optimumCurrentIndex = _optimumEndIndex = 0;

            UInt32 lenMain, numDistancePairs;
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

            UInt32 numAvailableBytes = _matchFinder.GetNumAvailableBytes() + 1;
            if (numAvailableBytes < 2)
            {
                backRes = UInt32.MaxValue;
                return 1;
            }
            if (numAvailableBytes > LzmaCoder.kMatchMaxLen)
                numAvailableBytes = LzmaCoder.kMatchMaxLen;

            UInt32 repMaxIndex = 0;
            UInt32 i;            
            for (i = 0; i < LzmaCoder.kNumRepDistances; i++)
            {
                reps[i] = _repDistances[i];
                repLens[i] = _matchFinder.GetMatchLen(0 - 1, reps[i], LzmaCoder.kMatchMaxLen);
                if (repLens[i] > repLens[repMaxIndex])
                    repMaxIndex = i;
            }
            if (repLens[repMaxIndex] >= _numFastBytes)
            {
                backRes = repMaxIndex;
                UInt32 lenRes = repLens[repMaxIndex];
                MovePos(lenRes - 1);
                return lenRes;
            }

            if (lenMain >= _numFastBytes)
            {
                backRes = _matchDistances[numDistancePairs - 1] + LzmaCoder.kNumRepDistances;
                MovePos(lenMain - 1);
                return lenMain;
            }
            
            Byte currentByte = _matchFinder.GetIndexByte(0 - 1);
            Byte matchByte = _matchFinder.GetIndexByte((Int32)(0 - _repDistances[0] - 1 - 1));

            if (lenMain < 2 && currentByte != matchByte && repLens[repMaxIndex] < 2)
            {
                backRes = (UInt32)UInt32.MaxValue;
                return 1;
            }

            _optimum[0].State = _state;

            UInt32 posState = (position & _posStateMask);

            _optimum[1].Price = _isMatch[(_state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].GetPrice0() +
                    _literalEncoder.GetSubCoder(position, _previousByte).GetPrice(!_state.IsCharState(), matchByte, currentByte);
            _optimum[1].MakeAsChar();

            UInt32 matchPrice = _isMatch[(_state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].GetPrice1();
            UInt32 repMatchPrice = matchPrice + _isRep[_state.Index].GetPrice1();

            if (matchByte == currentByte)
            {
                UInt32 shortRepPrice = repMatchPrice + GetRepLen1Price(_state, posState);
                if (shortRepPrice < _optimum[1].Price)
                {
                    _optimum[1].Price = shortRepPrice;
                    _optimum[1].MakeAsShortRep();
                }
            }

            UInt32 lenEnd = ((lenMain >= repLens[repMaxIndex]) ? lenMain : repLens[repMaxIndex]);

            if(lenEnd < 2)
            {
                backRes = _optimum[1].BackPrev;
                return 1;
            }
            
            _optimum[1].PosPrev = 0;

            _optimum[0].Backs0 = reps[0];
            _optimum[0].Backs1 = reps[1];
            _optimum[0].Backs2 = reps[2];
            _optimum[0].Backs3 = reps[3];

            UInt32 len = lenEnd;
            do
                _optimum[len--].Price = kIfinityPrice;
            while (len >= 2);

            for (i = 0; i < LzmaCoder.kNumRepDistances; i++)
            {
                UInt32 repLen = repLens[i];
                if (repLen < 2)
                    continue;
                UInt32 price = repMatchPrice + GetPureRepPrice(i, _state, posState);
                do
                {
                    UInt32 curAndLenPrice = price + _repMatchLenEncoder.GetPrice(repLen - 2, posState);
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

            UInt32 normalMatchPrice = matchPrice + _isRep[_state.Index].GetPrice0();
            
            len = ((repLens[0] >= 2) ? repLens[0] + 1 : 2);
            if (len <= lenMain)
            {
                UInt32 offs = 0;
                while (len > _matchDistances[offs])
                    offs += 2;
                for (; ; len++)
                {
                    UInt32 distance = _matchDistances[offs + 1];
                    UInt32 curAndLenPrice = normalMatchPrice + GetPosLenPrice(distance, len, posState);
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

            UInt32 cur = 0;

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
                UInt32 posPrev = _optimum[cur].PosPrev;
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
                            reps[0] = opt.Backs0;
                            reps[1] = opt.Backs1;
                            reps[2] = opt.Backs2;
                            reps[3] = opt.Backs3;
                        }
                        else if (pos == 1)
                        {
                            reps[0] = opt.Backs1;
                            reps[1] = opt.Backs0;
                            reps[2] = opt.Backs2;
                            reps[3] = opt.Backs3;
                        }
                        else if (pos == 2)
                        {
                            reps[0] = opt.Backs2;
                            reps[1] = opt.Backs0;
                            reps[2] = opt.Backs1;
                            reps[3] = opt.Backs3;
                        }
                        else
                        {
                            reps[0] = opt.Backs3;
                            reps[1] = opt.Backs0;
                            reps[2] = opt.Backs1;
                            reps[3] = opt.Backs2;
                        }
                    }
                    else
                    {
                        reps[0] = (pos - LzmaCoder.kNumRepDistances);
                        reps[1] = opt.Backs0;
                        reps[2] = opt.Backs1;
                        reps[3] = opt.Backs2;
                    }
                }
                _optimum[cur].State = state;
                _optimum[cur].Backs0 = reps[0];
                _optimum[cur].Backs1 = reps[1];
                _optimum[cur].Backs2 = reps[2];
                _optimum[cur].Backs3 = reps[3];
                UInt32 curPrice = _optimum[cur].Price;

                currentByte = _matchFinder.GetIndexByte(0 - 1);
                matchByte = _matchFinder.GetIndexByte((Int32)(0 - reps[0] - 1 - 1));

                posState = (position & _posStateMask);

                UInt32 curAnd1Price = curPrice +
                    _isMatch[(state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].GetPrice0() +
                    _literalEncoder.GetSubCoder(position, _matchFinder.GetIndexByte(0 - 2)).
                    GetPrice(!state.IsCharState(), matchByte, currentByte);

                var nextOptimum = _optimum[cur + 1];

                bool nextIsChar = false;
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
                    UInt32 shortRepPrice = repMatchPrice + GetRepLen1Price(state, posState);
                    if (shortRepPrice <= nextOptimum.Price)
                    {
                        nextOptimum.Price = shortRepPrice;
                        nextOptimum.PosPrev = cur;
                        nextOptimum.MakeAsShortRep();
                        nextIsChar = true;
                    }
                }

                UInt32 numAvailableBytesFull = _matchFinder.GetNumAvailableBytes() + 1;
                numAvailableBytesFull = Math.Min(kNumOpts - 1 - cur, numAvailableBytesFull);
                numAvailableBytes = numAvailableBytesFull;

                if (numAvailableBytes < 2)
                    continue;
                if (numAvailableBytes > _numFastBytes)
                    numAvailableBytes = _numFastBytes;
                if (!nextIsChar && matchByte != currentByte)
                {
                    // try Literal + rep0
                    UInt32 t = Math.Min(numAvailableBytesFull - 1, _numFastBytes);
                    UInt32 lenTest2 = _matchFinder.GetMatchLen(0, reps[0], t);
                    if (lenTest2 >= 2)
                    {
                        LzmaCoder.State state2 = state;
                        state2.UpdateChar();
                        UInt32 posStateNext = (position + 1) & _posStateMask;
                        UInt32 nextRepMatchPrice = curAnd1Price +
                            _isMatch[(state2.Index << LzmaCoder.kNumPosStatesBitsMax) + posStateNext].GetPrice1() +
                            _isRep[state2.Index].GetPrice1();
                        {
                            UInt32 offset = cur + 1 + lenTest2;
                            while (lenEnd < offset)
                                _optimum[++lenEnd].Price = kIfinityPrice;
                            UInt32 curAndLenPrice = nextRepMatchPrice + GetRepPrice(
                                0, lenTest2, state2, posStateNext);
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
                }

                UInt32 startLen = 2; // speed optimization 

                for (UInt32 repIndex = 0; repIndex < LzmaCoder.kNumRepDistances; repIndex++)
                {
                    UInt32 lenTest = _matchFinder.GetMatchLen(0 - 1, reps[repIndex], numAvailableBytes);
                    if (lenTest < 2)
                        continue;
                    UInt32 lenTestTemp = lenTest;
                    do
                    {
                        while (lenEnd < cur + lenTest)
                            _optimum[++lenEnd].Price = kIfinityPrice;
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
                    while(--lenTest >= 2);
                    lenTest = lenTestTemp;

                    if (repIndex == 0)
                        startLen = lenTest + 1;

                    // if (_maxMode)
                    if (lenTest < numAvailableBytesFull)
                    {
                        UInt32 t = Math.Min(numAvailableBytesFull - 1 - lenTest, _numFastBytes);
                        UInt32 lenTest2 = _matchFinder.GetMatchLen((Int32)lenTest, reps[repIndex], t);
                        if (lenTest2 >= 2)
                        {
                            LzmaCoder.State state2 = state;
                            state2.UpdateRep();
                            UInt32 posStateNext = (position + lenTest) & _posStateMask;
                            UInt32 curAndLenCharPrice = 
                                    repMatchPrice + GetRepPrice(repIndex, lenTest, state, posState) + 
                                    _isMatch[(state2.Index << LzmaCoder.kNumPosStatesBitsMax) + posStateNext].GetPrice0() +
                                    _literalEncoder.GetSubCoder(position + lenTest, 
                                    _matchFinder.GetIndexByte((Int32)lenTest - 1 - 1)).GetPrice(true,
                                    _matchFinder.GetIndexByte((Int32)((Int32)lenTest - 1 - (Int32)(reps[repIndex] + 1))), 
                                    _matchFinder.GetIndexByte((Int32)lenTest - 1));
                            state2.UpdateChar();
                            posStateNext = (position + lenTest + 1) & _posStateMask;
                            UInt32 nextMatchPrice = curAndLenCharPrice + _isMatch[(state2.Index << LzmaCoder.kNumPosStatesBitsMax) + posStateNext].GetPrice1();
                            UInt32 nextRepMatchPrice = nextMatchPrice + _isRep[state2.Index].GetPrice1();
                            
                            // for(; lenTest2 >= 2; lenTest2--)
                            {
                                UInt32 offset = lenTest + 1 + lenTest2;
                                while(lenEnd < cur + offset)
                                    _optimum[++lenEnd].Price = kIfinityPrice;
                                UInt32 curAndLenPrice = nextRepMatchPrice + GetRepPrice(0, lenTest2, state2, posStateNext);
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
                    for (numDistancePairs = 0; newLen > _matchDistances[numDistancePairs]; numDistancePairs += 2) ;
                    _matchDistances[numDistancePairs] = newLen;
                    numDistancePairs += 2;
                }
                if (newLen >= startLen)
                {
                    normalMatchPrice = matchPrice + _isRep[state.Index].GetPrice0();
                    while (lenEnd < cur + newLen)
                        _optimum[++lenEnd].Price = kIfinityPrice;

                    UInt32 offs = 0;
                    while (startLen > _matchDistances[offs])
                        offs += 2;

                    for (UInt32 lenTest = startLen; ; lenTest++)
                    {
                        UInt32 curBack = _matchDistances[offs + 1];
                        UInt32 curAndLenPrice = normalMatchPrice + GetPosLenPrice(curBack, lenTest, posState);
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
                                UInt32 t = Math.Min(numAvailableBytesFull - 1 - lenTest, _numFastBytes);
                                UInt32 lenTest2 = _matchFinder.GetMatchLen((Int32)lenTest, curBack, t);
                                if (lenTest2 >= 2)
                                {
                                    LzmaCoder.State state2 = state;
                                    state2.UpdateMatch();
                                    UInt32 posStateNext = (position + lenTest) & _posStateMask;
                                    UInt32 curAndLenCharPrice = curAndLenPrice +
                                        _isMatch[(state2.Index << LzmaCoder.kNumPosStatesBitsMax) + posStateNext].GetPrice0() +
                                        _literalEncoder.GetSubCoder(position + lenTest,
                                        _matchFinder.GetIndexByte((Int32)lenTest - 1 - 1)).
                                        GetPrice(true,
                                        _matchFinder.GetIndexByte((Int32)lenTest - (Int32)(curBack + 1) - 1),
                                        _matchFinder.GetIndexByte((Int32)lenTest - 1));
                                    state2.UpdateChar();
                                    posStateNext = (position + lenTest + 1) & _posStateMask;
                                    UInt32 nextMatchPrice = curAndLenCharPrice + _isMatch[(state2.Index << LzmaCoder.kNumPosStatesBitsMax) + posStateNext].GetPrice1();
                                    UInt32 nextRepMatchPrice = nextMatchPrice + _isRep[state2.Index].GetPrice1();

                                    UInt32 offset = lenTest + 1 + lenTest2;
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

        private void WriteEndMarker(UInt32 posState)
        {
            if (!_writeEndMark)
                return;

            _isMatch[(_state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].Encode(_rangeEncoder, true);
            _isRep[_state.Index].Encode(_rangeEncoder, false);
            _state.UpdateMatch();
            UInt32 len = LzmaCoder.kMatchMinLen;
            _lenEncoder.Encode(_rangeEncoder, len - LzmaCoder.kMatchMinLen, posState);
            UInt32 posSlot = (1 << LzmaCoder.kNumPosSlotBits) - 1;
            UInt32 lenToPosState = LzmaCoder.GetLenToPosState(len);
            _posSlotEncoder[lenToPosState].Encode(_rangeEncoder, posSlot);
            int footerBits = 30;
            UInt32 posReduced = (((UInt32)1) << footerBits) - 1;
            _rangeEncoder.EncodeDirectBits(posReduced >> LzmaCoder.kNumAlignBits, footerBits - LzmaCoder.kNumAlignBits);
            _posAlignEncoder.ReverseEncode(_rangeEncoder, posReduced & LzmaCoder.kAlignMask);
        }

        private void Flush(UInt32 nowPos)
        {
            ReleaseMFStream();
            WriteEndMarker(nowPos & _posStateMask);
            _rangeEncoder.FlushData();
        }

        private void ReleaseMFStream()
        {
            if (_matchFinder != null && _needReleaseMFStream)
            {
                _matchFinder.ReleaseStream();
                _needReleaseMFStream = false;
            }
        }

        private void SetOutStream(IOutputByteStream<UInt64> outStream) { _rangeEncoder.SetStream(outStream); }
        private void ReleaseOutStream() { _rangeEncoder.ReleaseStream(); }

        private void ReleaseStreams()
        {
            ReleaseMFStream();
            ReleaseOutStream();
        }

        private void SetStreams(IInputByteStream<UInt64> inStream, IOutputByteStream<UInt64> outStream)
        {
            _inStream = inStream;
            _finished = false;
            Create();
            SetOutStream(outStream);
            Init();

            // if (!_fastMode)
            {
                FillDistancesPrices();
                FillAlignPrices();
            }

            _lenEncoder.SetTableSize(_numFastBytes + 1 - LzmaCoder.kMatchMinLen);
            _lenEncoder.UpdateTables((UInt32)1 << _posStateBits);
            _repMatchLenEncoder.SetTableSize(_numFastBytes + 1 - LzmaCoder.kMatchMinLen);
            _repMatchLenEncoder.UpdateTables((UInt32)1 << _posStateBits);

            nowPos64 = 0;
        }

        private void FillDistancesPrices()
        {
            for (UInt32 i = LzmaCoder.kStartPosModelIndex; i < LzmaCoder.kNumFullDistances; i++)
            { 
                UInt32 posSlot = GetPosSlot(i);
                int footerBits = (int)((posSlot >> 1) - 1);
                UInt32 baseVal = ((2 | (posSlot & 1)) << footerBits);
                tempPrices[i] = BitTreeEncoder.ReverseGetPrice(_posEncoders, 
                    baseVal - posSlot - 1, footerBits, i - baseVal);
            }

            for (UInt32 lenToPosState = 0; lenToPosState < LzmaCoder.kNumLenToPosStates; lenToPosState++)
            {
                UInt32 posSlot;
                RangeCoder.BitTreeEncoder encoder = _posSlotEncoder[lenToPosState];
            
                UInt32 st = (lenToPosState << LzmaCoder.kNumPosSlotBits);
                for (posSlot = 0; posSlot < _distTableSize; posSlot++)
                    _posSlotPrices[st + posSlot] = encoder.GetPrice(posSlot);
                for (posSlot = LzmaCoder.kEndPosModelIndex; posSlot < _distTableSize; posSlot++)
                    _posSlotPrices[st + posSlot] += ((((posSlot >> 1) - 1) - LzmaCoder.kNumAlignBits) << RangeCoder.BitEncoder.kNumBitPriceShiftBits);

                UInt32 st2 = lenToPosState * LzmaCoder.kNumFullDistances;
                UInt32 i;
                for (i = 0; i < LzmaCoder.kStartPosModelIndex; i++)
                    _distancesPrices[st2 + i] = _posSlotPrices[st + i];
                for (; i < LzmaCoder.kNumFullDistances; i++)
                    _distancesPrices[st2 + i] = _posSlotPrices[st + GetPosSlot(i)] + tempPrices[i];
            }
            _matchPriceCount = 0;
        }

        private void FillAlignPrices()
        {
            for (UInt32 i = 0; i < LzmaCoder.kAlignTableSize; i++)
                _alignPrices[i] = _posAlignEncoder.ReverseGetPrice(i);
            _alignPriceCount = 0;
        }
    }
}

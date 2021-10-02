using System;

namespace Utility.IO.Compression.Lzma
{
    class LzmaLenEncoder
    {
        RangeCoder.BitEncoder _choice = new RangeCoder.BitEncoder();
        RangeCoder.BitEncoder _choice2 = new RangeCoder.BitEncoder();
        RangeCoder.BitTreeEncoder[] _lowCoder = new RangeCoder.BitTreeEncoder[LzmaCoder.kNumPosStatesEncodingMax];
        RangeCoder.BitTreeEncoder[] _midCoder = new RangeCoder.BitTreeEncoder[LzmaCoder.kNumPosStatesEncodingMax];
        RangeCoder.BitTreeEncoder _highCoder = new RangeCoder.BitTreeEncoder(LzmaCoder.kNumHighLenBits);

        public LzmaLenEncoder()
        {
            for (UInt32 posState = 0; posState < LzmaCoder.kNumPosStatesEncodingMax; posState++)
            {
                _lowCoder[posState] = new RangeCoder.BitTreeEncoder(LzmaCoder.kNumLowLenBits);
                _midCoder[posState] = new RangeCoder.BitTreeEncoder(LzmaCoder.kNumMidLenBits);
            }
        }

        public void Init(UInt32 numPosStates)
        {
            _choice.Init();
            _choice2.Init();
            for (UInt32 posState = 0; posState < numPosStates; posState++)
            {
                _lowCoder[posState].Init();
                _midCoder[posState].Init();
            }
            _highCoder.Init();
        }

        public virtual void Encode(RangeCoder.RangeEncoder rangeEncoder, UInt32 symbol, UInt32 posState)
        {
            if (symbol < LzmaCoder.kNumLowLenSymbols)
            {
                _choice.Encode(rangeEncoder, false);
                _lowCoder[posState].Encode(rangeEncoder, symbol);
            }
            else
            {
                symbol -= LzmaCoder.kNumLowLenSymbols;
                _choice.Encode(rangeEncoder, true);
                if (symbol < LzmaCoder.kNumMidLenSymbols)
                {
                    _choice2.Encode(rangeEncoder, false);
                    _midCoder[posState].Encode(rangeEncoder, symbol);
                }
                else
                {
                    _choice2.Encode(rangeEncoder, true);
                    _highCoder.Encode(rangeEncoder, symbol - LzmaCoder.kNumMidLenSymbols);
                }
            }
        }

        public void SetPrices(UInt32 posState, UInt32 numSymbols, UInt32[] prices, UInt32 st)
        {
            UInt32 a0 = _choice.GetPrice0();
            UInt32 a1 = _choice.GetPrice1();
            UInt32 b0 = a1 + _choice2.GetPrice0();
            UInt32 b1 = a1 + _choice2.GetPrice1();
            UInt32 i = 0;
            for (i = 0; i < LzmaCoder.kNumLowLenSymbols; i++)
            {
                if (i >= numSymbols)
                    return;
                prices[st + i] = a0 + _lowCoder[posState].GetPrice(i);
            }
            for (; i < LzmaCoder.kNumLowLenSymbols + LzmaCoder.kNumMidLenSymbols; i++)
            {
                if (i >= numSymbols)
                    return;
                prices[st + i] = b0 + _midCoder[posState].GetPrice(i - LzmaCoder.kNumLowLenSymbols);
            }
            for (; i < numSymbols; i++)
                prices[st + i] = b1 + _highCoder.GetPrice(i - LzmaCoder.kNumLowLenSymbols - LzmaCoder.kNumMidLenSymbols);
        }
    };

}

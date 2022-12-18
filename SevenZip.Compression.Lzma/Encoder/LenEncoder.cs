// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using SevenZip.Compression.Lzma.Encoder.RangeEncoder;
using System;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Encoder
{
    class LenEncoder
    {
        private readonly BitTreeEncoder[] _lowCoder;
        private readonly BitTreeEncoder[] _midCoder;
        private readonly BitTreeEncoder _highCoder;

        private BitEncoder _choice;
        private BitEncoder _choice2;

        public LenEncoder(UInt32 numPosStates)
        {
            _choice = new BitEncoder();
            _choice2 = new BitEncoder();
            _lowCoder = new BitTreeEncoder[numPosStates];
            _lowCoder.FillArray(_ => new BitTreeEncoder(LzmaConstants.kNumLowLenBits));
            _midCoder = new BitTreeEncoder[numPosStates];
            _midCoder.FillArray(_ => new BitTreeEncoder(LzmaConstants.kNumMidLenBits));
            _highCoder = new BitTreeEncoder(LzmaConstants.kNumHighLenBits);
        }

        public virtual void Encode(IBasicOutputByteStream outStream, RangeEncoder.Encoder rangeEncoder, UInt32 symbol, UInt32 posState)
        {
            if (symbol < LzmaConstants.kNumLowLenSymbols)
            {
                _choice.Encode(outStream, rangeEncoder, 0);
                _lowCoder[posState].Encode(outStream, rangeEncoder, symbol);
            }
            else
            {
                symbol -= LzmaConstants.kNumLowLenSymbols;
                _choice.Encode(outStream, rangeEncoder, 1);
                if (symbol < LzmaConstants.kNumMidLenSymbols)
                {
                    _choice2.Encode(outStream, rangeEncoder, 0);
                    _midCoder[posState].Encode(outStream, rangeEncoder, symbol);
                }
                else
                {
                    _choice2.Encode(outStream, rangeEncoder, 1);
                    _highCoder.Encode(outStream, rangeEncoder, symbol - LzmaConstants.kNumMidLenSymbols);
                }
            }
        }

        public void SetPrices(UInt32 posState, UInt32 numSymbols, UInt32[] prices, UInt32 st)
        {
            var a0 = _choice.GetPrice0();
            var a1 = _choice.GetPrice1();
            var b0 = a1 + _choice2.GetPrice0();
            var b1 = a1 + _choice2.GetPrice1();
            var i = 0U;
            while (i < LzmaConstants.kNumLowLenSymbols)
            {
                if (i >= numSymbols)
                    return;
                prices[st + i] = a0 + _lowCoder[posState].GetPrice(i);
                ++i;
            }
            while (i < LzmaConstants.kNumLowLenSymbols + LzmaConstants.kNumMidLenSymbols)
            {
                if (i >= numSymbols)
                    return;
                prices[st + i] = b0 + _midCoder[posState].GetPrice(i - LzmaConstants.kNumLowLenSymbols);
                ++i;
            }
            while (i < numSymbols)
            {
                prices[st + i] = b1 + _highCoder.GetPrice(i - LzmaConstants.kNumLowLenSymbols - LzmaConstants.kNumMidLenSymbols);
                ++i;
            }
        }
    }
}

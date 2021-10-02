using System;

namespace Utility.IO.Compression.RangeCoder
{
    struct BitTreeEncoder
    {
        private BitEncoder[] _models;
        private int _numBitLevels;

        public BitTreeEncoder(int numBitLevels)
        {
            _numBitLevels = numBitLevels;
            _models = new BitEncoder[1 << numBitLevels];
        }

        public void Init()
        {
            for (uint i = 1; i < (1 << _numBitLevels); i++)
                _models[i].Init();
        }

        public void Encode(RangeEncoder rangeEncoder, UInt32 symbol)
        {
            var m = 1U;
            for (int bitIndex = _numBitLevels; bitIndex > 0;)
            {
                bitIndex--;
                var bit = ((symbol >> bitIndex) & 1) != 0;
                _models[m].Encode(rangeEncoder, bit);
                m = m.ConcatBit(bit);
            }
        }

        public void ReverseEncode(RangeEncoder rangeEncoder, UInt32 symbol)
        {
            var m = 1U;
            for (var i = 0; i < _numBitLevels; i++)
            {
                var bit = (symbol & 1) != 0;
                _models[m].Encode(rangeEncoder, bit);
                m = m.ConcatBit(bit);
                symbol >>= 1;
            }
        }

        public UInt32 GetPrice(UInt32 symbol)
        {
            var price = 0U;
            var m = 1U;
            for (var bitIndex = _numBitLevels; bitIndex > 0;)
            {
                bitIndex--;
                var bit = ((symbol >> bitIndex) & 1) != 0;
                price += _models[m].GetPrice(bit);
                m = m.ConcatBit(bit);
            }
            return price;
        }

        public UInt32 ReverseGetPrice(UInt32 symbol)
        {
            var price = 0U;
            var m = 1U;
            for (var i = _numBitLevels; i > 0; i--)
            {
                var bit = (symbol & 1) != 0;
                symbol >>= 1;
                price += _models[m].GetPrice(bit);
                m = m.ConcatBit(bit);
            }
            return price;
        }

        public static UInt32 ReverseGetPrice(BitEncoder[] models, UInt32 startIndex,
            int NumBitLevels, UInt32 symbol)
        {
            var price = 0U;
            var m = 1U;
            for (var i = NumBitLevels; i > 0; i--)
            {
                var bit = (symbol & 1) != 0;
                symbol >>= 1;
                price += models[startIndex + m].GetPrice(bit);
                m = m.ConcatBit(bit);
            }
            return price;
        }

        public static void ReverseEncode(BitEncoder[] models, UInt32 startIndex, RangeEncoder rangeEncoder, int NumBitLevels, UInt32 symbol)
        {
            var m = 1U;
            for (var i = 0; i < NumBitLevels; i++)
            {
                var bit = (symbol & 1) != 0;
                models[startIndex + m].Encode(rangeEncoder, bit);
                m = m.ConcatBit(bit);
                symbol >>= 1;
            }
        }
    }
}

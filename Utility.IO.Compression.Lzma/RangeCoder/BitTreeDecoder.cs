using System;

namespace Utility.IO.Compression.Lzma.RangeCoder
{
    struct BitTreeDecoder
    {
        private BitDecoder[] _models;
        private int _numBitLevels;

        public BitTreeDecoder(int numBitLevels)
        {
            _numBitLevels = numBitLevels;
            _models = new BitDecoder[1 << numBitLevels];
        }

        public void Init()
        {
            for (uint i = 1; i < (1 << _numBitLevels); i++)
                _models[i].Init();
        }

        public uint Decode(RangeCoder.RangeDecoder rangeDecoder)
        {
            var m = 1U;
            for (int bitIndex = _numBitLevels; bitIndex > 0; bitIndex--)
                m = m.ConcatBit(_models[m].Decode(rangeDecoder));
            return m - (1U << _numBitLevels);
        }

        public uint ReverseDecode(RangeCoder.RangeDecoder rangeDecoder)
        {
            var m = 1U;
            var symbol = 0U;
            for (int bitIndex = 0; bitIndex < _numBitLevels; bitIndex++)
            {
                var bit = _models[m].Decode(rangeDecoder);
                m = m.ConcatBit(bit);
                if (bit)
                    symbol |= 1U << bitIndex;
            }
            return symbol;
        }

        public static uint ReverseDecode(BitDecoder[] models, UInt32 startIndex, RangeCoder.RangeDecoder rangeDecoder, int numBitLevels)
        {
            var m = 1U;
            var symbol = 0U;
            for (int bitIndex = 0; bitIndex < numBitLevels; bitIndex++)
            {
                var bit = models[startIndex + m].Decode(rangeDecoder);
                m = m.ConcatBit(bit);
                if (bit)
                    symbol |= 1U << bitIndex;
            }
            return symbol;
        }
    }
}

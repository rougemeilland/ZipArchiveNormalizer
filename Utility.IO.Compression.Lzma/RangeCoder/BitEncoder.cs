using System;

namespace Utility.IO.Compression.RangeCoder
{
    struct BitEncoder
    {
        public const int kNumBitModelTotalBits = 11;
        public const uint kBitModelTotal = (1 << kNumBitModelTotalBits);
        public const int kNumBitPriceShiftBits = 6;

        private const int kNumMoveBits = 5;
        private const int kNumMoveReducingBits = 2;
        private static UInt32[] _probPrices;

        private uint _prob;


        static BitEncoder()
        {
            _probPrices = new UInt32[kBitModelTotal >> kNumMoveReducingBits];
            const int kNumBits = (kNumBitModelTotalBits - kNumMoveReducingBits);
            for (int i = kNumBits - 1; i >= 0; i--)
            {
                UInt32 start = (UInt32)1 << (kNumBits - i - 1);
                UInt32 end = (UInt32)1 << (kNumBits - i);
                for (UInt32 j = start; j < end; j++)
                    _probPrices[j] = ((UInt32)i << kNumBitPriceShiftBits) +
                        (((end - j) << kNumBitPriceShiftBits) >> (kNumBits - i - 1));
            }
        }

        public void Init() { _prob = kBitModelTotal >> 1; }

        public void Encode(RangeEncoder encoder, bool symbol)
        {
            uint newBound = (encoder.Range >> kNumBitModelTotalBits) * _prob;
            if (symbol)
            {
                encoder.Low += newBound;
                encoder.Range -= newBound;
                _prob -= _prob >> kNumMoveBits;
            }
            else
            {
                encoder.Range = newBound;
                _prob += (kBitModelTotal - _prob) >> kNumMoveBits;
            }
            if (encoder.Range < RangeEncoder.kTopValue)
            {
                encoder.Range <<= 8;
                encoder.ShiftLow();
            }
        }

        public uint GetPrice(bool symbol)
        {
            return _probPrices[((symbol ? ~(_prob - 1) : _prob) & (kBitModelTotal - 1)) >> kNumMoveReducingBits];
        }
        public uint GetPrice0() { return _probPrices[_prob >> kNumMoveReducingBits]; }
        public uint GetPrice1() { return _probPrices[(kBitModelTotal - _prob) >> kNumMoveReducingBits]; }
    }
}

// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using System;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Encoder.RangeEncoder
{
    struct BitEncoder
    {
        public const Int32 kNumBitPriceShiftBits = 6;

        private const Int32 kNumBitModelTotalBits = 11;
        private const UInt32 kBitModelTotal = 1 << kNumBitModelTotalBits;
        private const Int32 kNumMoveReducingBits = 2;

        private static readonly UInt32[] _probPrices;

        private UInt16 _prob;

        static BitEncoder()
        {
            const Int32 kNumBits = kNumBitModelTotalBits - kNumMoveReducingBits;

            _probPrices = new UInt32[kBitModelTotal >> kNumMoveReducingBits];
            for (var i = kNumBits - 1; i >= 0; --i)
            {
                var start = 1U << (kNumBits - i - 1);
                var end = 1U << (kNumBits - i);
                for (var j = start; j < end; ++j)
                    _probPrices[j] = ((UInt32)i << kNumBitPriceShiftBits) +
                        (((end - j) << kNumBitPriceShiftBits) >> (kNumBits - i - 1));
            }
        }

        public BitEncoder()
        {
            _prob = (UInt16)(kBitModelTotal >> 1);
        }

        public void Encode(IBasicOutputByteStream outStream, Encoder encoder, UInt32 symbol) => encoder.Encode(outStream, symbol, ref _prob);
        public UInt32 GetPrice(UInt32 symbol) => _probPrices[(((_prob - symbol) ^ (-(Int32)symbol)) & (kBitModelTotal - 1)) >> kNumMoveReducingBits];
        public UInt32 GetPrice0() => _probPrices[_prob >> kNumMoveReducingBits];
        public UInt32 GetPrice1() => _probPrices[(kBitModelTotal - _prob) >> kNumMoveReducingBits];
    }
}

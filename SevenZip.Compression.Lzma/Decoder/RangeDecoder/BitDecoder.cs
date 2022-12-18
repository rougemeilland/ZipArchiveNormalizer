// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using System;
using System.Runtime.CompilerServices;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Decoder.RangeDecoder
{
    struct BitDecoder
    {
        private const Int32 kNumBitModelTotalBits = 11;
        private const UInt32 kBitModelTotal = 1 << kNumBitModelTotalBits;

        private UInt16 _prob;

        public BitDecoder()
        {
            _prob = (UInt16)(kBitModelTotal >> 1);
        }

        public UInt32 Decode(IBasicInputByteStream inStream, Decoder rangeDecoder) =>
            rangeDecoder.Decode(inStream, ref _prob);
    }
}

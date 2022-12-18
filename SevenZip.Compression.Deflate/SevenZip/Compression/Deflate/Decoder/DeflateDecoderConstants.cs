// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;

namespace SevenZip.Compression.Deflate.Decoder
{
    class DeflateDecoderConstants
        : DeflateConstants
    {
        public const Int32 kNumBigValueBits = 8 * 4;
        public const Int32 kNumValueBytes = 3;
        public const Int32 kNumValueBits = 8 * kNumValueBytes;
        public const UInt32 kMask = (1 << kNumValueBits) - 1;
        public const Int32 kNumPairLenBits = 4;
        public const UInt32 kPairLenMask = (1U << kNumPairLenBits) - 1;
        public const Int32 kLenIdFinished = -1;
        public const Int32 kLenIdNeedInit = -2;
    }
}

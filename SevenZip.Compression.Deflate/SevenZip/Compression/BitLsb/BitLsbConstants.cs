// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility;

namespace SevenZip.Compression.BitLsb
{
    static class BitLsbConstants
    {
        public const Int32 kNumBigValueBits = 8 * 4;
        public const Int32 kNumValueBytes = 3;
        public const Int32 kNumValueBits = 8 * kNumValueBytes;
        public const UInt32 kMask = (1 << kNumValueBits) - 1;

        // TODO: 正常動作を確認後、SpanやMemoryを配列に置き換えることを考慮に入れて高速化を試みる。
        public static readonly ReadOnlyMemory<Byte> kInvertTable;

        static BitLsbConstants()
        {
            var invertTable = new Byte[256];
            for (var i = 0U; i < invertTable.Length; i++)
            {
                var x = ((i & 0x55) << 1) | ((i & 0xAA) >> 1);
                x = ((x & 0x33) << 2) | ((x & 0xCC) >> 2);
                invertTable[i] = (Byte)(((x & 0x0F) << 4) | ((x & 0xF0) >> 4));
            }
            kInvertTable = invertTable;
        }
    }
}

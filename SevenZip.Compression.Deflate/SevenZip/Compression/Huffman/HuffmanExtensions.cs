// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility;

namespace SevenZip.Compression.Huffman
{
    static class HuffmanExtensions
    {
        private const Int32 NUM_BITS = 10;
        private const Int32 kMaxLen = 16;

        public static void HuffmanGenerate(this ReadOnlySpan<UInt32> freqs, Span<UInt32> p, Span<Byte> lens, UInt32 numSymbols, UInt32 maxLen)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            var num = 0;
            for (var i = 0U; i < numSymbols; i++)
            {
                var freq = freqs[(Int32)i];
                if (freq == 0)
                    lens[(Int32)i] = 0;
                else
                    p[num++] = i | (freq << NUM_BITS);
            }
            p[..num].QuickSort();
            if (num < 2)
            {
                var minCode = 0U;
                var maxCode = 1U;
                if (num == 1)
                {
                    maxCode = p[0] & ((1U << NUM_BITS) - 1);
                    if (maxCode == 0)
                        maxCode++;
                }
                p[(Int32)minCode] = 0;
                p[(Int32)maxCode] = 1;
                lens[(Int32)minCode] = 1;
                lens[(Int32)maxCode] = 1;
                return;
            }
            var e = 0U;
            {
                var i = 0;
                var b = 0;
                do
                {
                    var n = (i != num && (b == e || (p[i] >> NUM_BITS) <= (p[b] >> NUM_BITS))) ? i++ : b++;
                    var freq = p[n] & ~((1U << NUM_BITS) - 1);
                    p[n] = (p[n] & ((1U << NUM_BITS) - 1)) | (e << NUM_BITS);
                    var m = (i != num && (b == e || (p[i] >> NUM_BITS) <= (p[b] >> NUM_BITS))) ? i++ : b++;
                    freq += p[m] & ~((1U << NUM_BITS) - 1);
                    p[m] = (p[m] & ((1U << NUM_BITS) - 1)) | (e << NUM_BITS);
                    p[(Int32)e] = (p[(Int32)e] & ((1U << NUM_BITS) - 1)) | freq;
                    e++;
                }
                while (num - e > 1);
            }
            Span<UInt32> lenCounters = stackalloc UInt32[kMaxLen + 1];
            lenCounters.Clear();
            p[(Int32)(--e)] &= (1U << NUM_BITS) - 1;
            lenCounters[1] = 2;
            while (e > 0)
            {
                UInt32 len = (p[(Int32)(p[(Int32)(--e)] >> NUM_BITS)] >> NUM_BITS) + 1;
                p[(Int32)e] = (p[(Int32)e] & ((1U << NUM_BITS) - 1)) | (len << NUM_BITS);
                if (len >= maxLen)
                {
                    len = maxLen - 1;
                    while (lenCounters[(Int32)len] == 0)
                        --len;
                }
                lenCounters[(Int32)len]--;
                lenCounters[(Int32)len + 1] += 2;
            }
            {
                var i = 0;
                for (var len = maxLen; len != 0; len--)
                {
                    UInt32 k;
                    for (k = lenCounters[(Int32)len]; k != 0; k--)
                        lens[(Int32)(p[i++] & ((1U << NUM_BITS) - 1))] = (Byte)len;
                }
            }
            {
                Span<UInt32> nextCodes = stackalloc UInt32[kMaxLen + 1];
                UInt32 code = 0;
                for (var len = 1; len <= kMaxLen; len++)
                {
                    code = (code + lenCounters[len - 1]) << 1;
                    nextCodes[len] = code;
                }
                for (var k = 0; k < numSymbols; k++)
                    p[k] = nextCodes[lens[k]]++;
            }
        }
    }
}

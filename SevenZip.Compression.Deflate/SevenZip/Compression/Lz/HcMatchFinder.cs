// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility;

namespace SevenZip.Compression.Lz
{
    abstract class HcMatchFinder
        : MatchFinder
    {
        protected HcMatchFinder(UInt32 numHashBytes, UInt32 historySize, UInt32 keepAddBufferBefore, UInt32 matchMaxLen, UInt32 keepAddBufferAfter, UInt32 matchFinderCycles)
            : base(false, numHashBytes, historySize, keepAddBufferBefore, matchMaxLen, keepAddBufferAfter, matchFinderCycles)
        {
        }

        protected static UInt32 GetMatchesSpec(UInt32 lenLimit, UInt32 curMatch, UInt32 pos, ReadOnlyArrayPointer<Byte> cur, UInt32[] son, UInt32 cyclicBufferPos, UInt32 cyclicBufferSize, UInt32 cutValue, Span<UInt32> distances, UInt32 distanceOffset, UInt32 maxLen)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            son[cyclicBufferPos] = curMatch;
            do
            {
                if (curMatch == 0)
                    break;
                var delta = pos - curMatch;
                if (delta >= cyclicBufferSize)
                    break;
                curMatch =
                    son[
                       cyclicBufferPos >= delta
                        ? cyclicBufferPos - delta
                        : cyclicBufferSize - delta + cyclicBufferPos
                    ];
                if (cur[maxLen] == cur[maxLen - delta])
                {
                    var len = 0U;
                    while (cur[len] == (cur - delta)[len])
                    {
                        if (++len >= lenLimit)
                        {
                            distances[(Int32)(distanceOffset + 0)] = lenLimit;
                            distances[(Int32)(distanceOffset + 1)] = delta - 1;
                            return distanceOffset + 2;
                        }
                    }
                    if (maxLen < len)
                    {
                        maxLen = len;
                        distances[(Int32)(distanceOffset + 0)] = len;
                        distances[(Int32)(distanceOffset + 1)] = delta - 1;
                        distanceOffset += 2;
                    }
                }
            }
            while (--cutValue > 0);
            return distanceOffset;
        }
    }
}

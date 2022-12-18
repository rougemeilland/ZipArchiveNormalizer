// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility;

namespace SevenZip.Compression.Lz
{
    abstract class BtMatchFinder
        : MatchFinder
    {
        protected BtMatchFinder(UInt32 numHashBytes, UInt32 historySize, UInt32 keepAddBufferBefore, UInt32 matchMaxLen, UInt32 keepAddBufferAfter, UInt32 matchFinderCycles)
            : base(true, numHashBytes, historySize, keepAddBufferBefore, matchMaxLen, keepAddBufferAfter, matchFinderCycles)
        {
        }

        protected static UInt32 GetMatchesSpec(UInt32 lenLimit, UInt32 curMatch, UInt32 pos, ReadOnlyArrayPointer<Byte> cur, UInt32[] son, UInt32 cyclicBufferPos, UInt32 cyclicBufferSize, UInt32 cutValue, Span<UInt32> distances, UInt32 distanceOffset, UInt32 maxLen)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            var ptr0 = (cyclicBufferPos << 1) + 1;
            var ptr1 = cyclicBufferPos << 1;
            var len0 = 0U;
            var len1 = 0U;
            var cmCheck = pos - cyclicBufferSize;
            if (pos <= cyclicBufferSize)
                cmCheck = 0;
            if (cmCheck < curMatch)
            {
                do
                {
                    var delta = pos - curMatch;
                    var pair =
                        cyclicBufferPos >= delta
                        ? ((cyclicBufferPos - delta) << 1)
                        : ((cyclicBufferSize - delta + cyclicBufferPos) << 1);
                    var pb = cur - delta;
                    var len = len0.Minimum(len1);
                    if (pb[len] == cur[len])
                    {
                        if (++len < lenLimit && pb[len] == cur[len])
                        {
                            while (++len < lenLimit)
                            {
                                if (pb[len] != cur[len])
                                    break;
                            }
                        }
                        if (maxLen < len)
                        {
                            maxLen = len;
                            distances[(Int32)distanceOffset++] = len;
                            distances[(Int32)distanceOffset++] = delta - 1;
                            if (len >= lenLimit)
                            {
                                (son[ptr1], son[ptr0]) = (son[pair + 0], son[pair + 1]);
                                return distanceOffset;
                            }
                        }
                    }
                    if (pb[len] < cur[len])
                    {
                        son[ptr1] = curMatch;

                        curMatch = son[pair + 1];
                        ptr1 = pair + 1;
                        len1 = len;
                    }
                    else
                    {
                        son[ptr0] = curMatch;
                        curMatch = son[pair];
                        ptr0 = pair;
                        len0 = len;
                    }
                }
                while (--cutValue > 0 && cmCheck < curMatch);
            }
            son[ptr0] = 0;
            son[ptr1] = 0;
            return distanceOffset;
        }

        protected static void SkipMatchesSpec(UInt32 lenLimit, UInt32 curMatch, UInt32 pos, ReadOnlyArrayPointer<Byte> cur, UInt32[] son, UInt32 cyclicBufferPos, UInt32 cyclicBufferSize, UInt32 cutValue)
        {
            var ptr0 = (cyclicBufferPos << 1) + 1;
            var ptr1 = cyclicBufferPos << 1;
            var len0 = 0U;
            var len1 = 0U;
            var cmCheck = pos > cyclicBufferSize ? pos - cyclicBufferSize : 0;
            if (cmCheck < curMatch)
            {
                do
                {
                    var delta = pos - curMatch;
                    var pair =
                        cyclicBufferPos >= delta
                        ? (cyclicBufferPos - delta) << 1
                        : (cyclicBufferSize - delta + cyclicBufferPos) << 1;
                    var pb = cur - delta;
                    var len = len0.Minimum(len1);
                    if (pb[len] == cur[len])
                    {
                        while (++len < lenLimit)
                        {
                            if (pb[len] != cur[len])
                                break;
                        }
                        if (len >= lenLimit)
                        {
                            (son[ptr1], son[ptr0]) = (son[pair + 0], son[pair + 1]);
                            return;
                        }
                    }
                    if (pb[len] < cur[len])
                    {
                        son[ptr1] = curMatch;
                        curMatch = son[pair + 1];
                        ptr1 = pair + 1;
                        len1 = len;
                    }
                    else
                    {
                        son[ptr0] = curMatch;
                        curMatch = son[pair + 0];
                        ptr0 = pair;
                        len0 = len;
                    }
                }
                while (--cutValue > 0 && cmCheck < curMatch);
            }
            son[ptr0] = 0;
            son[ptr1] = 0;
        }
    }
}

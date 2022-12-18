// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

// The following code is not used.

using System;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lz
{
    // 
    class Bt2MatchFinder
        : BtMatchFinder
    {
        public Bt2MatchFinder(UInt32 historySize, UInt32 keepAddBufferBefore, UInt32 matchMaxLen, UInt32 keepAddBufferAfter, UInt32 matchFinderCycles)
            : base(2, historySize, keepAddBufferBefore, matchMaxLen, keepAddBufferAfter, matchFinderCycles)
        {
        }

        public override UInt32 GetMatches(IBasicInputByteStream inStream, Span<UInt32> distances)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            var distanceOffset = 0U;
            if (LenLimit < 2)
            {
                MovePos(inStream);
                return distanceOffset;
            }
            var hv = BufferPointer.ToUInt16LE();
            var curMatch = Hash[hv];
            Hash[hv] = Pos;
            distanceOffset =
                GetMatchesSpec(
                    LenLimit,
                    curMatch,
                    Pos++,
                    BufferPointer++,
                    Son,
                    CyclicBufferPos++,
                    CyclicBufferSize,
                    CutValue,
                    distances,
                    distanceOffset,
                    1U);
            if (Pos >= PosLimit)
                CheckLimits(inStream);
            return distanceOffset;
        }

        public override void Skip(IBasicInputByteStream inStream, UInt32 num)
        {
            do
            {
                if (LenLimit < 2)
                    MovePos(inStream);
                else
                {
                    var hv = BufferPointer.ToUInt16LE();
                    var curMatch = Hash[hv];
                    Hash[hv] = Pos;
                    SkipMatchesSpec(
                        LenLimit,
                        curMatch,
                        Pos++,
                        BufferPointer++,
                        Son,
                        CyclicBufferPos++,
                        CyclicBufferSize,
                        CutValue);
                    if (Pos >= PosLimit)
                        CheckLimits(inStream);
                }
            }
            while (--num > 0);
        }
    }
}

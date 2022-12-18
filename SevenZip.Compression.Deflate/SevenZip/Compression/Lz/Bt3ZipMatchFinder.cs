// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility.IO;

namespace SevenZip.Compression.Lz
{
    class Bt3ZipMatchFinder
        : BtMatchFinder
    {
        public Bt3ZipMatchFinder(UInt32 historySize, UInt32 keepAddBufferBefore, UInt32 matchMaxLen, UInt32 keepAddBufferAfter, UInt32 matchFinderCycles)
            : base(3, historySize, keepAddBufferBefore, matchMaxLen, keepAddBufferAfter, matchFinderCycles)
        {
        }

        public override UInt32 GetMatches(IBasicInputByteStream inStream, Span<UInt32> distances)
        {
            var distanceOffset = 0U;
            if (LenLimit < 3)
            {
                MovePos(inStream);
                return distanceOffset;
            }
            var crc = LzConstants.Crc.Span;
            var hv = (UInt16)((BufferPointer[2] | ((UInt32)BufferPointer[0] << 8)) ^ crc[BufferPointer[1]]);
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
                    2U);
            if (Pos >= PosLimit)
                CheckLimits(inStream);
            return distanceOffset;
        }

        public override void Skip(IBasicInputByteStream inStream, UInt32 num)
        {
            var crc = LzConstants.Crc.Span;
            do
            {
                if (LenLimit < 3)
                    MovePos(inStream);
                else
                {
                    var hv = ((BufferPointer[2] | ((UInt32)BufferPointer[0] << 8)) ^ crc[BufferPointer[1]]) & UInt16.MaxValue;
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

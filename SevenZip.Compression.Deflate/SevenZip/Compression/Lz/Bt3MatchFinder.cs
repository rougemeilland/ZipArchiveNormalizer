// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

// The following code is not used.

using System;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lz
{
    class Bt3MatchFinder
        : BtMatchFinder
    {
        private readonly UInt32[] _fixedHash2;

        public Bt3MatchFinder(UInt32 historySize, UInt32 keepAddBufferBefore, UInt32 matchMaxLen, UInt32 keepAddBufferAfter, UInt32 matchFinderCycles)
            : base(3, historySize, keepAddBufferBefore, matchMaxLen, keepAddBufferAfter, matchFinderCycles)
        {
            _fixedHash2 = new UInt32[LzConstants.kHash2Size];
        }

        public override UInt32 GetMatches(IBasicInputByteStream inStream, Span<UInt32> distances)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            var distanceOffset = 0U;
            if (LenLimit < 3)
            {
                MovePos(inStream);
                return distanceOffset;
            }
            var crc = LzConstants.Crc.Span;
            var temp = crc[BufferPointer[0]] ^ BufferPointer[1];
            var h2 = temp & (LzConstants.kHash2Size - 1);
            var hv = (temp ^ ((UInt32)BufferPointer[2] << 8)) & HashMask;
            var distance2 = Pos - _fixedHash2[h2];
            var curMatch = Hash[hv];
            _fixedHash2[h2] = Pos;
            Hash[hv] = Pos;
            var mmm = CyclicBufferSize.Minimum(Pos);
            var maxLen = 2U;
            if (distance2 < mmm && (BufferPointer - distance2)[0] == BufferPointer[0])
            {
                while (maxLen < LenLimit)
                {
                    if ((BufferPointer + maxLen - distance2)[0] != (BufferPointer + maxLen)[0])
                        break;
                    ++maxLen;
                }
                distances[(Int32)(distanceOffset + 0)] = maxLen;
                distances[(Int32)(distanceOffset + 1)] = distance2 - 1;
                distanceOffset += 2;
                if (maxLen == LenLimit)
                {
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
                    return distanceOffset;
                }
            }
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
                    maxLen);
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
                    var temp = crc[BufferPointer[0]] ^ BufferPointer[1];
                    var h2 = temp & (LzConstants.kHash2Size - 1);
                    var hv = (temp ^ ((UInt32)BufferPointer[2] << 8)) & HashMask;
                    var curMatch = Hash[hv];
                    _fixedHash2[h2] = Pos;
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

        protected override void Normalize3(UInt32 subValue)
        {
            base.Normalize3(subValue);
            NormalizeHash(subValue, _fixedHash2);
        }
    }
}

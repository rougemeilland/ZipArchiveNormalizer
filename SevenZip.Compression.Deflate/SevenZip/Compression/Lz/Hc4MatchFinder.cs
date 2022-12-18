// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

// The following code is not used.

using System;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lz
{
    class Hc4MatchFinder
        : HcMatchFinder
    {
        private readonly UInt32[] _fixedHash2;
        private readonly UInt32[] _fixedHash3;

        public Hc4MatchFinder(UInt32 historySize, UInt32 keepAddBufferBefore, UInt32 matchMaxLen, UInt32 keepAddBufferAfter, UInt32 matchFinderCycles)
            : base(4, historySize, keepAddBufferBefore, matchMaxLen, keepAddBufferAfter, matchFinderCycles)
        {
            _fixedHash2 = new UInt32[LzConstants.kHash2Size];
            _fixedHash3 = new UInt32[LzConstants.kHash3Size];
        }

        public override UInt32 GetMatches(IBasicInputByteStream inStream, Span<UInt32> distances)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            var distanceOffset = 0U;
            if (LenLimit < 4)
            {
                MovePos(inStream);
                return distanceOffset;
            }
            var crc = LzConstants.Crc.Span;
            var temp = crc[BufferPointer[0]] ^ BufferPointer[1];
            var h2 = temp & (LzConstants.kHash2Size - 1);
            temp ^= (UInt32)BufferPointer[2] << 8;
            var h3 = temp & (LzConstants.kHash3Size - 1);
            var hv = (temp ^ (crc[BufferPointer[3]] << LzConstants.kLzHash_CrcShift_1)) & HashMask;
            var distance2 = Pos - _fixedHash2[h2];
            var distance3 = Pos - _fixedHash3[h3];
            var curMatch = Hash[hv];
            _fixedHash2[h2] = Pos;
            _fixedHash3[h3] = Pos;
            Hash[hv] = Pos;
            var mmm = CyclicBufferSize.Minimum(Pos);
            var maxLen = 3U;
            while (true)
            {
                if (distance2 < mmm && (BufferPointer - distance2)[0] == BufferPointer[0])
                {
                    distances[(Int32)(distanceOffset + 0)] = 2;
                    distances[(Int32)(distanceOffset + 1)] = distance2 - 1;
                    distanceOffset += 2;
                    if ((BufferPointer - distance2)[2] == BufferPointer[2])
                    {

                    }
                    else if (distance3 < mmm && (BufferPointer - distance3)[0] == BufferPointer[0])
                    {
                        distance2 = distance3;
                        distances[(Int32)(distanceOffset + 1)] = distance3 - 1;
                        distanceOffset += 2;
                    }
                    else
                        break;
                }
                else if (distance3 < mmm && (BufferPointer - distance3)[0] == BufferPointer[0])
                {
                    distance2 = distance3;
                    distances[(Int32)(distanceOffset + 1)] = distance3 - 1;
                    distanceOffset += 2;
                }
                else
                    break;
                while (maxLen < LenLimit)
                {
                    if ((BufferPointer + maxLen - distance2)[0] != (BufferPointer + maxLen)[0])
                        break;
                    ++maxLen;
                }
                distances[(Int32)(distanceOffset - 2)] = maxLen;
                if (maxLen >= LenLimit)
                {
                    Son[CyclicBufferPos] = curMatch;
                    ++CyclicBufferPos;
                    ++BufferPointer;
                    if (++Pos >= PosLimit)
                        CheckLimits(inStream);
                    return distanceOffset;
                }
                break;
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
                if (LenLimit < 4)
                {
                    MovePos(inStream);
                    --num;
                }
                else
                {
                    var num2 = num.Minimum(PosLimit - Pos);
                    num -= num2;
                    do
                    {
                        var temp = crc[BufferPointer[0]] ^ BufferPointer[1];
                        var h2 = temp & (LzConstants.kHash2Size - 1);
                        temp ^= (UInt32)BufferPointer[2] << 8;
                        var h3 = temp & (LzConstants.kHash3Size - 1);
                        var hv = (temp ^ (crc[BufferPointer[3]] << LzConstants.kLzHash_CrcShift_1)) & HashMask;
                        var curMatch = Hash[hv];
                        _fixedHash2[h2] = Pos;
                        _fixedHash3[h3] = Pos;
                        Hash[hv] = Pos;
                        ++BufferPointer;
                        ++Pos;
                        Son[CyclicBufferPos++] = curMatch;
                    }
                    while (--num2 > 0);
                    if (Pos >= PosLimit)
                        CheckLimits(inStream);
                }
            } while (num > 0);
        }

        protected override void Normalize3(UInt32 subValue)
        {
            base.Normalize3(subValue);
            NormalizeHash(subValue, _fixedHash2);
            NormalizeHash(subValue, _fixedHash3);
        }
    }
}

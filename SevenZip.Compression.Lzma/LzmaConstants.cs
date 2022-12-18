// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using System;
using Utility;

namespace SevenZip.Compression.Lzma
{
    static class LzmaConstants
    {
        public const UInt32 kNumRepDistances = 4;
        public const UInt32 kNumStates = 12;
        public const Int32 kNumPosSlotBits = 6;
        public const Int32 kDicLogSizeMin = 0;
        public const Int32 kNumLenToPosStatesBits = 2;
        public const UInt32 kNumLenToPosStates = 1 << kNumLenToPosStatesBits;
        public const UInt32 kMatchMinLen = 2;
        public const Int32 kNumAlignBits = 4;
        public const UInt32 kAlignTableSize = 1 << kNumAlignBits;
        public const UInt32 kAlignMask = kAlignTableSize - 1;
        public const UInt32 kStartPosModelIndex = 4;
        public const UInt32 kEndPosModelIndex = 14;
        public const UInt32 kNumPosModels = kEndPosModelIndex - kStartPosModelIndex;
        public const UInt32 kNumFullDistances = 1 << ((Int32)kEndPosModelIndex / 2);
        public const Int32 kNumLitPosStatesBitsEncodingMax = 4;
        public const Int32 kNumLitContextBitsMax = 8;
        public const Int32 kNumPosStatesBitsMax = 4;
        public const UInt32 kNumPosStatesMax = 1 << kNumPosStatesBitsMax;
        public const Int32 kNumPosStatesBitsEncodingMax = 4;
        public const UInt32 kNumPosStatesEncodingMax = 1 << kNumPosStatesBitsEncodingMax;
        public const Int32 kNumLowLenBits = 3;
        public const Int32 kNumMidLenBits = 3;
        public const Int32 kNumHighLenBits = 8;
        public const UInt32 kNumLowLenSymbols = 1 << kNumLowLenBits;
        public const UInt32 kNumMidLenSymbols = 1 << kNumMidLenBits;
        public const UInt32 kNumLenSymbols = kNumLowLenSymbols + kNumMidLenSymbols + (1 << kNumHighLenBits);
        public const UInt32 kMatchMaxLen = kMatchMinLen + kNumLenSymbols - 1;

        public static UInt32 GetLenToPosState(this UInt32 len) => (len - kMatchMaxLen).Minimum(kNumLenToPosStates - 1);
    }
}

// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility;

namespace SevenZip.Compression.Deflate
{
    class DeflateConstants
    {
        public const Int32 kNumHuffmanBits = 15;
        public const UInt32 kHistorySize32 = 1 << 15;
        public const UInt32 kHistorySize64 = 1 << 16;
        public const Int32 kDistTableSize32 = 30;
        public const Int32 kDistTableSize64 = 32;
        public const Int32 kNumLenSymbols32 = 256;
        public const UInt32 kNumLenSymbols64 = 255;
        public const UInt32 kNumLenSymbolsMax = kNumLenSymbols32;
        public const Int32 kNumLenSlots = 29;
        public const Int32 kFixedDistTableSize = 32;
        public const Int32 kFixedLenTableSize = 31;
        public const Int32 kSymbolEndOfBlock = 0x100;
        public const Int32 kSymbolMatch = kSymbolEndOfBlock + 1;
        public const Int32 kMainTableSize = kSymbolMatch + kNumLenSlots;
        public const Int32 kFixedMainTableSize = kSymbolMatch + kFixedLenTableSize;
        public const Int32 kLevelTableSize = 19;
        public const Int32 kTableDirectLevels = 16;
        public const Int32 kTableLevelRepNumber = kTableDirectLevels;
        public const Int32 kTableLevel0Number = kTableLevelRepNumber + 1;
        public const Int32 kTableLevel0Number2 = kTableLevel0Number + 1;
        public const UInt32 kLevelMask = 0xF;
        // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
        public static readonly ReadOnlyMemory<Byte> kLenStart32 = new Byte[kFixedLenTableSize] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 16, 20, 24, 28, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 255, 0, 0 };
        public static readonly ReadOnlyMemory<Byte> kLenStart64 = new Byte[kFixedLenTableSize] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 16, 20, 24, 28, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 0, 0, 0 };
        public static readonly ReadOnlyMemory<Byte> kLenDirectBits32 = new Byte[kFixedLenTableSize] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0, 0, 0 };
        public static readonly ReadOnlyMemory<Byte> kLenDirectBits64 = new Byte[kFixedLenTableSize] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 16, 0, 0 };
        public static readonly ReadOnlyMemory<UInt32> kDistStart = new UInt32[kDistTableSize64] { 0, 1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64, 96, 128, 192, 256, 384, 512, 768, 1024, 1536, 2048, 3072, 4096, 6144, 8192, 12288, 16384, 24576, 32768, 49152 };
        public static readonly ReadOnlyMemory<Byte> kDistDirectBits = new Byte[kDistTableSize64] { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14 };
        public static readonly ReadOnlyMemory<Byte> kLevelDirectBits = new Byte[3] { 2, 3, 7 };
        public static readonly ReadOnlyMemory<Byte> kCodeLengthAlphabetOrder = new Byte[kLevelTableSize] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
        public const Int32 kMatchMinLen = 3;
        public const Int32 kMatchMaxLen32 = kNumLenSymbols32 + kMatchMinLen - 1;
        public const UInt32 kMatchMaxLen64 = kNumLenSymbols64 + kMatchMinLen - 1;
        public const Int32 kMatchMaxLen = kMatchMaxLen32;
        public const Int32 kFinalBlockFieldSize = 1;
        public const Int32 kBlockTypeFieldSize = 2;
        public const Int32 kNumLenCodesFieldSize = 5;
        public const Int32 kNumDistCodesFieldSize = 5;
        public const Int32 kNumLevelCodesFieldSize = 4;
        public const Int32 kNumLitLenCodesMin = 257;
        public const Int32 kNumDistCodesMin = 1;
        public const Int32 kNumLevelCodesMin = 4;
        public const Int32 kLevelFieldSize = 3;
        public const Int32 kStoredBlockLengthFieldSize = 16;

        public static class FinalBlockField
        {
            public const UInt32 kNotFinalBlock = 0;
            public const UInt32 kFinalBlock = 1;
        };

        public static class BlockType
        {
            public const UInt32 kStored = 0;
            public const UInt32 kFixedHuffman = 1;
            public const UInt32 kDynamicHuffman = 2;
        }
    }
}

// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility;

namespace SevenZip.Compression.Deflate.Encoder
{
    class DeflateEncoderConstants
        : DeflateConstants
    {
        public const Int32 kNumOptsBase = 1 << 12;
        public const UInt32 kNumOpts = kNumOptsBase + kMatchMaxLen;
        public const Int32 kNumDivPassesMax = 10;
        public const Int32 kNumTables = 1 << kNumDivPassesMax;
        public const Int32 kFixedHuffmanCodeBlockSizeMax = 1 << 8;
        public const Int32 kDivideCodeBlockSizeMin = 1 << 7;
        public const Int32 kDivideBlockSizeMin = 1 << 6;
        public const Int32 kMaxUncompressedBlockSize = ((1 << 16) - 1) * 1;
        public const Int32 kMatchArraySize = kMaxUncompressedBlockSize * 10;
        public const UInt32 kMatchArrayLimit = kMatchArraySize - kMatchMaxLen * 4 * sizeof(UInt16);
        public const UInt32 kBlockUncompressedSizeThreshold = kMaxUncompressedBlockSize - kMatchMaxLen - kNumOpts;
        public const Int32 kMaxLevelBitLength = 7;
        public const Byte kNoLiteralStatPrice = 11;
        public const Byte kNoLenStatPrice = 11;
        public const Byte kNoPosStatPrice = 6;
        public const Int32 kNumLogBits = 9;
        public const Int32 kIfinityPrice = 0xFFFFFFF;
        // TODO: 正常動作を確認後、LenSlotsやFastPosを配列に置き換えることを考慮に入れて高速化を試みる。
        public readonly static ReadOnlyMemory<Byte> LenSlots;
        public readonly static ReadOnlyMemory<Byte> FastPos;

        static DeflateEncoderConstants()
        {
            var kLenStart32 = DeflateConstants.kLenStart32.Span;
            var kLenDirectBits32 = DeflateConstants.kLenDirectBits32.Span;
            var lenSlots = new Byte[kNumLenSymbolsMax];
            for (var i = 0; i < kNumLenSlots; i++)
                lenSlots.FillArray((Byte)i, kLenStart32[i], 1 << kLenDirectBits32[i]);
            LenSlots = lenSlots;

            var fastPos = new Byte[1 << kNumLogBits];
            const UInt32 kFastSlots = kNumLogBits * 2;
            var kDistDirectBits = DeflateConstants.kDistDirectBits.Span;
            var fastPosOffset = 0;
            var slotFast = (Byte)0;
            do
            {
                var length = 1 << kDistDirectBits[slotFast];
                fastPos.FillArray(
                    slotFast,
                    fastPosOffset,
                    length);
                fastPosOffset += length;
            }
            while (++slotFast < kFastSlots);
            FastPos = fastPos.AsReadOnly();
        }
    }
}

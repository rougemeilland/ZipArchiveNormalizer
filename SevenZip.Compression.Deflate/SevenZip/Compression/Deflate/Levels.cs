// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility;

namespace SevenZip.Compression.Deflate
{
    class Levels
    {
        public Levels()
        {
            LitLenLevels = new Byte[DeflateConstants.kFixedMainTableSize];
            DistLevels = new Byte[DeflateConstants.kFixedDistTableSize];
        }

        public Byte[] LitLenLevels { get; }
        public Byte[] DistLevels { get; }

        public void SubClear()
        {
            LitLenLevels.ClearArray(
                DeflateConstants.kNumLitLenCodesMin,
                DeflateConstants.kFixedMainTableSize - DeflateConstants.kNumLitLenCodesMin);
            DistLevels.ClearArray();
        }

        public void SetFixedLevels()
        {
            LitLenLevels.FillArray((Byte)8, 0, 144 - 0);
            LitLenLevels.FillArray((Byte)9, 144, 256 - 144);
            LitLenLevels.FillArray((Byte)7, 256, 280 - 256);
            LitLenLevels.FillArray((Byte)8, 280, 288 - 280);
            DistLevels.FillArray((Byte)5);
        }

        public void SetLitLenLevels(ReadOnlySpan<Byte> array)
        {
            array.CopyTo(LitLenLevels);
        }

        public void SetDistLevels(ReadOnlySpan<Byte> array)
        {
            array.CopyTo(DistLevels);
        }
    }
}

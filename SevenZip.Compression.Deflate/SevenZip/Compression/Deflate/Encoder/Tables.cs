// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility;

namespace SevenZip.Compression.Deflate.Encoder
{
    class Tables : Levels
    {
        public Tables()
        {
            UseSubBlocks = false;
            StoreMode = false;
            StaticMode = false;
            BlockSizeRes = 0;
            Pos = 0;
        }

        public bool UseSubBlocks { get; set; }
        public bool StoreMode { get; set; }
        public bool StaticMode { get; set; }
        public UInt32 BlockSizeRes { get; set; }
        public UInt32 Pos { get; set; }

        public void Initialize()
        {
            LitLenLevels.FillArray((Byte)8, 0, 256 - 0);
            LitLenLevels[256] = 13;
            LitLenLevels.FillArray((Byte)5, 257);
            DistLevels.FillArray((Byte)5);
        }
    }
}

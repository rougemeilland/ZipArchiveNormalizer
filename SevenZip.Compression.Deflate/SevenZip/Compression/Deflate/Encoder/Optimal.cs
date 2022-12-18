// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;

namespace SevenZip.Compression.Deflate.Encoder
{
    class Optimal
    {
        public Optimal()
        {
            Price = 0;
            PosPrev = 0;
            BackPrev = 0;
        }

        public UInt32 Price { get; set; }
        public UInt16 PosPrev { get; set; }
        public UInt16 BackPrev { get; set; }
    }
}

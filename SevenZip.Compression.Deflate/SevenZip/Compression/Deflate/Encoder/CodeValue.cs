// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;

namespace SevenZip.Compression.Deflate.Encoder
{
    class CodeValue
    {
        public CodeValue()
        {
            Len = 0;
            Pos = 0;
        }

        public void SetAsLiteral() => Len = 1 << 15;
        public bool IsLiteral => Len >= (1 << 15);

        public UInt16 Len { get; set; }
        public UInt16 Pos { get; set; }
    }
}

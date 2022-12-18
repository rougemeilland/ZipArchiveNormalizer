// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;

namespace SevenZip.Compression.BitLsb
{
    class InBuffer
        : InBufferBase, IInBuffer
    {
        public InBuffer(UInt32 bufSize)
            : base(bufSize)
        {
        }
    }
}

// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility.IO;

namespace SevenZip.Compression.Huffman
{
    interface IBitDecoder
    {
        UInt32 GetValue(IBasicInputByteStream inStream, Int32 numBits);
        void MovePos(Int32 numBits);
    }
}

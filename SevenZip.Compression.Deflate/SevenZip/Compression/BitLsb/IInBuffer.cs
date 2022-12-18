// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility.IO;

namespace SevenZip.Compression.BitLsb
{
    interface IInBuffer
    {
        UInt64 StreamSize { get; }
        UInt64 ProcessedSize { get; }
        UInt32 NumExtraBytes { get; }
        Byte ReadByte(IBasicInputByteStream inStream);
        bool ReadByteFromBuf(out Byte data);
    }
}

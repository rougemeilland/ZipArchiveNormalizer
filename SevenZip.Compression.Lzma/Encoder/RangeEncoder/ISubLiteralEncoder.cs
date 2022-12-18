// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using System;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Encoder.RangeEncoder
{
    interface ISubLiteralEncoder
    {
        void Encode(IBasicOutputByteStream outStream, Encoder rangeEncoder, Byte symbol);
        void EncodeMatched(IBasicOutputByteStream outStream, Encoder rangeEncoder, Byte matchByte, Byte symbol);
        UInt32 GetPrice(bool matchMode, Byte matchByte, Byte symbol);
    }
}
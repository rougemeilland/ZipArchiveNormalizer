using System;

namespace ZipUtility.ZipFileHeader
{
    [Flags]
    enum ZipFileEntryGeneralPurposeBitFlag
        : UInt16
    {
        None = 0,
        UseUnicodeEncodingForNameAndComment = (UInt16)1 << 11,
    }
}
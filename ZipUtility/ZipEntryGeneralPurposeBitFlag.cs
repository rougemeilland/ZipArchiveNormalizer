using System;

namespace ZipUtility
{
    [Flags]
    public enum ZipEntryGeneralPurposeBitFlag
        : UInt16
    {
        None = 0,
        IsEncrypted = (UInt16)1 << 0,
        HasDataDescriptor = (UInt16)1 << 3,
        IsCompressedPatchedData = (UInt16)1 << 5,
        IsStrongEncrypted = (UInt16)1 << 6,
        UseUnicodeEncodingForNameAndComment = (UInt16)1 << 11,
        IsMoreStrongEncrypted = (UInt16)1 << 13
    }
}
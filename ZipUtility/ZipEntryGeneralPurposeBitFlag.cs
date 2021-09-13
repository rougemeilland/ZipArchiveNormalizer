using System;

namespace ZipUtility
{
    [Flags]
    public enum ZipEntryGeneralPurposeBitFlag
        : UInt16
    {
        None = 0,
        Encrypted = (UInt16)1 << 0,
        CompresssionOption0 = (UInt16)1 << 1,
        CompresssionOption1 = (UInt16)1 << 2,
        HasDataDescriptor = (UInt16)1 << 3,
        CompressedPatchedData = (UInt16)1 << 5,
        StrongEncrypted = (UInt16)1 << 6,
        UseUnicodeEncodingForNameAndComment = (UInt16)1 << 11,
        EncryptedCentralDirectory = (UInt16)1 << 13
    }
}
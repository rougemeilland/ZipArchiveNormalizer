using System;

namespace ZipUtility
{
    [Flags]
    public enum  ZipEntryEncryptionFlag
        : UInt16
    {
        RequiredPassword = 1 << 0,
        RequiredCertification = 1 << 1,
    }
}

using System;

namespace ZipUtility
{
    static class EnumExtensions
    {
        public static bool HasEncryptionFlag(this ZipEntryGeneralPurposeBitFlag flag)
        {
            return
                (flag &
                    (ZipEntryGeneralPurposeBitFlag.Encrypted |
                     ZipEntryGeneralPurposeBitFlag.EncryptedCentralDirectory |
                     ZipEntryGeneralPurposeBitFlag.StrongEncrypted))
                != ZipEntryGeneralPurposeBitFlag.None;
        }

        public static int GetCompressionOptionValue(this ZipEntryGeneralPurposeBitFlag flag)
        {
            return ((UInt16)flag >> 1) & 3;
        }
    }
}

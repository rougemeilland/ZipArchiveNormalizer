namespace ZipUtility
{
    public static class ZipEntryCompressionMethodIdExtensions
    {
        public static ZipEntryCompressionMethod GetCompressionMethod(this ZipEntryCompressionMethodId compressionMethodId, ZipEntryGeneralPurposeBitFlag flag)
        {
            return ZipEntryCompressionMethod.GetCompressionMethod(compressionMethodId, flag);
        }
    }
}
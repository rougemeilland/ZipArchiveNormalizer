namespace ZipUtility
{
    public enum ZipFileCheckResult
    {
        Ok,
        Corrupted,
        Encrypted,
        UnsupportedCompressionMethod,
        UnsupportedFunction,
    }
}

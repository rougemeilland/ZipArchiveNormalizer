namespace ZipUtility.IO.Compression
{
    public interface ICompressionCoder
    {
        CompressionMethodId CompressionMethodId { get; }
        ICoderOption DefaultOption { get; }
        ICoderOption GetOptionFromGeneralPurposeFlag(bool bit1, bool bit2);
    }
}
namespace ZipUtility.IO.Compression.BZip2
{
    public class BZip2CoderPlugin
        : ICompressionCoder
    {
        public CompressionMethodId CompressionMethodId => CompressionMethodId.BZIP2;

        ICoderOption ICompressionCoder.DefaultOption => CompressionOption.EmptyOption;

        ICoderOption ICompressionCoder.GetOptionFromGeneralPurposeFlag(bool bit1, bool bit2) =>
            CompressionOption.EmptyOption;
    }
}

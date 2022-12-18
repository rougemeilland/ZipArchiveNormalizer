namespace ZipUtility.IO.Compression.Ppmd
{
    public class PpmdCoderPlugin
        : ICompressionCoder
    {
        public CompressionMethodId CompressionMethodId => CompressionMethodId.PPMd;

        ICoderOption ICompressionCoder.DefaultOption => CompressionOption.EmptyOption;

        ICoderOption ICompressionCoder.GetOptionFromGeneralPurposeFlag(bool bit1, bool bit2) =>
            CompressionOption.EmptyOption;
    }
}

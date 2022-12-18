namespace ZipUtility.IO.Compression.Deflate64
{
    public abstract class Deflate64CoderPlugin
        : ICompressionCoder
    {
        public CompressionMethodId CompressionMethodId => CompressionMethodId.Deflate64;

        public ICoderOption DefaultOption =>
            new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.Normal };

        public ICoderOption GetOptionFromGeneralPurposeFlag(bool bit1, bool bit2) =>
            bit2
                ? bit1
                    ? new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.SuperFast }
                    : new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.Fast }
                : bit1
                    ? new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.Maximum }
                    : new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.Normal };
    }
}

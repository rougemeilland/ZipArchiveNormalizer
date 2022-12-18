using System;

namespace ZipUtility.IO.Compression.Lzma
{
    public class LzmaCoderPlugin
        : ICompressionCoder
    {
        public CompressionMethodId CompressionMethodId => CompressionMethodId.LZMA;

        public ICoderOption DefaultOption =>
            new LzmaCompressionOption { UseEndOfStreamMarker = true };

        public ICoderOption GetOptionFromGeneralPurposeFlag(bool bit1, bool bit2) =>
            new LzmaCompressionOption { UseEndOfStreamMarker = bit1 };
    }
}

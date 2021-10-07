using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Lzma
{
    public class LZMACompressionMethod
        : ICompressionMethod
    {
        public CompressionMethodId CompressionMethodId => CompressionMethodId.LZMA;

        public ICompressionOption CreateOptionFromGeneralPurposeFlag(bool bit1, bool bit2)
        {
            return new LzmaCompressionOption { UseEndOfStreamMarker = bit1 };
        }

        public IInputByteStream<UInt64> GetDecodingStream(IInputByteStream<UInt64> baseStream, ICompressionOption option, ulong size, ICodingProgressReportable progressReporter)
        {
            if (!(option is LzmaCompressionOption))
                throw new ArgumentException();
            var useEndOfStreamMarker = (option as LzmaCompressionOption)?.UseEndOfStreamMarker ?? true;
            return new LzmaDecodingStream(baseStream, size, progressReporter);
        }

        public IOutputByteStream<UInt64> GetEncodingStream(IOutputByteStream<UInt64> baseStream, ICompressionOption option, ulong? size, ICodingProgressReportable progressReporter)
        {
            if (!(option is LzmaCompressionOption))
                throw new ArgumentException();
            var useEndOfStreamMarker = (option as LzmaCompressionOption)?.UseEndOfStreamMarker ?? true;
            return new LzmaEncodingStream(baseStream, useEndOfStreamMarker, size, progressReporter);
        }
    }
}
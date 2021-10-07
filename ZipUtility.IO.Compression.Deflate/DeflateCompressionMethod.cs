using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Deflate
{
    public class DeflateCompressionMethod
        : ICompressionMethod
    {
        public CompressionMethodId CompressionMethodId => CompressionMethodId.Deflate;

        public ICompressionOption CreateOptionFromGeneralPurposeFlag(bool bit1, bool bit2)
        {
            if (bit2)
            {
                if (bit1)
                    return new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.SuperFast };
                else
                    return new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.Fast };
            }
            else
            {
                if (bit1)
                    return new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.Maximum };
                else
                    return new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.Normal };
            }
        }

        public IInputByteStream<UInt64> GetDecodingStream(IInputByteStream<UInt64> baseStream, ICompressionOption option, ulong size, ICodingProgressReportable progressReporter)
        {
            return new DeflateDecodingStream(baseStream, size, progressReporter);
        }

        public IOutputByteStream<UInt64> GetEncodingStream(IOutputByteStream<UInt64> baseStream, ICompressionOption option, ulong? size, ICodingProgressReportable progressReporter)
        {
            if (!(option is DeflateCompressionOption))
                throw new ArgumentException();
            var compressionLevel = (option as DeflateCompressionOption)?.CompressionLevel ?? DeflateCompressionLevel.Normal;
            if (compressionLevel < DeflateCompressionLevel.Minimum || compressionLevel > DeflateCompressionLevel.Maximum)
                throw new ArgumentException();
            return new DeflateEncodingStream(baseStream, (int)compressionLevel, size, progressReporter);
        }
    }
}
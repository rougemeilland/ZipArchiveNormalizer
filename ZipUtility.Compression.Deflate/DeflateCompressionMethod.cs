using System;
using System.IO;
using ZipUtility.Compression.Deflate.DataStream;

namespace ZipUtility.Compression.Deflate
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

        public Stream GetInputStream(Stream baseStream, ICompressionOption option, long? offset, long packedSize, long size, bool leaveOpen)
        {
            if (offset.HasValue && baseStream.CanSeek == false)
                throw new IOException("ZIP stream can not seek.");
            return new DeflateInputStream(baseStream, offset, packedSize, size, leaveOpen);
        }

        public Stream GetOutputStream(Stream baseStream, ICompressionOption option, long? offset, long? size, bool leaveOpen)
        {
            if (offset.HasValue && baseStream.CanSeek == false)
                throw new IOException("ZIP stream can not seek.");
            if (!(option is DeflateCompressionOption))
                throw new ArgumentException();
            var compressionLevel = (option as DeflateCompressionOption)?.CompressionLevel ?? DeflateCompressionLevel.Normal;
            if (compressionLevel < DeflateCompressionLevel.Minimum || compressionLevel > DeflateCompressionLevel.Maximum)
                throw new ArgumentException();
            return new InflateStream(baseStream, (int)compressionLevel, offset, size, leaveOpen);
        }
    }
}
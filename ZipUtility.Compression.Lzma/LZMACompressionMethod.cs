using System.IO;
using System;
using ZipUtility.Compression.Lzma.DataStream;

namespace ZipUtility.Compression.Lzma
{
    public class LZMACompressionMethod
        : ICompressionMethod
    {
        public CompressionMethodId CompressionMethodId => CompressionMethodId.LZMA;

        public ICompressionOption CreateOptionFromGeneralPurposeFlag(bool bit1, bool bit2)
        {
            return new LzmaCompressionOption { UseEndOfStreamMarker = bit1 };
        }

        public Stream GetInputStream(Stream baseStream, ICompressionOption option, long? offset, long packedSize, long size, bool leaveOpen)
        {
            if (!(option is LzmaCompressionOption))
                throw new ArgumentException();
            var useEndOfStreamMarker = (option as LzmaCompressionOption)?.UseEndOfStreamMarker ?? true;
            return new LzmaInputStream(baseStream, useEndOfStreamMarker, offset, packedSize, size, leaveOpen);
        }

        public Stream GetOutputStream(Stream baseStream, ICompressionOption option, long? offset, long? size, bool leaveOpen)
        {
            if (!(option is LzmaCompressionOption))
                throw new ArgumentException();
            var useEndOfStreamMarker = (option as LzmaCompressionOption)?.UseEndOfStreamMarker ?? true;
            return new LzmaOutputStream(baseStream, useEndOfStreamMarker, offset, size, leaveOpen);
        }
    }
}
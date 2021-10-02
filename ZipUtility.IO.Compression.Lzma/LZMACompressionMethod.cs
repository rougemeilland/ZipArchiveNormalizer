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

        public IInputByteStream<UInt64> GetInputStream(IInputByteStream<UInt64> baseStream, ICompressionOption option, ulong size)
        {
            if (!(option is LzmaCompressionOption))
                throw new ArgumentException();
            var useEndOfStreamMarker = (option as LzmaCompressionOption)?.UseEndOfStreamMarker ?? true;
            return new LzmaInputStream(baseStream, size);
        }

        public IOutputByteStream<UInt64> GetOutputStream(IOutputByteStream<UInt64> baseStream, ICompressionOption option, ulong? size)
        {
            if (!(option is LzmaCompressionOption))
                throw new ArgumentException();
            var useEndOfStreamMarker = (option as LzmaCompressionOption)?.UseEndOfStreamMarker ?? true;
            return new LzmaOutputStream(baseStream, useEndOfStreamMarker, size);
        }
    }
}
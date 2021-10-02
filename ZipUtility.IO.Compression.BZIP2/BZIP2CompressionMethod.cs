using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.BZIP2
{
    public class BZIP2CompressionMethod
        : ICompressionMethod
    {
        private const int _compressionLevel = 9;

        public CompressionMethodId CompressionMethodId => CompressionMethodId.BZIP2;

        public ICompressionOption CreateOptionFromGeneralPurposeFlag(bool bit1, bool bit2) => null;

        public IInputByteStream<UInt64> GetInputStream(IInputByteStream<UInt64> baseStream, ICompressionOption option, ulong size)
        {
            return new Bzip2InputStream(baseStream, size);
        }

        public IOutputByteStream<UInt64> GetOutputStream(IOutputByteStream<UInt64> baseStream, ICompressionOption option, ulong? size)
        {
            return new Bzip2OutputStream(baseStream, _compressionLevel, size);
        }
    }
}

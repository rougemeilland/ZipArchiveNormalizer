using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Stored
{
    public class StoredCompressionMethod
        : ICompressionMethod
    {
        public CompressionMethodId CompressionMethodId => CompressionMethodId.Stored;

        public ICompressionOption CreateOptionFromGeneralPurposeFlag(bool bit1, bool bit2) => null;

        public IInputByteStream<UInt64> GetInputStream(IInputByteStream<UInt64> baseStream, ICompressionOption option, ulong size)
        {
            return baseStream;
        }

        public IOutputByteStream<UInt64> GetOutputStream(IOutputByteStream<UInt64> baseStream, ICompressionOption option, ulong? size)
        {
            return baseStream;
        }
    }
}

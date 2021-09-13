using System;
using System.IO;
using Utility.IO;

namespace ZipUtility.Compression.Stored
{
    public class StoredCompressionMethod
        : ICompressionMethod
    {
        public CompressionMethodId CompressionMethodId => CompressionMethodId.Stored;

        public ICompressionOption CreateOptionFromGeneralPurposeFlag(bool bit1, bool bit2) => null;

        public Stream GetInputStream(Stream baseStream, ICompressionOption option, long? offset, long packedSize, long size, bool leaveOpen)
        {
            if (offset.HasValue && baseStream.CanSeek == false)
                throw new IOException("ZIP stream can not seek.");
            if (packedSize != size)
                throw new ArgumentException();
            return new PartialInputStream(baseStream, offset, packedSize, leaveOpen);
        }

        public Stream GetOutputStream(Stream baseStream, ICompressionOption option, long? offset, long? size, bool leaveOpen)
        {
            if (offset.HasValue && baseStream.CanSeek == false)
                throw new IOException("ZIP stream can not seek.");
            return new PartialOutputStream(baseStream, offset, size, leaveOpen);
        }
    }
}

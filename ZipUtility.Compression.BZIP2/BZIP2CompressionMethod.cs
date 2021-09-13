using System.IO;
using ZipUtility.Compression.BZIP2.DataStream;

namespace ZipUtility.Compression.BZIP2
{
    public class BZIP2CompressionMethod
        : ICompressionMethod
    {
        private const int _compressionLevel = 9;

        public CompressionMethodId CompressionMethodId => CompressionMethodId.BZIP2;

        public ICompressionOption CreateOptionFromGeneralPurposeFlag(bool bit1, bool bit2) => null;

        public Stream GetInputStream(Stream baseStream, ICompressionOption option, long? offset, long packedSize, long size, bool leaveOpen)
        {
            if (offset.HasValue && baseStream.CanSeek == false)
                throw new IOException("ZIP stream can not seek.");
            return new Bzip2InputStream(baseStream, offset, packedSize, size, leaveOpen);
        }

        public Stream GetOutputStream(Stream baseStream, ICompressionOption option, long? offset, long? size, bool leaveOpen)
        {
            if (offset.HasValue && baseStream.CanSeek == false)
                throw new IOException("ZIP stream can not seek.");
            return new Bzip2OutputStream(baseStream, _compressionLevel, offset, size, leaveOpen);
        }
    }
}

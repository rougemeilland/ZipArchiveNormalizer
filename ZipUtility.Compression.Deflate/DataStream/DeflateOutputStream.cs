using System.IO;
using Utility.IO;

namespace ZipUtility.Compression.Deflate.DataStream
{
    public class InflateStream
        : OutputStream
    {
        public InflateStream(Stream baseStream, int level, long? offset, long? size, bool leaveOpen)
            : base(baseStream, offset, size, false)
        {
            SetDestinationStream(
                new Ionic.Zlib.DeflateStream(
                    new PartialOutputStream(baseStream, offset, null, leaveOpen),
                    Ionic.Zlib.CompressionMode.Compress,
                    (Ionic.Zlib.CompressionLevel)level,
                    false));
        }
    }
}
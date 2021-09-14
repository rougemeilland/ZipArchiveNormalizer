using System.IO;
using Utility.IO;

namespace ZipUtility.Compression.Deflate.DataStream
{
    public class DeflateInputStream
        : InputStream
    {
        public DeflateInputStream(Stream baseStream, long? offset, long packedSize, long size, bool leaveOpen = false)
            : base(baseStream, offset, packedSize, size, false)
        {
            SetSourceStream(
                new Ionic.Zlib.DeflateStream(
                    new PartialInputStream(baseStream, offset, packedSize, leaveOpen),
                    Ionic.Zlib.CompressionMode.Decompress,
                    false));
        }
    }
}

using System.IO;
using Utility.IO;

namespace ZipUtility.Compression.BZIP2.DataStream
{
    public class Bzip2InputStream
        : InputStream
    {
        public Bzip2InputStream(Stream baseStream, long? offset, long packedSize, long size, bool leaveOpen = false)
            : base(baseStream, offset, packedSize, size, false)
        {
            SetSourceStream(new Bzip2.BZip2InputStream(new BufferedInputStream(new PartialInputStream(baseStream, offset, packedSize, leaveOpen)), false));
        }
    }
}
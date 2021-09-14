using System.IO;
using Utility.IO;

namespace ZipUtility.Compression.BZIP2.DataStream
{
    public class Bzip2OutputStream
        : OutputStream
    {
        public Bzip2OutputStream(Stream baseStream, int level, long? offset, long? size, bool leaveOpen)
            : base(baseStream, offset, size, false)
        {
            SetDestinationStream(new Bzip2.BZip2OutputStream(new BufferedOutputStream(new PartialOutputStream(baseStream, offset, null, leaveOpen)), true, level));
        }

        protected override void FlushDestinationStream(Stream destinationStream, bool isEndOfData)
        {
            // Bzip2.BZip2OutputStream.Flush は例外を返すため、呼び出さない。
            //destinationStream.Flush();
        }
    }
}
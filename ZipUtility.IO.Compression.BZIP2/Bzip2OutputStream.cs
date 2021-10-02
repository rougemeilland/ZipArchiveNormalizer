using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.BZIP2
{
    public class Bzip2OutputStream
        : ZipContentOutputStream
    {
        public Bzip2OutputStream(IOutputByteStream<UInt64> baseStream, int level, ulong? size)
            : base(baseStream, size)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                SetDestinationStream(
                    new Bzip2.BZip2OutputStream(
                        baseStream
                            .WithCache()
                            .AsStream(),
                        true,
                        level)
                    .AsOutputByteStream());
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }

        protected override void FlushDestinationStream(IOutputByteStream<UInt64> destinationStream, bool isEndOfData)
        {
            // Bzip2.BZip2OutputStream.Flush は例外を返すため、呼び出さない。
            //destinationStream.Flush();
        }
    }
}
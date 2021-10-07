using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.BZIP2
{
    class Bzip2EncodingStream
        : ZipContentOutputStream
    {
        public Bzip2EncodingStream(IOutputByteStream<UInt64> baseStream, int level, ulong? size, ICodingProgressReportable progressReporter)
            : base(baseStream, size, progressReporter)
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

        protected override void FlushDestinationStream(IBasicOutputByteStream destinationStream, bool isEndOfData)
        {
            // Bzip2.BZip2OutputStream.Flush は例外を返すため、呼び出さない。
            //destinationStream.Flush();
        }
    }
}
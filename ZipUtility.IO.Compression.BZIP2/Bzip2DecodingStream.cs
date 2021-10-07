using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.BZIP2
{
    class Bzip2DecodingStream
        : ZipContentInputStream
    {
        public Bzip2DecodingStream(IInputByteStream<UInt64> baseStream, ulong size, ICodingProgressReportable progressReporter)
            : base(baseStream, size, progressReporter)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                SetSourceStream(
                    new Bzip2.BZip2InputStream(
                        baseStream
                            .WithCache()
                            .AsStream(),
                        false)
                    .AsInputByteStream());
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }
    }
}
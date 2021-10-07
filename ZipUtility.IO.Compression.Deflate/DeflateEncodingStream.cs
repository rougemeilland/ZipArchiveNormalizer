using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Deflate
{
    class DeflateEncodingStream
        : ZipContentOutputStream
    {
        public DeflateEncodingStream(IOutputByteStream<UInt64> baseStream, int level, ulong? size, ICodingProgressReportable progressReporter)
            : base(baseStream, size, progressReporter)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                SetDestinationStream(
                    new Ionic.Zlib.DeflateStream(
                        baseStream
                            .AsStream(),
                        Ionic.Zlib.CompressionMode.Compress,
                        (Ionic.Zlib.CompressionLevel)level,
                        false)
                    .AsOutputByteStream());
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }
    }
}
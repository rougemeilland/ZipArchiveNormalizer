using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Deflate
{
    class DeflateDecodingStream
        : ZipContentInputStream
    {
        public DeflateDecodingStream(IInputByteStream<UInt64> baseStream, ulong size, ICodingProgressReportable progressReporter)
            : base(baseStream, size, progressReporter)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                SetSourceStream(
                    new Ionic.Zlib.DeflateStream(
                        baseStream
                            .AsStream(),
                        Ionic.Zlib.CompressionMode.Decompress,
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

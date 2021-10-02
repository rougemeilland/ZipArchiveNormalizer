using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Deflate
{
    public class InflateStream
        : ZipContentOutputStream
    {
        public InflateStream(IOutputByteStream<UInt64> baseStream, int level, ulong? size)
            : base(baseStream, size)
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
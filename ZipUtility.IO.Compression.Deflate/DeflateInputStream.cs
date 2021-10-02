using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Deflate
{
    public class DeflateInputStream
        : ZipContentInputStream
    {
        public DeflateInputStream(IInputByteStream<UInt64> baseStream, ulong size)
            : base(baseStream, size)
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

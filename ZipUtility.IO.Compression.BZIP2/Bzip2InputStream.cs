using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.BZIP2
{
    public class Bzip2InputStream
        : ZipContentInputStream
    {
        public Bzip2InputStream(IInputByteStream<UInt64> baseStream, ulong size)
            : base(baseStream, size)
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
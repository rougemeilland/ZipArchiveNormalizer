using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Stored
{
    internal class StoredDecodingStream
        : ZipContentInputStream
    {
        public StoredDecodingStream(IInputByteStream<UInt64> baseStream, ulong size, ICodingProgressReportable progressReporter)
            : base(baseStream, size, progressReporter)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                SetSourceStream(baseStream);
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }
    }
}

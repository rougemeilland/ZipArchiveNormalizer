using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Stored
{
    class StoredEncodingStream
        : ZipContentOutputStream
    {
        public StoredEncodingStream(IOutputByteStream<UInt64> baseStream, ulong? size, ICodingProgressReportable progressReporter)
            : base(baseStream, size, progressReporter)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                SetDestinationStream(baseStream);
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }
    }
}
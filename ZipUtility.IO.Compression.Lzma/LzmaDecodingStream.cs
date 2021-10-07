using System;
using System.IO;
using Utility;
using Utility.IO;

namespace ZipUtility.IO.Compression.Lzma
{
    class LzmaDecodingStream
        : ZipContentInputStream
    {
        public LzmaDecodingStream(IInputByteStream<UInt64> baseStream, ulong size, ICodingProgressReportable progressReporter)
            : base(baseStream, size)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var header = baseStream.ReadBytes(9);
                if (header.Length != 9)
                    throw new IOException("Too short source stream");
                var majorVersion = header[0];
                var minorVersion = header[1];
                var propertyLength = header.ToUInt16LE(2);
                if (propertyLength != 5)
                    throw new IOException("Invalid LZMA property size");
                var properties = new byte[5];
                header.CopyTo(4, properties, 0, 5);

                SetSourceStream(
                    new Utility.IO.Compression.Lzma.LzmaDecodingStream(
                        baseStream,
                        properties.AsReadOnly(),
                        size,
                        progressReporter,
                        false));
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }
    }
}

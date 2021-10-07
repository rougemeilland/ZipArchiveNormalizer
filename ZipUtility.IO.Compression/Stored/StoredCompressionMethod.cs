using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Stored
{
    public class StoredCompressionMethod
        : ICompressionMethod
    {
        public CompressionMethodId CompressionMethodId => CompressionMethodId.Stored;

        public ICompressionOption CreateOptionFromGeneralPurposeFlag(bool bit1, bool bit2) => null;

        public IInputByteStream<UInt64> GetDecodingStream(IInputByteStream<UInt64> baseStream, ICompressionOption option, ulong size, ICodingProgressReportable progressReporter)
        {
            return new StoredDecodingStream(baseStream, size, progressReporter);
        }

        public IOutputByteStream<UInt64> GetEncodingStream(IOutputByteStream<UInt64> baseStream, ICompressionOption option, ulong? size, ICodingProgressReportable progressReporter)
        {
            return new StoredEncodingStream(baseStream, size, progressReporter);
        }
    }
}

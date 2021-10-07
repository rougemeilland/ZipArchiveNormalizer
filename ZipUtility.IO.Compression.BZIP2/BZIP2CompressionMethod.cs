using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.BZIP2
{
    public class BZIP2CompressionMethod
        : ICompressionMethod
    {
        private const int _compressionLevel = 9;

        public CompressionMethodId CompressionMethodId => CompressionMethodId.BZIP2;

        public ICompressionOption CreateOptionFromGeneralPurposeFlag(bool bit1, bool bit2) => null;

        public IInputByteStream<UInt64> GetDecodingStream(IInputByteStream<UInt64> baseStream, ICompressionOption option, ulong size, ICodingProgressReportable progressReporter)
        {
            return new Bzip2DecodingStream(baseStream, size, progressReporter);
        }

        public IOutputByteStream<UInt64> GetEncodingStream(IOutputByteStream<UInt64> baseStream, ICompressionOption option, ulong? size, ICodingProgressReportable progressReporter)
        {
            return new Bzip2EncodingStream(baseStream, _compressionLevel, size, progressReporter);
        }
    }
}

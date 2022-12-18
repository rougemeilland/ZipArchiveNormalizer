using SevenZip.Compression.Deflate.Encoder;
using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Deflate
{
    public class DeflateEncoderPlugin
        : DeflateCoderPlugin, ICompressionHierarchicalEncoder
    {
        private class Encoder
            : HierarchicalEncoder
        {
            public Encoder(IOutputByteStream<UInt64> baseStream, Int32 level, UInt64? unpackedStreamSize, IProgress<UInt64>? progress)
                : base(GetBaseStream(baseStream, level, progress), unpackedStreamSize)
            {
            }

            private static DeflateEncoderStream GetBaseStream(IOutputByteStream<UInt64> baseStream, Int32 level, IProgress<UInt64>? progress)
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return
                    new DeflateEncoderStream(
                        baseStream.WithCache(),
                        new DeflateEncoderProperties
                        {
                            Level = level,
                        },
                        progress,
                        false);
            }

            protected override void FlushDestinationStream(IBasicOutputByteStream destinationStream, bool isEndOfData)
            {
                if (isEndOfData)
                    destinationStream.Dispose();
            }
        }

        public IOutputByteStream<UInt64> GetEncodingStream(IOutputByteStream<UInt64> baseStream, ICoderOption option, UInt64? unpackedStreamSize, IProgress<UInt64>? progress = null)
        {
            if (baseStream is null)
                throw new ArgumentNullException(nameof(baseStream));
            if (option is null)
                throw new ArgumentNullException(nameof(option));
            if (option is not DeflateCompressionOption deflateOption)
                throw new ArgumentException($"Illegal {nameof(option)} data", nameof(option));
            var compressionLevel = deflateOption.CompressionLevel;
            if (compressionLevel < DeflateCompressionLevel.Minimum || compressionLevel > DeflateCompressionLevel.Maximum)
                throw new ArgumentException($"Illegal {nameof(option)}.CompressionLevel value", nameof(option));

            return new Encoder(baseStream, (Int32)compressionLevel, unpackedStreamSize, progress);
        }
    }
}

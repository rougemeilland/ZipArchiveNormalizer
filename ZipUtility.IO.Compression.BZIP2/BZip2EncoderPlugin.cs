using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;
using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.BZip2
{
    public class BZip2EncoderPlugin
        : BZip2CoderPlugin, ICompressionHierarchicalEncoder
    {
        private class Encoder
            : HierarchicalEncoder
        {
            public Encoder(IOutputByteStream<UInt64> baseStream, UInt64? unpackedStreamSize, IProgress<UInt64>? progress)
                : base(GetBaseStream(baseStream), unpackedStreamSize, progress)
            {
            }

            private static IBasicOutputByteStream GetBaseStream(IOutputByteStream<UInt64> baseStream)
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return
                    new BZip2Stream(
                        baseStream.WithCache().AsStream(),
                         CompressionMode.Compress,
                         false)
                    .AsOutputByteStream(stream => stream.Finish())
                    .WithCache();
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

            return new Encoder(baseStream, unpackedStreamSize, progress);
        }
    }
}

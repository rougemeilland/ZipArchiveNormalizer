using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;
using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.BZip2
{
    public class BZip2DecoderPlugin
        : BZip2CoderPlugin, ICompressionHierarchicalDecoder
    {
        private class Decoder
            : HierarchicalDecoder
        {
            public Decoder(IInputByteStream<UInt64> baseStream, UInt64 unpackedStreamSize, IProgress<UInt64>? progress)
                : base(GetBaseStream(baseStream), unpackedStreamSize, progress)
            {
            }

            private static IBasicInputByteStream GetBaseStream(IInputByteStream<UInt64> baseStream)
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return
                    new BZip2Stream(
                        baseStream.WithCache().AsStream(),
                        CompressionMode.Decompress,
                        false)
                    .AsInputByteStream()
                    .WithCache();
            }
        }

        public IInputByteStream<UInt64> GetDecodingStream(IInputByteStream<UInt64> baseStream, ICoderOption option, UInt64 unpackedStreamSize, UInt64 packedStreamSize, IProgress<UInt64>? progress = null)
        {
            if (baseStream is null)
                throw new ArgumentNullException(nameof(baseStream));
            if (option is null)
                throw new ArgumentNullException(nameof(option));

            return new Decoder(baseStream, unpackedStreamSize, progress);
        }
    }
}

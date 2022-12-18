using SharpCompress.Compressors.PPMd;
using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Ppmd
{
    public class PpmdDecoderPlugin
        : PpmdCoderPlugin, ICompressionHierarchicalDecoder
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

                Span<Byte> properties = stackalloc Byte[2];
                if (baseStream.ReadBytes(properties) != properties.Length)
                    throw new DataErrorException("Too short properties");
                return
                    new PpmdStream(
                        new PpmdProperties(properties),
                        baseStream.WithCache().AsStream(),
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

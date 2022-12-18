using SevenZip;
using SevenZip.Compression.Deflate.Decoder;
using System;
using System.Threading;
using System.Threading.Tasks;
using Utility.IO;

namespace ZipUtility.IO.Compression.Deflate64
{
    public class Deflate64DecoderPlugin
        : Deflate64CoderPlugin, ICompressionHierarchicalDecoder
    {
        private class Decoder
            : HierarchicalDecoder
        {
            public Decoder(IInputByteStream<UInt64> baseStream, UInt64 unpackedStreamSize, IProgress<UInt64>? progress)
                : base(GetBaseStream(baseStream, unpackedStreamSize, progress), unpackedStreamSize)
            {
            }

            protected override Int32 ReadFromSourceStream(IBasicInputByteStream sourceStream, Span<byte> buffer)
            {
                try
                {
                    return base.ReadFromSourceStream(sourceStream, buffer);
                }
                catch (SevenZipDataErrorException ex)
                {
                    throw new DataErrorException("Detected data error", ex);
                }
            }

            protected override async Task<Int32> ReadFromSourceStreamAsync(IBasicInputByteStream sourceStream, Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                try
                {
                    return await base.ReadFromSourceStreamAsync(sourceStream, buffer, cancellationToken).ConfigureAwait(false);
                }
                catch (SevenZipDataErrorException ex)
                {
                    throw new DataErrorException("Detected data error", ex);
                }
            }

            private static IBasicInputByteStream GetBaseStream(IInputByteStream<UInt64> baseStream, UInt64 size, IProgress<UInt64>? progress)
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return
                    new Deflate64DecoderStream(
                        baseStream.WithCache(),
                        size,
                        progress,
                        false);
            }
        }

        public IInputByteStream<UInt64> GetDecodingStream(IInputByteStream<UInt64> baseStream, ICoderOption option, UInt64 unpackedStreamSize, UInt64 packedStreamSize, IProgress<UInt64>? progress = null)
        {
            if (baseStream is null)
                throw new ArgumentNullException(nameof(baseStream));
            if (option is null)
                throw new ArgumentNullException(nameof(option));
            if (option is not DeflateCompressionOption)
                throw new ArgumentException($"Illegal {nameof(option)} data", nameof(option));

            return new Decoder(baseStream, unpackedStreamSize, progress);
        }
    }
}

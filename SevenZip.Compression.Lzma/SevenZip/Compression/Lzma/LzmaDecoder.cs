using System;
using Utility.IO;

namespace SevenZip.Compression.Lzma
{
    public class LzmaDecoder
    {
        public const Int32 MaximumPropertyLength = Base.kPropSize;

        private readonly Decoder _decoder;

        public LzmaDecoder(Span<Byte> properties)
        {
            _decoder = new Decoder();
            _decoder.SetDecoderProperties(properties);
        }

        public void Decode(IBasicInputByteStream packedStream, IBasicOutputByteStream unpackedStream, UInt64? packedStreamSize, UInt64? unpackedStreamSize, IProgress<UInt64>? progress)
        {
            if (packedStream is null)
                throw new ArgumentNullException(nameof(packedStream));
            if (unpackedStream is null)
                throw new ArgumentNullException(nameof(unpackedStream));
            if (unpackedStreamSize.HasValue && unpackedStreamSize.Value > Int64.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(unpackedStreamSize));
            if (packedStreamSize.HasValue && packedStreamSize.Value > Int64.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(unpackedStreamSize));

            _decoder.Code(
                packedStream,
                unpackedStream,
                packedStreamSize.HasValue ? (Int64)packedStreamSize.Value : -1L,
                unpackedStreamSize.HasValue ? (Int64)unpackedStreamSize.Value : -1L,
                progress);
        }
    }
}

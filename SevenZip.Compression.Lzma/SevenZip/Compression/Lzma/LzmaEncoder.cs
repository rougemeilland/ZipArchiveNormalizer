using System;
using Utility.IO;

namespace SevenZip.Compression.Lzma
{
    public class LzmaEncoder
    {
        public const Int32 MaximumPropertyLength = Base.kPropSize;

        private readonly Encoder _encoder;
        private Func<IBasicOutputByteStream, IEncoderProperties, Int32> _headerWriter;

        public LzmaEncoder(LzmaEncoderProperties properties, Func<IBasicOutputByteStream, IEncoderProperties, Int32> headerWriter)
        {
            _encoder = new Encoder();
            _headerWriter = headerWriter;
            _encoder.SetCoderProperties(properties);
        }

        public Int32 GetEncoderProperties(Span<byte> buffer)
        {
            return _encoder.GetEncoderProperties(buffer);
        }

        public void Encode(IBasicInputByteStream unpackedStream, IBasicOutputByteStream packedStream, UInt64? unpackedStreamSize, UInt64? packedStreamSize, IProgress<UInt64>? progress)
        {
            var headerLength = _headerWriter(packedStream, _encoder);
            _encoder.Code(
                unpackedStream,
                packedStream,
                unpackedStreamSize.HasValue ? (Int64)unpackedStreamSize.Value : -1L,
                packedStreamSize.HasValue ? (Int64)packedStreamSize.Value - headerLength : -1L,
                progress);
        }
    }
}

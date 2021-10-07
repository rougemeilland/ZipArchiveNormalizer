using System;
using Utility;
using Utility.IO;
using Utility.IO.Compression;

namespace ZipUtility.IO.Compression.Lzma
{
    class LzmaEncodingStream
        : ZipContentOutputStream
    {
        private const byte _LZMA_COMPRESSION_MAJOR_VERSION = 21;
        private const byte _LZMA_COMPRESSION_MINOR_VERSION = 3;
        private const int _DEFAULT_COMPRESSION_LEVEL = 5;

        public LzmaEncodingStream(IOutputByteStream<UInt64> baseStream, bool useEndOfStreamMarker, ulong? size, ICodingProgressReportable progressReporter)
            : base(baseStream, size)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var header = new byte[4];
                header[0] = _LZMA_COMPRESSION_MAJOR_VERSION;
                header[1] = _LZMA_COMPRESSION_MINOR_VERSION;
                header[2] = (byte)(Utility.IO.Compression.Lzma.LzmaEncodingStream.PROPERTY_SIZE >> 0);
                header[3] = (byte)(Utility.IO.Compression.Lzma.LzmaEncodingStream.PROPERTY_SIZE >> 8);
                baseStream.WriteBytes(header.AsReadOnly(), 0, header.Length);
                var encoder =
                    new Utility.IO.Compression.Lzma.LzmaEncodingStream(
                        baseStream,
                        MakeCoderProperties(_DEFAULT_COMPRESSION_LEVEL, useEndOfStreamMarker, size),
                        progressReporter);
                encoder.WriteCoderProperties();
                SetDestinationStream(encoder);
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }

        protected override void FlushDestinationStream(IBasicOutputByteStream destinationStream, bool isEndOfData)
        {
            if (isEndOfData)
                destinationStream.Close();
        }

        private static CoderProperties MakeCoderProperties(int level, bool useEndOfStreamMarker, ulong? uncompressedStreamLength)
        {
            UInt32 dictionarySize;
            switch (level)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    dictionarySize = 1U << (level * 2 + 16);
                    break;
                case 4:
                case 5:
                case 6:
                    dictionarySize = 1U << (level + 19);
                    break;
                case 7:
                    dictionarySize = 1U << 25;
                    break;
                case 8:
                case 9:
                    dictionarySize = 1U << 26;
                    break;
                default:
                    throw new ArgumentException();
            }
            if (uncompressedStreamLength.HasValue &&
                uncompressedStreamLength.Value <= UInt32.MaxValue &&
                dictionarySize > uncompressedStreamLength.Value)
            {
                dictionarySize = ((UInt32)uncompressedStreamLength.Value).Maximum(1U << 12).Minimum(dictionarySize);
            }
            return new CoderProperties
            {
                DictionarySize = dictionarySize,
                PosStateBits = 2,
                LitContextBits = 3,
                LitPosBits = 0,
                Algorithm = 1 /*level < 5 ? 0U : 1U*/,
                NumFastBytes = level < 7 ? 32U : 64U,
                MatchFinder = "bt4" /*level < 5 ? "hc5" : "bt4"*/,
                EndMarker = useEndOfStreamMarker,
            };
        }
    }
}
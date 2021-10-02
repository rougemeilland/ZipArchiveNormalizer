using SevenZip;
using SevenZip.Compression.Lzma;
using System;
using System.Threading.Tasks;
using Utility;
using Utility.IO;

namespace ZipUtility.IO.Compression.Lzma
{
    public class LzmaOutputStream
        : ZipContentOutputStream
    {
        private const uint _MAXIMUM_DICTIONAEY_SIZE = 1 << 30; // == 1GB
        private const uint _DEFAULT_DICTIONAEY_SIZE = 1 << 24; // == 16MB
        private const uint _MINIMUM_DICTIONAEY_SIZE = 1 << 12; // == 4KB
        private const byte _LZMA_COMPRESSION_MAJOR_VERSION = 21;
        private const byte _LZMA_COMPRESSION_MINOR_VERSION = 3;
        private const ushort _LZMA_PROPERTY_SIZE = 5;
        private bool _isDisposed;
        private IOutputByteStream<UInt64> _baseStream;
        private Task _backgroundEncoder;

        public LzmaOutputStream(IOutputByteStream<UInt64> baseByteStream, bool useEndOfStreamMarker, ulong? size)
            : this(baseByteStream, null, useEndOfStreamMarker, size)
        {
        }

        public LzmaOutputStream(IOutputByteStream<UInt64> baseByteStream, int level, bool useEndOfStreamMarker, ulong? size)
            : this(baseByteStream, (int?)level, useEndOfStreamMarker, size)
        {
        }

        private LzmaOutputStream(IOutputByteStream<UInt64> baseStream, int? level, bool useEndOfStreamMarker, ulong? size)
            : base(baseStream, size)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                _isDisposed = false;
                _baseStream = baseStream.WithCache();
                var fifo = new FifoBuffer();
                SetDestinationStream(fifo.GetOutputByteStream(false));
                _backgroundEncoder = Task.Run(() =>
                {
                    var inputByteStream = fifo.GetInputByteStream();
                    try
                    {
                        try
                        {
                            var header = new byte[4];
                            header[0] = _LZMA_COMPRESSION_MAJOR_VERSION;
                            header[1] = _LZMA_COMPRESSION_MINOR_VERSION;
                            header[2] = (byte)(_LZMA_PROPERTY_SIZE >> 0);
                            header[3] = (byte)(_LZMA_PROPERTY_SIZE >> 8);
                            _baseStream.WriteBytes(header.AsReadOnly(), 0, header.Length);
                            using (var inputStream = inputByteStream.AsStream(true))
                            using (var cSharpStream = _baseStream.AsStream(true))
                            {
                                var encoder = CreateEncoder(level, useEndOfStreamMarker, size);
                                encoder.WriteCoderProperties(cSharpStream);
                                encoder.Code(inputStream, cSharpStream, -1, -1, null);
                            }
                        }
                        finally
                        {
                            lock (this)
                            {
                                if (_baseStream != null)
                                {
                                    _baseStream.Dispose();
                                    _baseStream = null;
                                }
                            }
                            if (inputByteStream != null)
                            {
                                inputByteStream.Dispose();
                                inputByteStream = null;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // foreground task からの一方的な Dispose などに備え、例外はすべて無視する
                    }
                });
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }

        protected override void FlushDestinationStream(IOutputByteStream<UInt64> destinationStream, bool isEndOfData)
        {
            base.FlushDestinationStream(destinationStream, isEndOfData);

            if (isEndOfData)
            {
                // ストリームを close して、 background task の終了を待ち合わせる
                if (destinationStream != null)
                    destinationStream.Dispose();

                // background task が終了するのを待ち合わせる。
                _backgroundEncoder.Wait();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    lock (this)
                    {
                        if (_baseStream != null)
                        {
                            _baseStream.Dispose();
                            _baseStream = null;
                        }
                    }
                }
                _isDisposed = true;
                base.Dispose(disposing);
            }
        }

        private static LzmaEncoder CreateEncoder(int? level, bool useEndOfStreamMarker, ulong? uncompressedStreamLength)
        {
            var encoder = new LzmaEncoder();
            encoder.SetCoderProperties(MakeCoderProperties(level ?? 5, useEndOfStreamMarker, uncompressedStreamLength));
            return encoder;
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

        private static UInt32 DecideDictionarySize(UInt32? dictionarySize, long? uncompressedStreamLength)
        {
            if (dictionarySize.HasValue)
            {
                if (dictionarySize.Value > _MAXIMUM_DICTIONAEY_SIZE)
                    throw new ArgumentException("Too large dictionary size");
                if (dictionarySize.Value < _MINIMUM_DICTIONAEY_SIZE)
                    return _MINIMUM_DICTIONAEY_SIZE;
                return dictionarySize.Value;
            }
            else if (uncompressedStreamLength.HasValue)
            {
                var shiftCount = 16;
                while (shiftCount < 24 && (1U << shiftCount) < uncompressedStreamLength.Value)
                {
                    ++shiftCount;
                }
                return 1U << shiftCount;
            }
            else
                return _DEFAULT_DICTIONAEY_SIZE;
        }
    }
}
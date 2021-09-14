using SevenZip;
using SevenZip.Compression.Lzma;
using System;
using System.IO;
using System.Threading.Tasks;
using Utility.IO;

namespace ZipUtility.Compression.Lzma.DataStream
{
    public class LzmaOutputStream
        : OutputStream
    {
        private const uint _MAXIMUM_DICTIONAEY_SIZE = 1 << 30; // == 1GB
        private const uint _DEFAULT_DICTIONAEY_SIZE = 1 << 24; // == 16MB
        private const uint _MINIMUM_DICTIONAEY_SIZE = 1 << 12; // == 4KB
        private const byte _LZMA_COMPRESSION_MAJOR_VERSION = 21;
        private const byte _LZMA_COMPRESSION_MINOR_VERSION = 3;
        private const ushort _LZMA_PROPERT_SIZE = 5;
        private bool _isDisposed;
        private Stream _baseStream;
        private Task _backgroundEncoder;

        public LzmaOutputStream(Stream baseStream, bool useEndOfStreamMarker, long? offset, long? size, bool leaveOpen)
            : this(baseStream, null, useEndOfStreamMarker, offset, size, leaveOpen)
        {
        }

        public LzmaOutputStream(Stream baseStream, uint dictionarySize, bool useEndOfStreamMarker, long? offset, long? size, bool leaveOpen)
            : this(baseStream, (uint?)dictionarySize, useEndOfStreamMarker, offset, size, leaveOpen)
        {
        }

        private LzmaOutputStream(Stream baseStream, uint? dictionarySize, bool useEndOfStreamMarker, long? offset, long? size, bool leaveOpen)
            : base(baseStream, offset, size, false)
        {
            _isDisposed = false;
            _baseStream = new BufferedOutputStream(new PartialOutputStream(baseStream, offset, null, leaveOpen));
            var fifo = new FifoBuffer();
            SetDestinationStream(fifo.GetOutputStream(false));
            _backgroundEncoder = Task.Run(() =>
            {
                var inputStream = fifo.GetInputStream();
                try
                {
                    try
                    {
                        var header = new byte[4];
                        header[0] = _LZMA_COMPRESSION_MAJOR_VERSION;
                        header[1] = _LZMA_COMPRESSION_MINOR_VERSION;
                        header[2] = (byte)(_LZMA_PROPERT_SIZE >> 0);
                        header[3] = (byte)(_LZMA_PROPERT_SIZE >> 8);
                        _baseStream.Write(header, 0, header.Length);
                        var encoder = CreateEncoder(dictionarySize, useEndOfStreamMarker, size);
                        encoder.WriteCoderProperties(_baseStream);
                        encoder.Code(inputStream, _baseStream, -1, -1, null);
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
                        if (inputStream != null)
                        {
                            inputStream.Dispose();
                            inputStream = null;
                        }
                    }
                }
                catch (Exception)
                {
                    // foreground task からの一方的な Dispose などに備え、例外はすべて無視する
                }
            });
        }

        protected override void FlushDestinationStream(Stream destinationStream, bool isEndOfData)
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

        private static LzmaEncoder CreateEncoder(uint? dictionarySize, bool useEndOfStreamMarker, long? uncompressedStreamLength)
        {
            var encoder = new LzmaEncoder();
            encoder.SetCoderProperties(new CoderProperties
            {
                DictionarySize = DecideDictionarySize(dictionarySize, uncompressedStreamLength),
                PosStateBits = 2,
                LitContextBits = 3,
                LitPosBits = 0,
                Algorithm = 1,
                NumFastBytes = 128,
                MatchFinder = "bt4",
                EndMarker = useEndOfStreamMarker,
            });
            return encoder;
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
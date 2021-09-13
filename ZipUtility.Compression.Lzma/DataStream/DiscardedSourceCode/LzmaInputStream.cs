#if false
using SevenZip.Compression.Lzma;
using System;
using System.IO;
using Utility;
using Utility.IO;

namespace ZipUtility.Compression.Lzma.DataStream
{
    public class LzmaInputStream
        : Stream
    {
        private const int _MINIMUM_BUFFER_CAPACITY = 1 << 12; // == 4KB
        private bool _isDisposed;
        private MemoryStream _bufferStream;
        private bool _useEndOfStreamMarker;
        private LzmaDecoder _decoder;
        private Stream _baseStream;
        private long _size;
        private long _totalCount;
        private IReadOnlyArray<byte> _commonProperties;
        private bool _isCorruptedStream;
        private bool _firstChunkRead;
        private UInt16 _propertiesLength;
        private bool _isEntOfStream;

        public LzmaInputStream(Stream baseStream, bool useEndOfStreamMarker, long? offset, long packedSize, long size, bool leaveOpen = false)
        {
            if (offset.HasValue && offset.Value < 0)
                throw new ArgumentException();
            if (packedSize < 0)
                throw new ArgumentException();
            if (size < 0)
                throw new ArgumentException();

            _isDisposed = false;
            _baseStream = new PartialInputStream(baseStream, offset, packedSize, leaveOpen);
            _useEndOfStreamMarker = useEndOfStreamMarker;
            _size = size;
            _totalCount = 0;
            _commonProperties = new byte[0].AsReadOnly();
            _bufferStream = new MemoryStream();
            _isCorruptedStream = false;
            _decoder = new LzmaDecoder();
            _propertiesLength = 0;
            _isEntOfStream = false;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (_isCorruptedStream)
                throw new IOException("The stream is corrupted.");
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException();

            if (!_firstChunkRead)
            {
                ReadChunk(true);
                _firstChunkRead = true;
            }
            var readCount = 0;
            while (count > _bufferStream.Length - _bufferStream.Position && _isCorruptedStream == false && _isEntOfStream == false)
            {
                var temporaryBuffer = new byte[_bufferStream.Length - _bufferStream.Position];
                _bufferStream.ReadBytes(temporaryBuffer, 0, temporaryBuffer.Length);
                temporaryBuffer.CopyTo(buffer, offset);
                offset += temporaryBuffer.Length;
                count -= temporaryBuffer.Length;
                readCount += temporaryBuffer.Length;
                ReadChunk(false);
            }
            if (_isCorruptedStream == false && _isEntOfStream == false)
            {
                _bufferStream.Read(buffer, offset, count);
                readCount += count;
            }
            _totalCount += readCount;
            return readCount;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (_isCorruptedStream)
                throw new IOException("The stream is corrupted.");
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_baseStream != null)
                    {
                        _baseStream.Dispose();
                        _baseStream = null;
                    }
                    if (_bufferStream != null)
                    {
                        _bufferStream.Dispose();
                        _bufferStream = null;
                    }
                }
                base.Dispose(disposing);
            }
        }

        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        private void ReadChunk(bool isFirst)
        {
            if (isFirst)
            {
                var header = _baseStream.ReadBytes(4);
                var majorVersion = header[0];
                var minorVersion = header[1];
                _propertiesLength = header.ToUInt16LE(2);
            }

            var decodedChunkSize = (int?)0;
            var properties = new byte[0].AsReadOnly();
            try
            {
                var decodedChunkSize64 = (UInt64?)null;
                properties = GetLzmaProperties(out decodedChunkSize64);
                decodedChunkSize = decodedChunkSize64.HasValue ? (int)decodedChunkSize64.Value : (int?)null;
            }
            catch (InternalLzmaException)
            {
                _isCorruptedStream = true;
            }

            if (_isCorruptedStream == false && _isEntOfStream == false)
            {
                if (_firstChunkRead == false)
                    _commonProperties = properties;
                else if (_commonProperties.SequenceEqual(properties) == false)
                    _isCorruptedStream = true;
                else
                {
                    // NOP
                }
            }
            if (_isCorruptedStream == false && _isEntOfStream == false)
            {
                if (decodedChunkSize.HasValue)
                {
                    if (_bufferStream.Capacity < decodedChunkSize.Value)
                        _bufferStream.Capacity = decodedChunkSize.Value;
                    _bufferStream.SetLength(decodedChunkSize.Value);
                }
                else
                {
                    _bufferStream.Capacity = 0;
                    _bufferStream.SetLength(0);
                }
                _decoder.SetDecoderProperties(properties.ToArray());
                _bufferStream.Position = 0;
                _decoder.Code(_baseStream, _bufferStream, 0, decodedChunkSize ?? -1, null);
                _bufferStream.Position = 0;
            }
        }

        private IReadOnlyArray<byte> GetLzmaProperties(out UInt64? decodedChunkSize64)
        {
            var properties = _baseStream.ReadBytes(_propertiesLength);
            if (properties.Length == 0)
            {
                decodedChunkSize64 = UInt64.MaxValue;
                return properties;
            }
            if (properties.Length != _propertiesLength)
                throw new InternalLzmaException("The stream is too short to read LZMA properties.");

            if (_useEndOfStreamMarker)
            {
                decodedChunkSize64 = null;
            }
            else
            {
                var decodedChunkSizeBytes = _baseStream.ReadBytes(8);
                if (decodedChunkSizeBytes.Length != 8)
                    throw new InternalLzmaException("The stream is too short to read LZMA properties.");
                decodedChunkSize64 = decodedChunkSizeBytes.ToUInt64LE(0);
                if (decodedChunkSize64.Value.IsBetween(0UL, (UInt64)int.MaxValue) == false)
                    throw new InternalLzmaException("Chunk size is negative or too large.");
            }
            return properties;
        }
    }
}
#endif
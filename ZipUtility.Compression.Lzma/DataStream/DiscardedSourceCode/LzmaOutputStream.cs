#if false
using SevenZip;
using SevenZip.Compression.Lzma;
using System;
using System.IO;
using Utility;
using Utility.IO;

namespace ZipUtility.Compression.Lzma.DataStream
{
    public class LzmaOutputStream
        : Stream
    {
        private const int _MAXIMUM_BUFFER_CAPACITY = 1 << 30; // == 1GB
        private const int _DEFAULT_BUFFER_CAPACITY = 1 << 18; // == 256KB
        private const int _MINIMUM_BUFFER_CAPACITY = 1 << 12; // == 4KB
        private bool _isDisposed;
        private Stream _baseStream;
        private bool _useEndOfStreamMarker;
        private int _bufferCapacity;
        private long? _size;
        private long _totalCount;
        private MemoryStream _bufferStream;
        private LzmaEncoder _encoder;
        private uint _dictionarySize;

        public LzmaOutputStream(Stream baseStream, bool useEndOfStreamMarker, long? offset, long? size, bool leaveOpen)
            : this(baseStream, useEndOfStreamMarker, _DEFAULT_BUFFER_CAPACITY, offset, size, leaveOpen)
        {
        }

        public LzmaOutputStream(Stream baseStream, bool useEndOfStreamMarker, int bufferCapacity, long? offset, long? size, bool leaveOpen)
        {
            if (offset.HasValue && offset.Value < 0)
                throw new ArgumentException();
            if (size.HasValue && size.Value < 0)
                throw new ArgumentException();
            if (bufferCapacity.IsBetween(0, _MAXIMUM_BUFFER_CAPACITY) == false)

            _isDisposed = false;
            _baseStream = new PartialOutputStream(baseStream, offset, null, leaveOpen);
            _useEndOfStreamMarker = useEndOfStreamMarker;
            _bufferCapacity = bufferCapacity;
            if (_bufferCapacity < _MINIMUM_BUFFER_CAPACITY)
                _bufferCapacity = _MINIMUM_BUFFER_CAPACITY;
            _size = size;
            _totalCount = 0;
            _bufferStream = new MemoryStream();
            _encoder = new LzmaEncoder();
            _dictionarySize = _bufferCapacity;
            _bufferStream.Capacity = _bufferCapacity;
            SetLzmaProperties(_encoder);
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > buffer.Length)
                throw new ArgumentException();
            if (_size.HasValue && _totalCount + count > _size.Value)
                throw new InvalidOperationException("Can not write any more.");

            var writtenCount = 0;
            while (count >= _bufferCapacity - _bufferStream.Position)
            {
                int length = _bufferCapacity - (int)_bufferStream.Position;
                _bufferStream.Write(buffer, offset, length);
                offset += length;
                writtenCount += length;
                count -= length;
                WriteChunk();
            }
            _bufferStream.Write(buffer, offset, count);
            _totalCount += writtenCount;
        }

        public override void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            InternalFlush();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    InternalFlush();
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
                _isDisposed = true;
            }
        }

        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private void SetLzmaProperties(LzmaEncoder encoder)
        {
            encoder.SetCoderProperties(new CoderProperties
            {
                DictionarySize = _DEFAULT_BUFFER_CAPACITY,
                PosStateBits = 2,
                LitContextBits = 3,
                LitPosBits = 0,
                Algorithm = 1,
                NumFastBytes = 128,
                MatchFinder = "bt4",
                EndMarker = _useEndOfStreamMarker,
            });
        }

        private void InternalFlush()
        {
            WriteChunk();
        }

        private void WriteChunk()
        {
            _encoder.WriteCoderProperties(_baseStream);
            var streamSize = _bufferStream.Position;
            if (_bufferStream.Length != _bufferStream.Position)
                _bufferStream.SetLength(_bufferStream.Position);
            _bufferStream.Position = 0;
            if (_useEndOfStreamMarker == false)
            {
                var streamSizeBytes = streamSize.ToByteArrayLE();
                _baseStream.Write(streamSizeBytes, 0, streamSizeBytes.Length);
            }
            _encoder.Code(_bufferStream, _baseStream, -1, -1, null);
            if (_bufferStream.Position > int.MaxValue)
                throw new IOException("Too long chunk.");
            _bufferStream.Position = 0;
        }
    }
}
#endif

using System;
using System.IO;
using Utility.IO;

namespace ZipUtility.Compression.BZIP2.DataStream
{
    public class Bzip2InputStream
        : Stream
    {
        private bool _isDisposed;
        private Stream _baseStream;
        private long _size;
        private long _totalCount;

        public Bzip2InputStream(Stream baseStream, long? offset, long packedSize, long size, bool leaveOpen = false)
        {
            if (offset.HasValue && offset.Value < 0)
                throw new ArgumentException();
            if (packedSize < 0)
                throw new ArgumentException();
            if (size < 0)
                throw new ArgumentException();

            _isDisposed = false;
            _baseStream = new Bzip2.BZip2InputStream(new PartialInputStream(baseStream, offset, packedSize, leaveOpen), false);
            _size = size;
            _totalCount = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException();

            var length = _baseStream.Read(buffer, offset, count);
            if (length > 0)
                _totalCount += length;
            else if (_totalCount != _size)
                throw new IOException("Size not match");
            else
            {
                // NOP
            }
            return length;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
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
                }
                _isDisposed = true;
                base.Dispose(disposing);
            }
        }

        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
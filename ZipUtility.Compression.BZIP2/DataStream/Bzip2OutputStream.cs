using System;
using System.IO;
using Utility.IO;

namespace ZipUtility.Compression.BZIP2.DataStream
{
    public class Bzip2OutputStream
        : Stream
    {
        private bool _isDisposed;
        private Stream _baseStream;
        private long? _size;
        private long _totalCount;

        public Bzip2OutputStream(Stream baseStream, int level, long? offset, long? size, bool leaveOpen)
        {
            if (offset.HasValue && offset.Value < 0)
                throw new ArgumentException();
            if (size.HasValue && size.Value < 0)
                throw new ArgumentException();

            _isDisposed = false;
            _baseStream = new Bzip2.BZip2OutputStream(new BufferedOutputStream(new PartialOutputStream(baseStream, offset, null, leaveOpen)), true, level);
            _size = size;
            _totalCount = 0;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

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

            _baseStream.Write(buffer, offset, count);
            _totalCount += count;
        }

        public override void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            // Bzip2.BZip2OutputStream.Flush は例外を返すため、呼び出さない。
            //_baseStream.Flush();
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
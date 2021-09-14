using System;
using System.IO;

namespace Utility.IO
{
    public class PartialOutputStream
        : Stream
    {
        private bool _isDisposed;
        private Stream _baseStream;
        private long? _offset;
        private long? _size;
        private bool _leaveOpen;
        private long _totalCount;

        public PartialOutputStream(Stream baseStream, long? offset, long? size, bool leaveOpen = false)
        {
            if (offset.HasValue && baseStream.CanSeek == false)
                throw new NotSupportedException();
            if (offset.HasValue && offset.Value < 0)
                throw new ArgumentException();
            if (size.HasValue && size.Value < 0)
                throw new ArgumentException();

            _isDisposed = false;
            _baseStream = baseStream;
            _offset = offset;
            _size = size;
            _leaveOpen = leaveOpen;
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

            if (_offset.HasValue)
                _baseStream.Seek(_offset.Value + _totalCount, SeekOrigin.Begin);
            _baseStream.Write(buffer, offset, count);
            _totalCount += count;
        }

        public override void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            _baseStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_baseStream != null)
                    {
                        if (_leaveOpen == false)
                            _baseStream.Dispose();
                        _baseStream = null;
                    }
                }
                _isDisposed = true;
                base.Dispose(disposing);
            }
        }

        // CanSeek == false であるにもかかわらず Position を参照するアプリケーションが存在するため、 get だけは実装する。(LzmaとかLZmaとかLzmaとか。)
        public override long Position { get => _totalCount; set => throw new NotSupportedException(); }

        public override long Length => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => new NotSupportedException();
    }
}
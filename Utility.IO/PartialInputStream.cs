using System;
using System.IO;

namespace Utility.IO
{
    public class PartialInputStream
        : Stream
    {
        private bool _isDisposed;
        private Stream _baseStream;
        private long? _offset;
        private long _size;
        private bool _leaveOpen;
        private long _totalCount;

        public PartialInputStream(Stream baseStream, long? offset, long size, bool leaveOpen = false)
        {
            if (offset.HasValue && baseStream.CanSeek == false)
                throw new NotSupportedException();
            if (offset.HasValue && offset.Value < 0)
                throw new ArgumentException();
            if (size < 0)
                throw new ArgumentException();
            if (offset.HasValue && offset.Value + size > baseStream.Length)
                throw new ArgumentException();

            _isDisposed = false;
            _baseStream = baseStream;
            _offset = offset;
            _size = size;
            _leaveOpen = leaveOpen;
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

            var actualCount = _size - _totalCount;
            if (actualCount > count)
                actualCount = count;
            if (actualCount <= 0)
                return 0;
            if (_offset.HasValue)
                _baseStream.Seek(_offset.Value + _totalCount, SeekOrigin.Begin);
            var length = _baseStream.Read(buffer, offset, (int)actualCount);
            _totalCount += length;
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
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}

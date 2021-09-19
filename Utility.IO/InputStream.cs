using System;
using System.IO;

namespace Utility.IO
{
    public abstract class InputStream
        : Stream
    {
        private bool _isDisposed;
        private long _size;
        private bool _leaveOpen;
        private long _totalCount;
        private Stream _sourceStream;
        private bool _isEndOfStream;

        public InputStream(Stream baseStream, long? offset, long packedSize, long size, bool leaveOpen)
        {
            if (baseStream == null)
                throw new ArgumentNullException("baseStream");
            if (baseStream.CanRead == false)
                throw new ArgumentException("'baseStream' is not suppot 'Read'.", "baseStream");
            if (offset.HasValue && offset.Value < 0)
                throw new ArgumentException("'offset' must not be negative.", "offset");
            if (size < 0)
                throw new ArgumentException("'size' must not be negative.", "size");
            if (packedSize < 0)
                throw new ArgumentException("'packedSize' must not be negative.", "packedSize");

            _isDisposed = false;
            _leaveOpen = leaveOpen;
            _size = size;
            _sourceStream = null;
            _totalCount = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_sourceStream == null)
                throw new Exception("'InputStream.SetSourceStream' has not been called yet.");
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentException("'offset' must not be negative.", "offset");
            if (count < 0)
                throw new ArgumentException("'count' must not be negative.", "count");
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException("'offset + count' is greater than 'buffer.Length'.");
            if (_isEndOfStream)
                return 0;

            var length = ReadFromSourceStream(_sourceStream, buffer, offset, count);
            if (length > 0)
                _totalCount += length;
            else
            {
                _isEndOfStream = true;
                if (_totalCount != _size)
                    throw new IOException("Size not match");
                else
                    OnEndOfStream();
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
                    if (_sourceStream != null)
                    {
                        if (_leaveOpen == false)
                            _sourceStream.Dispose();
                        _sourceStream = null;
                    }
                }
                _isDisposed = true;
                base.Dispose(disposing);
            }
        }

        // 本来は CanSeek == false なら Position がされた場合は NotSupportedException 例外を発行すべきだが、
        // たまに CanSeek の値を確認せずに Position を参照するユーザが存在するので、 get だけは実装する。
        public override long Position { get => _totalCount; set => throw new NotSupportedException(); }
        public override long Length => _size;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected void SetSourceStream(Stream sourceStream)
        {
            if (sourceStream == null)
                throw new ArgumentNullException("sourceStream");
            if (sourceStream.CanRead == false)
                throw new ArgumentException("'sourceStream' is not suppot 'Read'.", "sourceStream");
            _sourceStream = sourceStream;
        }

        protected virtual int ReadFromSourceStream(Stream sourceStream, byte[] buffer, int offset, int count)
        {
            return sourceStream.Read(buffer, offset, count);
        }

        protected virtual void OnEndOfStream()
        {
        }
    }
}

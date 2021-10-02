using System;
using System.IO;
using Utility.IO;

namespace ZipUtility.IO
{
    public abstract class ZipContentInputStream
        : IInputByteStream<UInt64>
    {
        private bool _isDisposed;
        private ulong _size;
        private ulong _position;
        private IInputByteStream<UInt64> _sourceStream;
        private bool _isEndOfStream;

        public ZipContentInputStream(IInputByteStream<UInt64> baseStream, ulong size)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));

            _isDisposed = false;
            _size = size;
            _sourceStream = null;
            _position = 0;
        }

        public ulong Position => !_isDisposed ? _position : throw new ObjectDisposedException(GetType().FullName);

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_sourceStream == null)
                throw new Exception("'InputStream.SetSourceStream' has not been called yet.");
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentException("'offset' must not be negative.", nameof(offset));
            if (count < 0)
                throw new ArgumentException("'count' must not be negative.", nameof(count));
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException("'offset + count' is greater than 'buffer.Length'.");
            if (_isEndOfStream)
                return 0;

            var length = ReadFromSourceStream(_sourceStream, buffer, offset, count);
            if (length > 0)
                _position += (ulong)length;
            else
            {
                _isEndOfStream = true;
                if (_position != _size)
                    throw new IOException("Size not match");
                else
                    OnEndOfStream();
            }
            return length;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected void SetSourceStream(IInputByteStream<UInt64> sourceStream)
        {
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));
            _sourceStream = sourceStream;
        }

        protected virtual int ReadFromSourceStream(IInputByteStream<UInt64> sourceStream, byte[] buffer, int offset, int count)
        {
            return sourceStream.Read(buffer, offset, count);
        }

        protected virtual void OnEndOfStream()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_sourceStream != null)
                    {
                        _sourceStream.Dispose();
                        _sourceStream = null;
                    }
                }
                _isDisposed = true;
            }
        }
    }
}

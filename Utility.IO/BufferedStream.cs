using System;

namespace Utility.IO
{
    public abstract class BufferedStream
        : System.IO.Stream
    {
        private const int _MAXIMUM_BUFFER_SIZE = 1024 * 1024;
        private const int _DEFAULT_BUFFER_SIZE = 80 * 1024;
        private const int _MINIMUM_BUFFER_SIZE = 4 * 1024;

        private bool _isDisposed;
        private System.IO.Stream _baseStream;
        private bool _leaveOpen;
        private long _totalCount;
        private bool _isEndOfStream;

        protected BufferedStream(System.IO.Stream baseStream, bool leaveOpen = false)
            : this(baseStream, _DEFAULT_BUFFER_SIZE, leaveOpen)
        {
        }

        protected BufferedStream(System.IO.Stream baseStream, int bufferSize, bool leaveOpen = false)
        {
            if (bufferSize < 0)
                throw new ArgumentException();
            if (bufferSize > _MAXIMUM_BUFFER_SIZE)
                bufferSize = _MAXIMUM_BUFFER_SIZE;
            else if (bufferSize < _MINIMUM_BUFFER_SIZE)
                bufferSize = _MINIMUM_BUFFER_SIZE;
            else
            {
                // NOP
            }
            _isDisposed = false;
            _baseStream = baseStream;
            BufferSize = bufferSize;
            _leaveOpen = leaveOpen;
            _totalCount = 0;
            _isEndOfStream = false;
        }


        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (CanRead == false)
                throw new NotSupportedException();
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException();
            if (_isEndOfStream)
                return 0;

            var length = InternalRead(buffer, offset, count);
            if (length <= 0)
                _isEndOfStream = true;
            else
                _totalCount += length;
            return length;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (CanWrite == false)
                throw new NotSupportedException();
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > buffer.Length)
                throw new ArgumentException();

            InternalWrite(buffer, offset, count);
            _totalCount += count;
        }

        public override void Flush()
        {
            if (CanWrite)
                InternalFlush();
        }

        // CanSeek == false であるにもかかわらず Position を参照するアプリケーションが存在するため、 get だけは実装する。(LzmaとかLZmaとかLzmaとか。)
        public override long Position { get => _totalCount; set => throw new NotSupportedException(); }

        public override long Length => throw new NotSupportedException();
        public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected int BufferSize { get; }
        protected virtual int InternalRead(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected virtual void InternalWrite(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected virtual void InternalFlush()
        {
        }

        protected int ReadFromSource(byte[] buffer, int offset, int count)
        {
            return _baseStream.Read(buffer, offset, count);
        }

        protected void WriteToDestination(byte[] buffer, int offset, int count)
        {
            _baseStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_baseStream != null)
                    {
                        try
                        {
                            InternalFlush();
                        }
                        catch (Exception)
                        {
                        }
                        if (_leaveOpen == false)
                            _baseStream.Dispose();
                        _baseStream = null;
                    }
                }
                base.Dispose(disposing);
            }
        }
    }
}

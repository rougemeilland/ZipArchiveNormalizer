using System;

namespace Utility.IO
{
    abstract class BufferedRandomInputStream<POSITION_T>
        : IRandomInputByteStream<POSITION_T>
    {
        private const int _MAXIMUM_BUFFER_SIZE = 1024 * 1024;
        private const int _DEFAULT_BUFFER_SIZE = 80 * 1024;
        private const int _MINIMUM_BUFFER_SIZE = 4 * 1024;

        private bool _isDisposed;
        private IRandomInputByteStream<POSITION_T> _baseStream;
        private bool _leaveOpen;
        private int _bufferSize;
        private byte[] _internalBuffer;
        private int _internalBufferCount;
        private int _internalBufferIndex;

        public BufferedRandomInputStream(IRandomInputByteStream<POSITION_T> baseStream, bool leaveOpen)
            : this(baseStream, _DEFAULT_BUFFER_SIZE, leaveOpen)
        {
        }

        public BufferedRandomInputStream(IRandomInputByteStream<POSITION_T> baseStream, int bufferSize, bool leaveOpen)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (bufferSize < 0)
                    throw new ArgumentException();

                _isDisposed = false;
                _baseStream = baseStream;
                _bufferSize = bufferSize.Minimum(_MAXIMUM_BUFFER_SIZE).Maximum(_MINIMUM_BUFFER_SIZE);
                _leaveOpen = leaveOpen;
                _internalBuffer = new byte[_bufferSize];
                _internalBufferCount = 0;
                _internalBufferIndex = 0;
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public UInt64 Length
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return _baseStream.Length;
            }

            set => throw new NotSupportedException();
        }

        public POSITION_T Position
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return AddPosition(_baseStream.Position, (uint)_internalBufferIndex);
            }
        }

        public void Seek(POSITION_T offset)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _baseStream.Seek(offset);
            _internalBufferCount = 0;
            _internalBufferIndex = 0;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException();

            return InternalRead(buffer, offset, count);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected abstract POSITION_T AddPosition(POSITION_T x, UInt64 y);

        protected void Dispose(bool disposing)
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
            }
        }

        private int InternalRead(byte[] buffer, int offset, int count)
        {
            if (_internalBufferIndex >= _internalBufferCount)
            {
                _internalBufferCount = _baseStream.Read(_internalBuffer, 0, _internalBuffer.Length);
                if (_internalBufferCount <= 0)
                    return _internalBufferCount;
                _internalBufferIndex = 0;
            }
            var actualCount = _internalBufferCount - _internalBufferIndex;
            if (actualCount > count)
                actualCount = count;
            Array.Copy(_internalBuffer, _internalBufferIndex, buffer, offset, actualCount);
            _internalBufferIndex += actualCount;
            return actualCount;
        }
    }
}
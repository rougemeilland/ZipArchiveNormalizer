using System;

namespace Utility.IO
{
    abstract class BufferedInputStream<POSITION_T>
        : IInputByteStream<POSITION_T>
    {
        private const int _MAXIMUM_BUFFER_SIZE = 1024 * 1024;
        private const int _DEFAULT_BUFFER_SIZE = 80 * 1024;
        private const int _MINIMUM_BUFFER_SIZE = 4 * 1024;

        private bool _isDisposed;
        private IInputByteStream<POSITION_T> _baseStream;
        private bool _leaveOpen;
        private UInt64 _position;
        private int _bufferSize;
        private byte[] _internalBuffer;
        private int _internalBufferCount;
        private int _internalBufferIndex;
        private bool _isEndOfStream;
        private bool _isEndOfBaseStream;

        public BufferedInputStream(IInputByteStream<POSITION_T> baseStream, bool leaveOpen)
            : this(baseStream, _DEFAULT_BUFFER_SIZE, leaveOpen)
        {
        }

        public BufferedInputStream(IInputByteStream<POSITION_T> baseStream, int bufferSize, bool leaveOpen)
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
                _position = 0;
                _isEndOfStream = false;
                _internalBuffer = new byte[_bufferSize];
                _internalBufferCount = 0;
                _internalBufferIndex = 0;
                _isEndOfBaseStream = false;
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public POSITION_T Position => !_isDisposed ? AddPosition(ZeroPositionValue, _position) : throw new ObjectDisposedException(GetType().FullName);

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
            if (_isEndOfStream)
                return 0;

            var length = InternalRead(buffer, offset, count);
            if (length <= 0)
                _isEndOfStream = true;
            else
            {
#if DEBUG
                checked
#endif
                {
                    _position += (uint)length;
                }
            }
            return length;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected abstract POSITION_T ZeroPositionValue { get; }
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
            if (_isEndOfBaseStream)
                return 0;

            if (_internalBufferIndex >= _internalBufferCount)
            {
                _internalBufferCount = _baseStream.Read(_internalBuffer, 0, _internalBuffer.Length);
                if (_internalBufferCount <= 0)
                {
                    _isEndOfBaseStream = true;
                    return _internalBufferCount;
                }
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
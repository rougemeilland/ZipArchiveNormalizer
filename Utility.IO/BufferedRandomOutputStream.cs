using System;

namespace Utility.IO
{
    abstract class BufferedRandomOutputStream<POSITION_T>
        : IRandomOutputByteStream<POSITION_T>
    {
        private const int _MAXIMUM_BUFFER_SIZE = 1024 * 1024;
        private const int _DEFAULT_BUFFER_SIZE = 80 * 1024;
        private const int _MINIMUM_BUFFER_SIZE = 4 * 1024;

        private bool _isDisposed;
        private IRandomOutputByteStream<POSITION_T> _baseStream;
        private int _bufferSize;
        private bool _leaveOpen;
        private byte[] _internalBuffer;
        private int _internalBufferIndex;

        public BufferedRandomOutputStream(IRandomOutputByteStream<POSITION_T> baseStream, bool leaveOpen)
            : this(baseStream, _DEFAULT_BUFFER_SIZE, leaveOpen)
        {
        }

        public BufferedRandomOutputStream(IRandomOutputByteStream<POSITION_T> baseStream, int bufferSize, bool leaveOpen)
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

                if (_internalBufferIndex > 0)
                    return _baseStream.Length.Maximum((GetDistanceBetweenPositions(_baseStream.Position, ZeroPositionValue) + (uint)_internalBufferIndex));
                else
                    return _baseStream.Length;
            }

            set
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                InternalFlush();
                _baseStream.Length = value;
            }
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

            InternalFlush();
            _baseStream.Seek(offset);
        }

        public int Write(IReadOnlyArray<byte> buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > buffer.Length)
                throw new ArgumentException();

            return InternalWrite(buffer, offset, count);
        }

        public void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            InternalFlush();
        }

        public void Close()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            Dispose();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected abstract POSITION_T ZeroPositionValue { get; }
        protected abstract UInt64 GetDistanceBetweenPositions(POSITION_T x, POSITION_T y);
        protected abstract POSITION_T AddPosition(POSITION_T x, UInt64 y);

        protected void Dispose(bool disposing)
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
            }
        }

        private int InternalWrite(IReadOnlyArray<byte> buffer, int offset, int count)
        {
            if (_internalBufferIndex >= _internalBuffer.Length)
            {
                _baseStream.WriteBytes(_internalBuffer.AsReadOnly(), 0, _internalBuffer.Length);
                _internalBufferIndex = 0;
            }
            var actualCount = (_internalBuffer.Length - _internalBufferIndex).Minimum(count);
            buffer.CopyTo(offset, _internalBuffer, _internalBufferIndex, actualCount);
            _internalBufferIndex += actualCount;
            return actualCount;
        }

        private void InternalFlush()
        {
            if (_internalBufferIndex > 0)
            {
                _baseStream.WriteBytes(_internalBuffer.AsReadOnly(), 0, _internalBufferIndex);
                _internalBufferIndex = 0;
            }
            _baseStream.Flush();
        }
    }
}

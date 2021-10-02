using System;

namespace Utility.IO
{
    abstract class BufferedOutputStream<POSITION_T>
        : IOutputByteStream<POSITION_T>
    {
        private const int _MAXIMUM_BUFFER_SIZE = 1024 * 1024;
        private const int _DEFAULT_BUFFER_SIZE = 80 * 1024;
        private const int _MINIMUM_BUFFER_SIZE = 4 * 1024;

        private bool _isDisposed;
        private IOutputByteStream<POSITION_T> _baseStream;
        private int _bufferSize;
        private bool _leaveOpen;
        private UInt64 _position;

        private byte[] _internalBuffer;
        private int _internalBufferIndex;

        public BufferedOutputStream(IOutputByteStream<POSITION_T> baseStream, bool leaveOpen)
            : this(baseStream, _DEFAULT_BUFFER_SIZE, leaveOpen)
        {
        }

        public BufferedOutputStream(IOutputByteStream<POSITION_T> baseStream, int bufferSize, bool leaveOpen)
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

        public POSITION_T Position => !_isDisposed ? AddPosition(ZeroPositionValue, _position) : throw new ObjectDisposedException(GetType().FullName);

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

            var written = InternalWrite(buffer, offset, count);
            _position += (uint)written;
            return written;
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

        protected abstract POSITION_T ZeroPositionValue { get; }
        protected abstract POSITION_T AddPosition(POSITION_T x, UInt64 y);


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

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

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
    }
}
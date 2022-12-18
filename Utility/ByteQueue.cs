using System;

namespace Utility
{
    public class ByteQueue
    {
        private const Int32 _DEFAULT_BUFFER_SIZE = 80 * 1024;

        private readonly byte[] _internalBuffer;

        private Int32 _sizeOfDataInInternalBuffer;
        private Int32 _startOfDataInInternalBuffer;
        private bool _isCompeted;

        public ByteQueue(Int32 bufferSize = _DEFAULT_BUFFER_SIZE)
        {
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            _internalBuffer = new byte[bufferSize];
            _startOfDataInInternalBuffer = 0;
            _sizeOfDataInInternalBuffer = 0;
            _isCompeted = false;
        }

        public bool IsCompleted => _isCompeted;
        public bool IsEmpty => _sizeOfDataInInternalBuffer <= 0;
        public bool IsFull => _sizeOfDataInInternalBuffer >= _internalBuffer.Length;
        public Int32 AvailableDataCount => _sizeOfDataInInternalBuffer;
        public Int32 FreeAreaCount => _internalBuffer.Length - _sizeOfDataInInternalBuffer;
        public Int32 BufferSize => _internalBuffer.Length;

        public Int32 Read(Span<byte> buffer)
        {
            lock (this)
            {
                var actualCount =
                    buffer.Length
                    .Minimum(_sizeOfDataInInternalBuffer)
                    .Minimum(_internalBuffer.Length - _startOfDataInInternalBuffer);
                if (actualCount <= 0)
                {
                    if (buffer.Length > 0 && !_isCompeted)
                        throw new InvalidOperationException("Tried to read even though the buffer is empty.");
                    return 0;
                }
                _internalBuffer.AsSpan(_startOfDataInInternalBuffer, actualCount).CopyTo(buffer[..actualCount]);
                _startOfDataInInternalBuffer += actualCount;
                _sizeOfDataInInternalBuffer -= actualCount;
                if (_startOfDataInInternalBuffer >= _internalBuffer.Length)
                    _startOfDataInInternalBuffer = 0;
#if DEBUG
                if (!_startOfDataInInternalBuffer.InRange(0, _internalBuffer.Length))
                    throw new Exception();
                if (!_sizeOfDataInInternalBuffer.InRange(0, _internalBuffer.Length))
                    throw new Exception();
#endif
                return actualCount;
            }
        }

        public Int32 Write(ReadOnlySpan<byte> buffer)
        {
            lock (this)
            {
                if (_isCompeted)
                    throw new InvalidOperationException("Can not write any more.");

                var actualCount =
                    buffer.Length
                    .Minimum(
                        _sizeOfDataInInternalBuffer >= _internalBuffer.Length - _startOfDataInInternalBuffer
                        ? _internalBuffer.Length - _sizeOfDataInInternalBuffer
                        : _internalBuffer.Length - _sizeOfDataInInternalBuffer - _startOfDataInInternalBuffer);
                var offsetInInputBuffer =
                    _sizeOfDataInInternalBuffer >= _internalBuffer.Length - _startOfDataInInternalBuffer
                    ? _startOfDataInInternalBuffer - (_internalBuffer.Length - _sizeOfDataInInternalBuffer)
                    : _startOfDataInInternalBuffer + _sizeOfDataInInternalBuffer;

                buffer[..actualCount].CopyTo(_internalBuffer.AsSpan(offsetInInputBuffer, actualCount));
                _sizeOfDataInInternalBuffer += actualCount;
                if (_sizeOfDataInInternalBuffer == _internalBuffer.Length)
                    _sizeOfDataInInternalBuffer = 0;
#if DEBUG
                if (!_startOfDataInInternalBuffer.InRange(0, _internalBuffer.Length))
                    throw new Exception();
                if (!_sizeOfDataInInternalBuffer.InRange(0, _internalBuffer.Length))
                    throw new Exception();
#endif
                return actualCount;
            }
        }

        public void Compete()
        {
            lock (this)
            {
                _isCompeted = true;
            }
        }
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.IO
{
    abstract class BufferedInputStream<POSITION_T>
        : IInputByteStream<POSITION_T>
    {
        private const Int32 _MAXIMUM_BUFFER_SIZE = 1024 * 1024;
        private const Int32 _DEFAULT_BUFFER_SIZE = 80 * 1024;
        private const Int32 _MINIMUM_BUFFER_SIZE = 4 * 1024;

        private readonly IInputByteStream<POSITION_T> _baseStream;
        private readonly bool _leaveOpen;
        private readonly Int32 _bufferSize;
        private readonly byte[] _internalBuffer;

        private bool _isDisposed;
        private UInt64 _position;
        private Int32 _internalBufferCount;
        private Int32 _internalBufferIndex;
        private bool _isEndOfStream;
        private bool _isEndOfBaseStream;

        public BufferedInputStream(IInputByteStream<POSITION_T> baseStream, bool leaveOpen)
            : this(baseStream, _DEFAULT_BUFFER_SIZE, leaveOpen)
        {
        }

        public BufferedInputStream(IInputByteStream<POSITION_T> baseStream, Int32 bufferSize, bool leaveOpen)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (bufferSize < 0)
                    throw new ArgumentOutOfRangeException(nameof(bufferSize));

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
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public POSITION_T Position
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return AddPosition(ZeroPositionValue, _position);
            }
        }

        public Int32 Read(Span<byte> buffer)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (_isEndOfStream)
                return 0;

            var length = InternalRead(buffer);
            if (length <= 0)
                _isEndOfStream = true;
            else
            {
#if DEBUG
                checked
#endif
                {
                    _position += (UInt32)length;
                }
            }
            return length;
        }

        public async Task<Int32> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (_isEndOfStream)
                return 0;

            var length = await InternalReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (length <= 0)
                _isEndOfStream = true;
            else
            {
#if DEBUG
                checked
#endif
                {
                    _position += (UInt32)length;
                }
            }
            return length;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        protected abstract POSITION_T ZeroPositionValue { get; }
        protected abstract POSITION_T AddPosition(POSITION_T x, UInt64 y);
        protected bool IsDisposed => _isDisposed;

        protected void SetPosition(UInt64 position)
        {
            _position = position;
        }

        protected void ClearCache()
        {
            _internalBufferCount = 0;
            _internalBufferIndex = 0;
        }

        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (!_leaveOpen)
                        _baseStream.Dispose();
                }
                _isDisposed = true;
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!_isDisposed)
            {
                if (!_leaveOpen)
                    await _baseStream.DisposeAsync().ConfigureAwait(false);
                _isDisposed = true;
            }
        }

        private Int32 InternalRead(Span<byte> buffer)
        {
            if (_isEndOfBaseStream)
                return 0;

            if (IsBufferEmpty)
            {
                if (!SetReadLength(_baseStream.Read(_internalBuffer.AsSpan())))
                    return 0;
            }
            return ReadFromBuffer(buffer);
        }

        private async Task<Int32> InternalReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (_isEndOfBaseStream)
                return 0;

            if (IsBufferEmpty)
            {
                if (!SetReadLength(await _baseStream.ReadAsync(_internalBuffer.AsMemory(), cancellationToken).ConfigureAwait(false)))
                    return 0;
            }
            return ReadFromBuffer(buffer.Span);
        }

        private bool IsBufferEmpty =>
            _internalBufferIndex >= _internalBufferCount;

        private bool SetReadLength(Int32 readLength)
        {
            _internalBufferCount = readLength;
            _internalBufferIndex = 0;
            if (readLength <= 0)
            {
                _isEndOfBaseStream = true;
                return false;
            }
            return true;
        }

        private Int32 ReadFromBuffer(Span<byte> destination)
        {
            var copyCount = (_internalBufferCount - _internalBufferIndex).Minimum(destination.Length);
            _internalBuffer.AsSpan(_internalBufferIndex, copyCount).CopyTo(destination[..copyCount]);
            _internalBufferIndex += copyCount;
            return copyCount;
        }
    }
}

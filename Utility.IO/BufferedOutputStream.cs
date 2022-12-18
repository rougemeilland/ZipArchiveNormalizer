using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.IO
{
    abstract class BufferedOutputStream<POSITION_T>
        : IOutputByteStream<POSITION_T>
    {
        private const Int32 _MAXIMUM_BUFFER_SIZE = 1024 * 1024;
        private const Int32 _DEFAULT_BUFFER_SIZE = 80 * 1024;
        private const Int32 _MINIMUM_BUFFER_SIZE = 4 * 1024;

        private readonly IOutputByteStream<POSITION_T> _baseStream;
        private readonly Int32 _bufferSize;
        private readonly bool _leaveOpen;
        private readonly byte[] _internalBuffer;

        private bool _isDisposed;
        private UInt64 _position;
        private Int32 _internalBufferIndex;

        public BufferedOutputStream(IOutputByteStream<POSITION_T> baseStream, bool leaveOpen)
            : this(baseStream, _DEFAULT_BUFFER_SIZE, leaveOpen)
        {
        }

        public BufferedOutputStream(IOutputByteStream<POSITION_T> baseStream, Int32 bufferSize, bool leaveOpen)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (bufferSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(bufferSize));

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
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public POSITION_T Position => !_isDisposed ? AddPosition(ZeroPositionValue, _position) : throw new ObjectDisposedException(GetType().FullName);

        public Int32 Write(ReadOnlySpan<byte> buffer)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            var written = InternalWrite(buffer);
            _position += (UInt32)written;
            return written;
        }

        public async Task<Int32> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            var written = await InternalWriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            _position += (UInt32)written;
            return written;
        }

        public void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            InternalFlush();
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return InternalFlushAsync(cancellationToken);
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
        protected Int32 CachedDataLength => _internalBufferIndex;

        protected void SetPosition(UInt64 position)
        {
            _position = position;
        }

        protected void InternalFlush()
        {
            if (!IsCacheEmpty)
            {
                _baseStream.WriteBytes(_internalBuffer.AsReadOnly(0, _internalBufferIndex));
                _internalBufferIndex = 0;
            }
            _baseStream.Flush();
        }

        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    try
                    {
                        InternalFlush();
                    }
                    catch (Exception)
                    {
                    }
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
                try
                {
                    await InternalFlushAsync(default).ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
                if (!_leaveOpen)
                    await _baseStream.DisposeAsync().ConfigureAwait(false);
                _isDisposed = true;
            }
        }

        private bool IsCacheFull => _internalBufferIndex >= _internalBuffer.Length;
        private bool IsCacheEmpty => _internalBufferIndex <= 0;

        private Int32 InternalWrite(ReadOnlySpan<byte> buffer)
        {
            if (IsCacheFull)
            {
                _baseStream.WriteBytes(_internalBuffer.AsReadOnly());
                _internalBufferIndex = 0;
            }
            return WriteToCache(buffer);
        }

        private async Task<Int32> InternalWriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (IsCacheFull)
            {
                await _baseStream.WriteBytesAsync(_internalBuffer.AsReadOnly(), cancellationToken).ConfigureAwait(false);
                _internalBufferIndex = 0;
            }
            return WriteToCache(buffer.Span);
        }

        private async Task InternalFlushAsync(CancellationToken cancellationToken)
        {
            if (!IsCacheEmpty)
            {
                await _baseStream.WriteBytesAsync(_internalBuffer.AsReadOnly(0, _internalBufferIndex), default).ConfigureAwait(false);
                _internalBufferIndex = 0;
            }
            await _baseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private Int32 WriteToCache(ReadOnlySpan<byte> buffer)
        {
            var actualCount = (_internalBuffer.Length - _internalBufferIndex).Minimum(buffer.Length);
            buffer[..actualCount].CopyTo(_internalBuffer.AsSpan(_internalBufferIndex, actualCount));
            _internalBufferIndex += actualCount;
            return actualCount;
        }
    }
}

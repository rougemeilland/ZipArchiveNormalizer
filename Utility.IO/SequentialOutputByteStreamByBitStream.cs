using System;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.IO
{
    class SequentialOutputByteStreamByBitStream
        : IOutputByteStream<UInt64>
    {
        private readonly IOutputBitStream _baseStream;
        private readonly BitPackingDirection _bitPackingDirection;
        private readonly bool _leaveOpen;

        private bool _isDisposed;
        private UInt64 _position;

        public SequentialOutputByteStreamByBitStream(IOutputBitStream baseStream, BitPackingDirection bitPackingDirection, bool leaveOpen)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                _isDisposed = false;
                _baseStream = baseStream;
                _bitPackingDirection = bitPackingDirection;
                _leaveOpen = leaveOpen;
                _position = 0;
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public UInt64 Position => !_isDisposed ? _position : throw new ObjectDisposedException(GetType().FullName);

        public Int32 Write(ReadOnlySpan<byte> buffer)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            for (Int32 index = 0; index < buffer.Length; ++index)
                _baseStream.Write(TinyBitArray.FromByte(buffer[index], _bitPackingDirection));
            _position += (UInt32)buffer.Length;
            return buffer.Length;
        }

        public async Task<Int32> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            for (var index = 0; index < buffer.Length; ++index)
                await _baseStream.WriteAsync(TinyBitArray.FromByte(buffer.Span[index], _bitPackingDirection), cancellationToken).ConfigureAwait(false);
            _position += (UInt32)buffer.Length;
            return buffer.Length;
        }

        public void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _baseStream.Flush();
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return _baseStream.FlushAsync(cancellationToken);
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

        protected virtual void Dispose(bool disposing)
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
    }
}

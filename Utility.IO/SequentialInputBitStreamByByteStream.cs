using System;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.IO
{
    class SequentialInputBitStreamByByteStream
        : SequentialInputBitStreamBy
    {
        private readonly IInputByteStream<UInt64> _baseStream;
        private readonly bool _leaveOpen;

        private bool _isDisposed;

        public SequentialInputBitStreamByByteStream(IInputByteStream<UInt64> baseStream, BitPackingDirection bitPackingDirection, bool leaveOpen)
            : base(bitPackingDirection)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                _isDisposed = false;
                _baseStream = baseStream;
                _leaveOpen = leaveOpen;
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        protected override byte? GetNextByte() =>
            _baseStream.ReadByteOrNull();

        protected override Task<byte?> GetNextByteAsync(CancellationToken cancellationToken) =>
            _baseStream.ReadByteOrNullAsync(cancellationToken);

        protected override void Dispose(bool disposing)
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
            base.Dispose(disposing);
        }

        protected async override ValueTask DisposeAsyncCore()
        {
            if (!_isDisposed)
            {
                if (!_leaveOpen)
                    await _baseStream.DisposeAsync().ConfigureAwait(false);
                _isDisposed = true;
            }
            await base.DisposeAsyncCore().ConfigureAwait(false);
        }
    }
}

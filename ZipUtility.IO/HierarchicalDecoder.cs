using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Utility.IO;


namespace ZipUtility.IO
{
    public abstract class HierarchicalDecoder
        : IInputByteStream<UInt64>
    {
        private const UInt64 _PROGRESS_STEP = 80 * 1024;

        private readonly IBasicInputByteStream _baseStream;
        private readonly UInt64 _size;
        private readonly IProgress<UInt64>? _progress;

        private bool _isDisposed;
        private UInt64 _position;
        private bool _isEndOfStream;
        private UInt64 _processedCount;

        public HierarchicalDecoder(IBasicInputByteStream baseStream, UInt64 size, IProgress<UInt64>? progress = null)
        {
            if (baseStream is null)
                throw new ArgumentNullException(nameof(baseStream));

            _isDisposed = false;
            _baseStream = baseStream;
            _size = size;
            _progress = progress;
            _position = 0;
            _processedCount = 0;
        }

        public UInt64 Position => !_isDisposed ? _position : throw new ObjectDisposedException(GetType().FullName);

        public Int32 Read(Span<byte> buffer)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (_isEndOfStream || buffer.Length <= 0)
                return 0;
            var length = ReadFromSourceStream(_baseStream, buffer);
            UpdatePosition(length);
            return length;
        }
        public async Task<Int32> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (_isEndOfStream || buffer.Length <= 0)
                return 0;
            var length = await ReadFromSourceStreamAsync(_baseStream, buffer, cancellationToken).ConfigureAwait(false);
            UpdatePosition(length);
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

        protected virtual Int32 ReadFromSourceStream(IBasicInputByteStream sourceStream, Span<byte> buffer)
        {
            return sourceStream.Read(buffer);
        }

        protected virtual Task<Int32> ReadFromSourceStreamAsync(IBasicInputByteStream sourceStream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return sourceStream.ReadAsync(buffer, cancellationToken);
        }

        protected virtual void OnEndOfStream()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                    _baseStream.Dispose();
                _isDisposed = true;
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!_isDisposed)
            {
                await _baseStream.DisposeAsync().ConfigureAwait(false);
                _isDisposed = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdatePosition(Int32 length)
        {
            if (length > 0)
            {
                _position += (UInt64)length;
                _processedCount += (UInt64)length;
                if (_processedCount >= _PROGRESS_STEP)
                    ReportProgress();
            }
            else
            {
                _isEndOfStream = true;
                if (_position != _size)
                    throw new IOException("Size not match");
                else
                    OnEndOfStream();
                ReportProgress();
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReportProgress()
        {
            try
            {
                _progress?.Report(_processedCount);
            }
            catch (Exception)
            {
            }
        }
    }
}

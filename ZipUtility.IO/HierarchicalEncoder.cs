﻿using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Utility.IO;

namespace ZipUtility.IO
{
    public abstract class HierarchicalEncoder
        : IOutputByteStream<UInt64>
    {
        private const UInt64 _PROGRESS_STEP = 80 * 1024;

        private readonly IBasicOutputByteStream _baseStream;
        private readonly UInt64? _size;
        private readonly IProgress<UInt64>? _progress;

        private bool _isDisposed;
        private UInt64 _position;
        private bool _isEndOfWriting;
        private UInt64 _processedCount;

        public HierarchicalEncoder(IBasicOutputByteStream baseStream, UInt64? size, IProgress<UInt64>? progress = null)
        {
            if (baseStream is null)
                throw new ArgumentNullException(nameof(baseStream));

            _isDisposed = false;
            _baseStream = baseStream;
            _size = size;
            _progress = progress;
            _position = 0;
            _isEndOfWriting = false;
            _processedCount = 0;
        }

        public UInt64 Position
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return _position;
            }
        }

        public Int32 Write(ReadOnlySpan<byte> buffer)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (buffer.Length <= 0)
                return 0;
            var written = WriteToDestinationStream(_baseStream, buffer);
            UpdatePosition(written);
            if (_size.HasValue && _position >= _size.Value)
            {
                FlushDestinationStream(_baseStream, true);
                _isEndOfWriting = true;
            }
            return written;
        }

        public async Task<Int32> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (buffer.Length <= 0)
                return 0;
            var written = await WriteToDestinationStreamAsync(_baseStream, buffer, cancellationToken).ConfigureAwait(false);
            UpdatePosition(written);
            if (_size is not null && _position >= _size.Value)
            {
                await FlushDestinationStreamAsync(_baseStream, true, cancellationToken).ConfigureAwait(false);
                _isEndOfWriting = true;
            }
            return written;
        }

        public void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            try
            {
                FlushDestinationStream(_baseStream, false);
            }
            catch (IOException)
            {
                throw;
            }
            catch (ObjectDisposedException ex)
            {
                throw new IOException("Stream is closed.", ex);
            }
            catch (Exception ex)
            {
                throw new IOException("Can not flush stream.", ex);
            }
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            try
            {
                await FlushDestinationStreamAsync(_baseStream, false, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                throw;
            }
            catch (ObjectDisposedException ex)
            {
                throw new IOException("Stream is closed.", ex);
            }
            catch (Exception ex)
            {
                throw new IOException("Can not flush stream.", ex);
            }
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
                    try
                    {
                        FlushDestinationStream(_baseStream, true);
                        _isEndOfWriting = true;
                    }
                    catch (Exception)
                    {
                    }
                    ReportProgress();
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
                    await FlushDestinationStreamAsync(_baseStream, true, default).ConfigureAwait(false);
                    _isEndOfWriting = true;
                }
                catch (Exception)
                {
                }
                ReportProgress();
                await _baseStream.DisposeAsync().ConfigureAwait(false);
                _isDisposed = true;
            }
        }

        protected virtual Int32 WriteToDestinationStream(IBasicOutputByteStream destinationStream, ReadOnlySpan<byte> buffer)
        {
            return destinationStream.Write(buffer);
        }

        protected virtual Task<Int32> WriteToDestinationStreamAsync(IBasicOutputByteStream destinationStream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            return destinationStream.WriteAsync(buffer, cancellationToken);
        }

        protected virtual void FlushDestinationStream(IBasicOutputByteStream destinationStream, bool isEndOfData)
        {
            if (!_isEndOfWriting)
                destinationStream.Flush();
        }

        protected virtual Task FlushDestinationStreamAsync(IBasicOutputByteStream destinationStream, bool isEndOfData, CancellationToken cancellationToken)
        {
            if (!_isEndOfWriting)
                return destinationStream.FlushAsync(cancellationToken);
            else
                return Task.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdatePosition(Int32 written)
        {
            if (written > 0)
            {
                _position += (UInt64)written;
                _processedCount += (UInt64)written;
                if (_processedCount >= _PROGRESS_STEP)
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

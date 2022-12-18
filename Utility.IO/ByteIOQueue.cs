﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.IO
{
    public class ByteIOQueue
    {
        private class Reader
            : IBasicInputByteStream
        {
            private readonly ByteIOQueue _buffer;

            private bool _isDisposed;

            public Reader(ByteIOQueue buffer)
            {
                _isDisposed = false;
                _buffer = buffer;
            }

            public Int32 Read(Span<byte> buffer)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return _buffer.Read(buffer);
            }

            public Task<Int32> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                cancellationToken.ThrowIfCancellationRequested();

                return Task.FromResult(_buffer.Read(buffer.Span));
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
                        _buffer.Complete();
                    }
                    _isDisposed = true;
                }
            }

            protected virtual ValueTask DisposeAsyncCore()
            {
                if (!_isDisposed)
                {
                    _buffer.Complete();
                    _isDisposed = true;
                }
                return ValueTask.CompletedTask;
            }
        }

        private class Writer
            : IBasicOutputByteStream
        {
            private readonly ByteIOQueue _buffer;

            private bool _isDisposed;

            public Writer(ByteIOQueue buffer)
            {
                _isDisposed = false;
                _buffer = buffer;
            }

            public Int32 Write(ReadOnlySpan<byte> buffer)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return _buffer.Write(buffer);
            }

            public Task<Int32> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                cancellationToken.ThrowIfCancellationRequested();

                return Task.FromResult(_buffer.Write(buffer.Span));
            }

            public void Flush()
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
            }

            public Task FlushAsync(CancellationToken cancellationToken = default)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return Task.CompletedTask;

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
                        _buffer.Complete();
                    _isDisposed = true;
                }
            }

            protected virtual ValueTask DisposeAsyncCore()
            {
                if (!_isDisposed)
                {
                    _buffer.Complete();
                    _isDisposed = true;
                }
                return ValueTask.CompletedTask;
            }
        }


        private readonly ByteQueue _buffer;

        public ByteIOQueue(Int32 bufferSize)
        {
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            _buffer = new ByteQueue(bufferSize);
        }

        public ByteIOQueue(UInt32 bufferSize)
            : this(bufferSize <= Int32.MaxValue ? (Int32)bufferSize : throw new ArgumentOutOfRangeException(nameof(bufferSize)))
        {
        }

        public bool IsCompleted => _buffer.IsCompleted;
        public bool IsEmpty => _buffer.IsEmpty;
        public bool IsFull => _buffer.IsFull;
        public Int32 AvailableDataCount => _buffer.AvailableDataCount;
        public Int32 FreeAreaCount => _buffer.FreeAreaCount;
        public Int32 BufferSize => _buffer.BufferSize;

        public IBasicInputByteStream GetReader() => new Reader(this);

        public IBasicOutputByteStream GetWriter() => new Writer(this);

        private Int32 Read(Span<byte> buffer)
        {
            var length = _buffer.Read(buffer);
            if (length <= 0 && buffer.Length > 0 && !_buffer.IsCompleted)
                throw new InvalidOperationException("Buffer is empty.");
            return length;
        }

        private Int32 Write(ReadOnlySpan<byte> buffer)
        {
            var length = _buffer.Write(buffer);
#if DEBUG
            if (buffer.Length > 0 && length <= 0)
                throw new Exception();
#endif
            return length;
        }

        private void Complete()
        {
            _buffer.Compete();
        }
    }
}

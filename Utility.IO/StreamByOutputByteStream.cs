using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.IO
{
    class StreamByOutputByteStream<BASE_STREAM>
        : Stream
        where BASE_STREAM : IOutputByteStream<UInt64>
    {
        private readonly BASE_STREAM _baseStream;
        private readonly Action<BASE_STREAM> _finishAction;
        private readonly bool _leaveOpen;
        private readonly IRandomInputByteStream<UInt64>? _randomAccessStream;

        private bool _isDisposed;

        public StreamByOutputByteStream(BASE_STREAM baseStream, Action<BASE_STREAM> finishAction, bool leaveOpen)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (finishAction is null)
                    throw new ArgumentNullException(nameof(finishAction));

                _isDisposed = false;
                _baseStream = baseStream;
                _finishAction = finishAction;
                _leaveOpen = leaveOpen;
                _randomAccessStream = baseStream as IRandomInputByteStream<UInt64>;
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public override bool CanSeek => _randomAccessStream is not null;
        public override bool CanRead => false;
        public override bool CanWrite => true;
        public override Int64 Length
        {
            get
            {
                if (_randomAccessStream is null)
                    throw new NotSupportedException();
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (_randomAccessStream.Length > Int64.MaxValue)
                    throw new IOException();

                return (Int64)_randomAccessStream.Length;
            }
        }

        public override void SetLength(Int64 value)
        {
            if (_randomAccessStream is null)
                throw new NotSupportedException();
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _randomAccessStream.Length = (UInt64)value;
        }

        public override Int64 Position
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (_baseStream.Position > Int64.MaxValue)
                    throw new IOException();

                return (Int64)_baseStream.Position;
            }

            set
            {
                if (_randomAccessStream is null)
                    throw new NotSupportedException();
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _randomAccessStream.Seek((UInt64)value);
            }
        }

        public override Int64 Seek(Int64 offset, SeekOrigin origin)
        {
            if (_randomAccessStream is null)
                throw new NotSupportedException();
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            UInt64 absoluteOffset;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0)
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    absoluteOffset = (UInt64)offset;
                    break;
                case SeekOrigin.Current:
                    try
                    {
                        absoluteOffset = _randomAccessStream.Position.AddAsUInt(offset);
                    }
                    catch (OverflowException ex)
                    {
                        throw new ArgumentOutOfRangeException($"Invalid {nameof(offset)} value", ex);
                    }
                    break;
                case SeekOrigin.End:
                    try
                    {
                        absoluteOffset = _randomAccessStream.Length.AddAsUInt(offset);
                    }
                    catch (OverflowException ex)
                    {
                        throw new ArgumentOutOfRangeException($"Invalid {nameof(offset)} value", ex);
                    }
                    break;
                default:
                    throw new ArgumentException($"Invalid {nameof(SeekOrigin)} value", nameof(origin));
            }
            if (absoluteOffset > Int64.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(offset));
            _randomAccessStream.Seek(absoluteOffset);
            return (Int64)absoluteOffset;
        }

        public override Int32 Read(byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();

        public override void Write(byte[] buffer, Int32 offset, Int32 count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _baseStream.WriteBytes(buffer, offset, count);
        }

        public override void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _baseStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return base.FlushAsync(cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    try
                    {
                        _finishAction(_baseStream);
                    }
                    catch (Exception)
                    {
                    }
                    if (!_leaveOpen)
                        _baseStream.Dispose();
                }
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                try
                {
                    _finishAction(_baseStream);
                }
                catch (Exception)
                {
                }
                if (!_leaveOpen)
                    await _baseStream.DisposeAsync().ConfigureAwait(false);
                _isDisposed = true;
            }
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}

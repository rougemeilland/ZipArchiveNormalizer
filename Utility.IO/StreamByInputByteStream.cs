using System;
using System.IO;
using System.Threading.Tasks;

namespace Utility.IO
{
    class StreamByInputByteStream
        : Stream
    {
        private readonly IInputByteStream<UInt64> _baseStream;
        private readonly bool _leaveOpen;
        private readonly IRandomInputByteStream<UInt64>? _randomAccessStream;

        private bool _isDisposed;

        public StreamByInputByteStream(IInputByteStream<UInt64> baseStream, bool leaveOpen)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                _isDisposed = false;
                _baseStream = baseStream;
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
        public override bool CanRead => true;
        public override bool CanWrite => false;

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

        public override Int32 Read(byte[] buffer, Int32 offset, Int32 count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return _baseStream.Read(buffer.AsSpan());
        }

        public override void Write(byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();

        public override void Flush() { }

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

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!_isDisposed)
            {
                if (!_leaveOpen)
                    _baseStream.Dispose();
                _isDisposed = true;
            }
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}

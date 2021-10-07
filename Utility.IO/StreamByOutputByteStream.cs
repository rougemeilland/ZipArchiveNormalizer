using System;
using System.IO;

namespace Utility.IO
{
    class StreamByOutputByteStream
        : Stream
    {
        private bool _isDisposed;
        private IOutputByteStream<UInt64> _baseStream;
        private bool _leaveOpen;
        private IRandomInputByteStream<UInt64> _randomAccessStream;

        public StreamByOutputByteStream(IOutputByteStream<UInt64> baseStream, bool leaveOpen)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                _isDisposed = false;
                _baseStream = baseStream;
                _leaveOpen = leaveOpen;
                _randomAccessStream = baseStream as IRandomInputByteStream<UInt64>;
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public override bool CanSeek => _randomAccessStream != null;
        public override bool CanRead => false;
        public override bool CanWrite => true;
        public override long Length
        {
            get
            {
                if (_randomAccessStream == null)
                    throw new NotSupportedException();
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (_randomAccessStream.Length > long.MaxValue)
                    throw new IOException();

                return (long)_randomAccessStream.Length;
            }
        }

        public override void SetLength(long value)
        {
            if (_randomAccessStream == null)
                throw new NotSupportedException();
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (value < 0)
                throw new ArgumentException();

            _randomAccessStream.Length = (ulong)value;
        }

        public override long Position
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (_baseStream.Position > long.MaxValue)
                    throw new IOException();

                return (long)_baseStream.Position;
            }

            set
            {
                if (_randomAccessStream == null)
                    throw new NotSupportedException();
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (value < 0)
                    throw new ArgumentException();

                _randomAccessStream.Seek((ulong)value);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (_randomAccessStream == null)
                throw new NotSupportedException();
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            ulong absoluteOffset;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0)
                        throw new ArgumentException();
                    absoluteOffset = (ulong)offset;
                    break;
                case SeekOrigin.Current:
                    try
                    {
#if DEBUG
                        checked
#endif
                        {
                            if (offset >= 0)
                                absoluteOffset = _randomAccessStream.Position + (ulong)offset;
                            else
                                absoluteOffset = _randomAccessStream.Position - (ulong)-offset;
                        }
                    }
                    catch (OverflowException ex)
                    {
                        throw new ArgumentException("Invalid offset value", nameof(offset), ex);
                    }
                    break;
                case SeekOrigin.End:
                    try
                    {
#if DEBUG
                        checked
#endif
                        {
                            if (offset >= 0)
                                absoluteOffset = _randomAccessStream.Length + (ulong)offset;
                            else
                                absoluteOffset = _randomAccessStream.Length - (ulong)-offset;
                        }
                    }
                    catch (OverflowException ex)
                    {
                        throw new ArgumentException("Invalid offset value", nameof(offset), ex);
                    }
                    break;
                default:
                    throw new ArgumentException();
            }
            if (absoluteOffset > long.MaxValue)
                throw new ArgumentException();
            _randomAccessStream.Seek(absoluteOffset);
            return (long)absoluteOffset;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _baseStream.WriteBytes(buffer.AsReadOnly(), offset, count);
        }

        public override void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _baseStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_baseStream != null)
                    {
                        try
                        {
                            _baseStream.Close();
                        }
                        catch (Exception)
                        {
                        }
                        if (_leaveOpen == false)
                            _baseStream.Dispose();
                        _baseStream = null;
                    }
                }
                base.Dispose(disposing);
                _isDisposed = true;
            }
        }
    }
}

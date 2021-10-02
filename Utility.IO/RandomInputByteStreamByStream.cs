using System;
using System.IO;

namespace Utility.IO
{
    class RandomInputByteStreamByStream
        : IRandomInputByteStream<UInt64>
    {
        private bool _isDisposed;
        private Stream _baseStream;
        private bool _leaveOpen;

        public RandomInputByteStreamByStream(Stream baseStream, bool leaveOpen)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream.CanRead == false)
                    throw new ArgumentException();
                if (baseStream.CanSeek == false)
                    throw new ArgumentException();

                _baseStream = baseStream;
                _leaveOpen = leaveOpen;
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public ulong Position
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (_baseStream.Position < 0)
                    throw new IOException();

                return (ulong)_baseStream.Position;
            }
        }

        public ulong Length
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (_baseStream.Length < 0)
                    throw new IOException();

                return (ulong)_baseStream.Length;
            }

            set => throw new NotSupportedException();
        }

        public void Seek(ulong offset)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (offset > long.MaxValue)
                throw new IOException();

            _baseStream.Seek((long)offset, SeekOrigin.Begin);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return _baseStream.Read(buffer, offset, count);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_baseStream != null)
                    {
                        if (_leaveOpen == false)
                            _baseStream.Dispose();
                        _baseStream = null;
                    }
                }
                _isDisposed = true;
            }
        }
    }
}

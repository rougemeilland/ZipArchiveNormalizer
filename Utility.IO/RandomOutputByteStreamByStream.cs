using System;
using System.IO;

namespace Utility.IO
{
    class RandomOutputByteStreamByStream
        : IRandomOutputByteStream<UInt64>
    {
        private bool _isDisposed;
        private Stream _baseStream;
        private bool _leaveOpen;

        public RandomOutputByteStreamByStream(Stream baseStream, bool leaveOpen)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream.CanWrite == false)
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

            set
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (value > long.MaxValue)
                    throw new IOException();

                _baseStream.SetLength((long)value);
            }
        }

        public void Seek(ulong offset)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (offset > long.MaxValue)
                throw new IOException();

            _baseStream.Seek((long)offset, SeekOrigin.Begin);
        }

        public int Write(IReadOnlyArray<byte> buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _baseStream.Write(buffer?.GetRawArray(), offset, count);
            return count;
        }

        public void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _baseStream.Flush();
        }

        public void Close()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            Dispose();
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

using System;
using System.IO;

namespace Utility.IO
{
    class SequentialInputByteStreamByStream
        : IInputByteStream<UInt64>
    {
        private bool _isDisposed;
        private Stream _baseStream;
        private bool _leaveOpen;
        private ulong _position;

        public SequentialInputByteStreamByStream(Stream baseStream, bool leaveOpen)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream.CanRead == false)
                    throw new ArgumentException();

                _baseStream = baseStream;
                _leaveOpen = leaveOpen;
                _position = 0;
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public ulong Position => !_isDisposed ? _position : throw new ObjectDisposedException(GetType().FullName);

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            var length = _baseStream.Read(buffer, offset, count);
            if (length > 0)
                _position += (ulong)length;
            return length;
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

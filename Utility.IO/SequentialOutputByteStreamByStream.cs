using System;
using System.IO;

namespace Utility.IO
{
    class SequentialOutputByteStreamByStream
        : IOutputByteStream<UInt64>
    {
        private bool _isDisposed;
        private Stream _baseStream;
        private bool _leaveOpne;
        private ulong _position;

        public SequentialOutputByteStreamByStream(Stream baseStream, bool leaveOpen)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream.CanWrite == false)
                    throw new ArgumentException();

                _baseStream = baseStream;
                _leaveOpne = leaveOpen;
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

        public int Write(IReadOnlyArray<byte> buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _baseStream.Write(buffer?.GetRawArray(), offset, count);
            _position += (ulong)count;
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

            _baseStream.Close();
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
                        if (_leaveOpne == false)
                            _baseStream.Dispose();
                        _baseStream = null;
                    }
                }
                _isDisposed = true;
            }
        }
    }
}

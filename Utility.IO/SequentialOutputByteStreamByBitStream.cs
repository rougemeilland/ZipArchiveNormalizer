using System;

namespace Utility.IO
{
    class SequentialOutputByteStreamByBitStream
        : IOutputByteStream<UInt64>
    {
        private bool _isDisposed;
        private IOutputBitStream _baseStream;
        private BitPackingDirection _packingDirection;
        private bool _leaveOpen;
        private ulong _position;

        public SequentialOutputByteStreamByBitStream(IOutputBitStream baseStream, BitPackingDirection packingDirection, bool leaveOpen)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                _isDisposed = false;
                _baseStream = baseStream;
                _packingDirection = packingDirection;
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

        public int Write(IReadOnlyArray<byte> buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > buffer.Length)
                throw new ArgumentException();

            for (int index = 0; index < count; ++index)
                _baseStream.Write(TinyBitArray.FromByte(buffer[offset + index], packingDirection: _packingDirection));
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
                    }
                }
                _isDisposed = true;
            }
        }
    }
}

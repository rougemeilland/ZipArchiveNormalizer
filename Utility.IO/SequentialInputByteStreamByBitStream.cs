using System;

namespace Utility.IO
{
    class SequentialInputByteStreamByBitStream
        : IInputByteStream<UInt64>
    {
        private bool _isDisposed;
        private IInputBitStream _baseStream;
        private BitPackingDirection _packingDirection;
        private bool _leaveOpen;
        private ulong _position;
        private BitQueue _bitQueue;
        private bool _isEndOfBaseStream;
        private bool _isEndOfStream;

        public SequentialInputByteStreamByBitStream(IInputBitStream baseStream, BitPackingDirection packingDirection, bool leaveOpen)
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
                _bitQueue = new BitQueue();
                _isEndOfBaseStream = false;
                _isEndOfStream = false;
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
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > buffer.Length)
                throw new ArgumentException();

            if (_isEndOfStream)
                return 0;


            while (_isEndOfBaseStream == false && _bitQueue.Count < BitQueue.RecommendedMaxCount)
            {
                var bitArray = _baseStream.ReadBits(BitQueue.RecommendedMaxCount - _bitQueue.Count);
                if (bitArray == null)
                {
                    _isEndOfBaseStream = true;
                    break;
                }
                _bitQueue.Enqueue(bitArray);
            }
            if (_bitQueue.Count <= 0)
            {
                _isEndOfStream = true;
                return 0;
            }
            var actualCount = 0;
            while (actualCount < count && _bitQueue.Count >= 8)
            {
                buffer[offset + actualCount] = _bitQueue.DequeueByte(_packingDirection);
                ++actualCount;
            }
            if (actualCount <= 0 && _bitQueue.Count > 0)
            {
                buffer[offset + actualCount] = _bitQueue.DequeueByte(_packingDirection);
                ++actualCount;
            }
            _position += (ulong)actualCount;
            return actualCount;
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

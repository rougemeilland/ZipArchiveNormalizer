using System;

namespace Utility.IO
{
    abstract class SequentialInputBitStreamBy
        : IInputBitStream
    {
        private bool _isDisposed;
        private BitPackingDirection _packingDirection;
        private BitQueue _bitQueue;
        private bool _isEndOfSourceSequence;
        private bool _isEndOfSequence;

        protected SequentialInputBitStreamBy(BitPackingDirection packingDirection)
        {
            _isDisposed = false;
            _packingDirection = packingDirection;
            _bitQueue = new BitQueue();
            _isEndOfSourceSequence = false;
            _isEndOfSequence = false;
        }

        public bool? ReadBit()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (_isEndOfSequence)
                return null;

            if (_isEndOfSourceSequence == false && _bitQueue.Count <= 0)
                ReadNextByte();
            if (_bitQueue.Count <= 0)
            {
                _isEndOfSequence = true;
                return null;
            }
            return _bitQueue.DequeueBoolean();
        }

        public TinyBitArray ReadBits(int bitCount)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (bitCount < 1)
                throw new ArgumentException();

            var maximumCount = BitQueue.RecommendedMaxCount - 8;
            bitCount = bitCount.Minimum(maximumCount);
            if (bitCount < 0)
                throw new ArgumentException();
            if (_isEndOfSequence)
                return null;

            while (_isEndOfSourceSequence == false && _bitQueue.Count < bitCount)
                ReadNextByte();
            if (_bitQueue.Count <= 0)
            {
                _isEndOfSequence = true;
                return null;
            }
            return _bitQueue.DequeueBitArray(bitCount.Minimum(_bitQueue.Count));
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected abstract byte? GetNextByte();

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                }
                _isDisposed = true;
            }
        }

        private void ReadNextByte()
        {
            var data = GetNextByte();
            if (data.HasValue)
                _bitQueue.Enqueue(data.Value, packingDirection: _packingDirection);
            else
                _isEndOfSourceSequence = true;
        }
    }
}
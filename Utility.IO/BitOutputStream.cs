using System;
using System.IO;

namespace Utility.IO
{
    public class BitOutputStream
        : IDisposable
    {
        private bool _isDisosed;
        private Stream _baseStream;
        private bool _leaveOpen;
        private BitQueue _bitQueue;


        public BitOutputStream(Stream baseStream, bool leaveOpen = false)
            : this(baseStream, BitPackingDirection.MsbToLsb, leaveOpen)
        {
        }

        public BitOutputStream(Stream baseStream, BitPackingDirection packingDirection, bool leaveOpen = false)
        {
            if (baseStream == null)
                throw new ArgumentNullException("baseStream");
            if (baseStream.CanWrite == false)
                throw new ArgumentException("'baseStream' is not suppot 'Write'.", "baseStream");

            _isDisosed = false;
            _baseStream = baseStream;
            PackingDirection = packingDirection;
            _leaveOpen = leaveOpen;
            _bitQueue = new BitQueue();
        }

        public BitPackingDirection PackingDirection { get; }


        public void Write(TinyBitArray bitArray)
        {
            if (bitArray == null)
                throw new ArgumentNullException("bitArray");

            while (bitArray.Length > 0)
            {
                var bitCount = (BitQueue.RecommendedMaxCount - _bitQueue.Count).Minimum(bitArray.Length);
                var data = bitArray.Divide(bitCount, out bitArray);
                _bitQueue.Enqueue(data);
                while (_bitQueue.Count >= 8)
                    _baseStream.Write(new[] { _bitQueue.DequeueByte(PackingDirection) }, 0, 1);
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisosed)
            {
                if (disposing)
                {
                    if (_baseStream != null)
                    {
#if DEBUG
                        if (_bitQueue.Count.IsAnyOf(0, 7) == false)
                            throw new Exception();
#endif
                        if (_bitQueue.Count > 0)
                        {
                            _bitQueue.Enqueue((Byte)0, 8 - _bitQueue.Count);
#if DEBUG
                            if (_bitQueue.Count != 8)
                                throw new Exception();
#endif
                            _baseStream.Write(new[] { _bitQueue.DequeueByte(PackingDirection) }, 0, 1);
                        }
                        if (_leaveOpen == false)
                            _baseStream.Dispose();
                        _baseStream = null;
                    }
                }
                _isDisosed = true;
            }
        }
    }
}

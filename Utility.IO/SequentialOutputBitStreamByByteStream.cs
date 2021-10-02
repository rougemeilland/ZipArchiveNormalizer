using System;

namespace Utility.IO
{
    class SequentialOutputBitStreamByByteStream
        : IOutputBitStream
    {
        private bool _isDisposed;
        private IOutputByteStream<UInt64> _baseStream;
        private BitPackingDirection _packingDirection;
        private bool _leaveOpen;
        private BitQueue _bitQueue;

        public SequentialOutputBitStreamByByteStream(IOutputByteStream<UInt64> baseStream, BitPackingDirection packingDirection, bool leaveOpen)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                _isDisposed = false;
                _baseStream = baseStream;
                _packingDirection = packingDirection;
                _leaveOpen = leaveOpen;
                _bitQueue = new BitQueue();
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public void Write(bool bit)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _bitQueue.Enqueue(bit);
            WriteBytes();
        }

        public void Write(TinyBitArray bitArray)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (bitArray == null)
                throw new ArgumentNullException(nameof(bitArray));

            while (bitArray.Length > 0)
            {
                var bitCount = (BitQueue.RecommendedMaxCount - _bitQueue.Count).Minimum(bitArray.Length);
                var data = bitArray.Divide(bitCount, out bitArray);
                _bitQueue.Enqueue(data);
                WriteBytes();
            }
        }

        public void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _baseStream.Flush();
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
                        try
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
                            }
#if DEBUG
                            if (_bitQueue.Count % 8 != 0)
                                throw new Exception();
#endif
                            WriteBytes();
                            _baseStream.Flush();
                        }
                        catch (Exception)
                        {
                        }
                        if (_leaveOpen == false)
                            _baseStream.Dispose();
                        _baseStream = null;
                    }
                }
                _isDisposed = true;
            }
        }

        private void WriteBytes()
        {
            while (_bitQueue.Count >= 8)
                _baseStream.WriteBytes(new[] { _bitQueue.DequeueByte(_packingDirection) }.AsReadOnly(), 0, 1);
        }
    }
}

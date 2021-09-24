using System;
using System.Collections.Generic;
using System.IO;

namespace Utility.IO
{
    public class BitInputStream
        : IDisposable
    {
        private bool _isDisosed;
        private IEnumerator<byte> _sourceSequenceEnumerator;
        private BitQueue _bitQueue;
        private bool _isEndOfBaseStream;
        private bool _isEndOfStream;

        public BitInputStream(Stream baseStream, bool leaveOpen = false)
            : this(baseStream, BitPackingDirection.MsbToLsb, leaveOpen)
        {
        }

        public BitInputStream(Stream baseStream, BitPackingDirection packingDirection, bool leaveOpen = false)
        {
            if (baseStream == null)
                throw new ArgumentNullException("baseStream");
            if (baseStream.CanRead == false)
                throw new ArgumentException("'baseStream' is not suppot 'Read'.", "baseStream");

            Initialize(baseStream.GetByteSequence(leaveOpen), packingDirection);
        }

        public BitInputStream(IEnumerable<byte> sourceSequence, BitPackingDirection packingDirection)
        {
            if (sourceSequence == null)
                throw new ArgumentNullException("sourceSequence");

            Initialize(sourceSequence, packingDirection);
        }

        public BitPackingDirection PackingDirection { get; private set; }

        public TinyBitArray Read(int count)
        {
            if (count < 0)
                throw new ArgumentException();
            if (_isEndOfStream)
                return null;

            count = count.Minimum(BitQueue.RecommendedMaxCount - 8);
            while (_isEndOfBaseStream == false && _bitQueue.Count < count)
            {
                if (_sourceSequenceEnumerator.MoveNext() == false)
                {
                    _isEndOfBaseStream = true;
                    break;
                }
                _bitQueue.Enqueue(_sourceSequenceEnumerator.Current, packingDirection: PackingDirection);
            }
            if (_bitQueue.Count <= 0)
            {
                _isEndOfStream = true;
                return null;
            }
            return _bitQueue.DequeueBitArray(count.Minimum(_bitQueue.Count));
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
                    if (_sourceSequenceEnumerator != null)
                    {
                        _sourceSequenceEnumerator.Dispose();
                        _sourceSequenceEnumerator = null;
                    }
                }
                _isDisosed = true;
            }
        }

        private void Initialize(IEnumerable<byte> sourceSequence, BitPackingDirection packingDirection)
        {
            _isDisosed = false;
            PackingDirection = packingDirection;
            _sourceSequenceEnumerator = sourceSequence.GetEnumerator();
            _bitQueue = new BitQueue();
            _isEndOfBaseStream = false;
            _isEndOfStream = false;
        }
    }
}

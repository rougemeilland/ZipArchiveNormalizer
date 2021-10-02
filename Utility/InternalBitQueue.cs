using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    internal class InternalBitQueue
        : IReadOnlyArray<bool>, IEquatable<InternalBitQueue>, ICloneable<InternalBitQueue>

    {
        private class RandomAccessBitQueue
                : IReadOnlyArray<bool>, ICloneable<RandomAccessBitQueue>, IEquatable<RandomAccessBitQueue>
        {
            private class RandomAccessQueue<ELEMENT_T>
                : IReadOnlyArray<ELEMENT_T>, ICloneable<RandomAccessQueue<ELEMENT_T>>, IEquatable<RandomAccessQueue<ELEMENT_T>>
                where ELEMENT_T : IEquatable<ELEMENT_T>
            {
                private SortedDictionary<long, ELEMENT_T> _queue;
                private long _indexOfStart;
                private long _indexOfEnd;

                public RandomAccessQueue()
                {
                    _queue = new SortedDictionary<long, ELEMENT_T>();
                    _indexOfStart = 0;
                    _indexOfEnd = 0;
                }

                public RandomAccessQueue(IEnumerable<ELEMENT_T> dataSource)
                    : this()
                {
                    foreach (var value in dataSource)
                    {
                        _queue.Add(_indexOfEnd, value);
                        ++_indexOfEnd;
                    }
                    Normalize();
#if DEBUG
                    Check();
#endif
                }

                public void Enqueue(ELEMENT_T value)
                {
                    _queue.Add(_indexOfEnd, value);
                    ++_indexOfEnd;
                    Normalize();
#if DEBUG
                    Check();
#endif
                }

                public ELEMENT_T Dequeue()
                {
                    if (_queue.Count <= 0)
                        throw new InvalidOperationException();
                    var value = _queue[_indexOfStart];
                    _queue.Remove(_indexOfStart);
                    ++_indexOfStart;
                    Normalize();
#if DEBUG
                    Check();
#endif
                    return value;
                }

                public ELEMENT_T this[int index] => _queue[_indexOfStart + index];
                public int Length => _queue.Count;
                public IEnumerator<ELEMENT_T> GetEnumerator() => _queue.Values.GetEnumerator();
                public ELEMENT_T[] DuplicateAsWritableArray() => _queue.Values.ToArray();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                [Obsolete]
                ELEMENT_T[] IReadOnlyArray<ELEMENT_T>.ToArray() => throw new NotSupportedException();


                public void CopyTo(ELEMENT_T[] destinationArray, int destinationOffset)
                {
                    if (destinationArray == null)
                        throw new ArgumentNullException();
                    if (destinationOffset < 0)
                        throw new IndexOutOfRangeException();
                    if (destinationOffset > destinationArray.Length)
                        throw new IndexOutOfRangeException();
                    if (destinationArray.Length - destinationOffset > _queue.Count)
                        throw new IndexOutOfRangeException();
                    InternalCopyTo(0, destinationArray, destinationOffset, destinationArray.Length - destinationOffset);
                }

                public void CopyTo(int sourceOffset, ELEMENT_T[] destinationArray, int destinationOffset, int count)
                {
                    if (sourceOffset < 0)
                        throw new IndexOutOfRangeException();
                    if (destinationArray == null)
                        throw new ArgumentNullException();
                    if (destinationOffset < 0)
                        throw new IndexOutOfRangeException();
                    if (count < 0)
                        throw new IndexOutOfRangeException();
                    if (sourceOffset + count > _queue.Count)
                        throw new IndexOutOfRangeException();
                    if (destinationOffset + count > destinationArray.Length)
                        throw new IndexOutOfRangeException();
                    InternalCopyTo(sourceOffset, destinationArray, destinationOffset, count);
                }

                public RandomAccessQueue<ELEMENT_T> Clone()
                {
                    return new RandomAccessQueue<ELEMENT_T>(_queue.Values);
                }

                public bool Equals(RandomAccessQueue<ELEMENT_T> other)
                {
                    if (other == null)
                        return false;
                    if (_queue.Count != other._queue.Count)
                        return false;
                    if (_queue.Values.SequenceEqual(other._queue.Values) == false)
                        return false;
                    return true;
                }

                public override bool Equals(object obj)
                {
                    if (obj == null || GetType() != obj.GetType())
                        return false;
                    return Equals((RandomAccessQueue<ELEMENT_T>)obj);
                }

                public override int GetHashCode()
                {
                    return _queue.Values.Aggregate(0, (hashCode, value) => hashCode ^ value.GetHashCode());
                }

                private void InternalCopyTo(int sourceOffset, ELEMENT_T[] destinationArray, int destinationOffset, int count)
                {
                    for (var index = 0; index < count; ++index)
                        destinationArray[destinationOffset + index] = _queue[_indexOfStart + sourceOffset + index];
                }

                private void Normalize()
                {
                    if (_queue.Count <= 0)
                    {
                        _indexOfStart = 0;
                        _indexOfEnd = 0;
                    }
                    if (_indexOfStart < 0 || _indexOfEnd < 0)
                    {
                        _queue.Clear();
                        foreach (var item in _queue.Values.Select((value, index) => new { value, index }))
                            _queue[item.index] = item.value;
                        _indexOfStart = 0;
                        _indexOfEnd = _queue.Count;
                    }
                }

#if DEBUG
                private void Check()
                {
                    if (_queue.Count > 0)
                    {
                        if (_queue.Keys.First() != _indexOfStart)
                            throw new Exception();
                        if (_queue.Keys.Last() + 1 != _indexOfEnd)
                            throw new Exception();
                        if (_queue.Count != _indexOfEnd - _indexOfStart)
                            throw new Exception();
                    }
                    else
                    {
                        if (_indexOfStart != 0)
                            throw new Exception();
                        if (_indexOfEnd != 0)
                            throw new Exception();
                    }
                }
#endif
            }


            //
            //          _firstBitArray                   _queue                     _LastBitArray
            //  [LSB..........................MSB]([LSB...MSB][LSB...MSB]...)[LSB..........................MSB]
            //  |<=_firstBitArrayLength=>|                                   |<=_LastBitArrayLength=>|
            //  <- FIRST..................         ............................................LAST ->
            //

            private UInt64 _firstBitArray;
            private int _firstBitArrayLength;
            private RandomAccessQueue<UInt64> _queue;
            private UInt64 _lastBitArray;
            private int _lastBitArrayLength;
            private int _totalBitLength;

            public RandomAccessBitQueue()
                : this(0, 0, new RandomAccessQueue<UInt64>(), 0, 0)
            {
            }

            public RandomAccessBitQueue(IEnumerable<bool> sequence)
            {
                var uint64Array =
                    sequence
                    .ToChunkOfReadOnlyArray(BIT_LENGTH_OF_UINT64)
                    .Select(bitArray => new
                    {
                        bitLength = bitArray.Length,
                        uint64Array =
                            Enumerable.Range(0, bitArray.Length)
                            .Select(bitIndex => bitArray[bitIndex] ? (1UL << bitIndex) : 0UL)
                            .Aggregate(0UL, (x, y) => x | y)
                    })
                    .ToArray();
                if (uint64Array.Length <= 0)
                {
                    _firstBitArray = 0;
                    _firstBitArrayLength = 0;
                    _queue = new RandomAccessQueue<UInt64>();
                    _lastBitArray = 0;
                    _lastBitArrayLength = 0;
                    _totalBitLength = 0;
                }
                else if (uint64Array[uint64Array.Length - 1].bitLength >= BIT_LENGTH_OF_UINT64)
                {
                    _firstBitArray = 0;
                    _firstBitArrayLength = 0;
                    _queue = new RandomAccessQueue<UInt64>(uint64Array.Select(item => item.uint64Array));
                    _lastBitArray = 0;
                    _lastBitArrayLength = 0;
                    _totalBitLength = _queue.Length * BIT_LENGTH_OF_UINT64;
                }
                else
                {
                    _firstBitArray = 0;
                    _firstBitArrayLength = 0;
                    _queue = new RandomAccessQueue<UInt64>(uint64Array.Select(item => item.uint64Array).Take(uint64Array.Length - 1));
                    _lastBitArray = uint64Array[uint64Array.Length - 1].uint64Array;
                    _lastBitArrayLength = uint64Array[uint64Array.Length - 1].bitLength;
                    _totalBitLength = _queue.Length * BIT_LENGTH_OF_UINT64 + _lastBitArrayLength;
                }
                Normalize();
#if DEBUG
                Check();
#endif
            }

            private RandomAccessBitQueue(UInt64 firstBitArray, int firstBitArrayLength, RandomAccessQueue<UInt64> queue, UInt64 lastBitArray, int lastBitArrayLength)
            {
                _firstBitArray = firstBitArray;
                _firstBitArrayLength = firstBitArrayLength;
                _queue = queue;
                _lastBitArray = lastBitArray;
                _lastBitArrayLength = lastBitArrayLength;
                _totalBitLength = firstBitArrayLength + queue.Length * BIT_LENGTH_OF_UINT64 + lastBitArrayLength;
                Normalize();
#if DEBUG
                Check();
#endif
            }

            public void Enqueue(UInt64 bitArray, int bitCount)
            {
                while (bitCount > 0)
                {
                    var length = InternalEnqueue(bitArray, bitCount);
                    bitCount -= length;
                    bitArray >>= length;
                }
            }

            public UInt64 Dequeue(int bitCount)
            {
                var result = 0UL;
                var bitIndex = 0;
                while (bitIndex < bitCount)
                {
                    UInt64 value;
                    var length = InternalDequeue(bitCount - bitIndex, out value);
                    result |= value << bitIndex;
                    bitIndex += length;
                }
                return result;
            }


            public int Length => _totalBitLength;

            public bool this[int index]
            {
                get
                {
                    if (index < 0)
                        throw new IndexOutOfRangeException();
                    if (index < _firstBitArrayLength)
                        return (_firstBitArray & (1UL << index)) != 0;
                    index -= _firstBitArrayLength;
                    if (index < _queue.Length * BIT_LENGTH_OF_UINT64)
                    {
                        var arrayIndex = index / BIT_LENGTH_OF_UINT64;
                        var bitIndex = index % BIT_LENGTH_OF_UINT64;
                        return (_queue[arrayIndex] & (1UL << bitIndex)) != 0;
                    }
                    index -= _queue.Length * BIT_LENGTH_OF_UINT64;
                    if (index < _lastBitArrayLength)
                        return (_lastBitArray & (1UL << index)) != 0;
                    throw new IndexOutOfRangeException();
                }
            }

            public void CopyTo(bool[] destinationArray, int destinationOffset)
            {
                if (destinationArray == null)
                    throw new ArgumentNullException(nameof(destinationArray));
                if (destinationOffset < 0)
                    throw new IndexOutOfRangeException();
                if (destinationOffset > destinationArray.Length)
                    throw new IndexOutOfRangeException();
                if (destinationArray.Length - destinationOffset > _totalBitLength)
                    throw new IndexOutOfRangeException();
                InternalCopyTo(0, destinationArray, destinationOffset, destinationArray.Length - destinationOffset);
            }

            public void CopyTo(int sourceOffset, bool[] destinationArray, int destinationOffset, int count)
            {
                if (sourceOffset < 0)
                    throw new IndexOutOfRangeException();
                if (destinationArray == null)
                    throw new ArgumentNullException(nameof(destinationArray));
                if (destinationOffset < 0)
                    throw new IndexOutOfRangeException();
                if (count < 0)
                    throw new IndexOutOfRangeException();
                if (sourceOffset + count > _totalBitLength)
                    throw new IndexOutOfRangeException();
                if (destinationOffset + count > destinationArray.Length)
                    throw new IndexOutOfRangeException();
                InternalCopyTo(sourceOffset, destinationArray, destinationOffset, count);
            }

            public bool[] DuplicateAsWritableArray()
            {
                var buffer = new bool[_totalBitLength];
                InternalCopyTo(0, buffer, 0, buffer.Length);
                return buffer;
            }

            public IEnumerator<bool> GetEnumerator() =>
                Enumerable.Range(0, _firstBitArrayLength)
                .Select(bitIndex => (_firstBitArray & (1UL << bitIndex)) != 0)
                .Concat(
                    _queue
                    .SelectMany(bitArray =>
                        Enumerable.Range(0, BIT_LENGTH_OF_UINT64)
                        .Select(bitIndex => (bitArray & (1UL << bitIndex)) != 0)))
                .Concat(
                    Enumerable.Range(0, _lastBitArrayLength)
                    .Select(bitIndex => (_lastBitArray & (1UL << bitIndex)) != 0))
                .GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public RandomAccessBitQueue Clone() => new RandomAccessBitQueue(_firstBitArray, _firstBitArrayLength, _queue.Clone(), _lastBitArray, _lastBitArrayLength);
            public bool Equals(RandomAccessBitQueue other) => other != null && this.SequenceEqual(other);

            [Obsolete]
            bool[] IReadOnlyArray<bool>.ToArray() => throw new NotSupportedException();

            private int InternalEnqueue(UInt64 bitArray, int bitCount)
            {
                if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT64) == false)
                    throw new ArgumentException();
#if DEBUG
                if (_lastBitArrayLength >= BIT_LENGTH_OF_UINT64)
                    throw new Exception();
#endif
                var actualBitCount = bitCount.Minimum(BIT_LENGTH_OF_UINT64 - _lastBitArrayLength);
                _lastBitArray |= (bitArray & (UInt64.MaxValue >> (BIT_LENGTH_OF_UINT64 - actualBitCount))) << _lastBitArrayLength;
                _lastBitArrayLength += actualBitCount;
                if (_lastBitArrayLength >= BIT_LENGTH_OF_UINT64)
                {
                    _queue.Enqueue(_lastBitArray);
                    _lastBitArray = 0;
                    _lastBitArrayLength = 0;
                }
                Normalize();
#if DEBUG
                Check();
#endif
                return actualBitCount;
            }

            private int InternalDequeue(int bitCount, out UInt64 resultBitArray)
            {
                if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT64) == false)
                    throw new ArgumentException();
#if DEBUG
                if (_lastBitArrayLength >= BIT_LENGTH_OF_UINT64)
                    throw new Exception();
#endif
                if (_firstBitArrayLength <= 0 && _queue.Length > 0)
                {
                    _firstBitArray = _queue.Dequeue();
                    _firstBitArrayLength = BIT_LENGTH_OF_UINT64;
                }
                if (_firstBitArrayLength <= 0)
                    throw new InvalidOperationException();
                var actualBitCount = bitCount.Minimum(_firstBitArrayLength);
                if (actualBitCount >= BIT_LENGTH_OF_UINT64)
                {
                    resultBitArray = _firstBitArray;
                    _firstBitArray = 0;
                    _firstBitArrayLength = 0;
                }
                else
                {
                    resultBitArray = _firstBitArray & (UInt64.MaxValue >> (BIT_LENGTH_OF_UINT64 - actualBitCount));
                    _firstBitArray >>= actualBitCount;
                    _firstBitArrayLength -= actualBitCount;
                }
                Normalize();
#if DEBUG
                Check();
#endif
                return actualBitCount;
            }

            private void InternalCopyTo(int sourceOffset, bool[] destinationArray, int destinationOffset, int count)
            {
                var sourceIndex = sourceOffset;
                var destinationIndex = destinationOffset;
                var base1 = 0;
                var limit1 = _firstBitArrayLength;
                while (sourceIndex < limit1 && count > 0)
                {
                    var sourceShiftCount = sourceIndex - base1;
                    destinationArray[destinationIndex] = (_firstBitArray & (1UL << sourceShiftCount)) != 0;
                    ++sourceIndex;
                    ++destinationIndex;
                    --count;
                }
                var base2 = _firstBitArrayLength;
                var limit2 = _firstBitArrayLength + _queue.Length * BIT_LENGTH_OF_UINT64;
                while (sourceIndex < limit2 && count > 0)
                {
                    var sourceArrayIndex = (sourceIndex - base2) / BIT_LENGTH_OF_UINT64;
                    var sourceShiftCount = (sourceIndex - base2) % BIT_LENGTH_OF_UINT64;
                    destinationArray[destinationIndex] = (_queue[sourceArrayIndex] & (1UL << sourceShiftCount)) != 0;
                    ++sourceIndex;
                    ++destinationIndex;
                    --count;
                }
                var base3 = _firstBitArrayLength + _queue.Length * BIT_LENGTH_OF_UINT64;
                var limit3 = _firstBitArrayLength + _queue.Length * BIT_LENGTH_OF_UINT64 + _lastBitArrayLength;
                while (sourceIndex < limit3 && count > 0)
                {
                    var sourceShiftCount = sourceIndex - base3;
                    destinationArray[destinationIndex] = (_lastBitArray & (1UL << sourceShiftCount)) != 0;
                    ++sourceIndex;
                    ++destinationIndex;
                    --count;
                }
            }

            private void Normalize()
            {
                if (_firstBitArrayLength <= 0 && _queue.Length <= 0 && _lastBitArrayLength > 0)
                {
                    _firstBitArray = _lastBitArray;
                    _firstBitArrayLength = _lastBitArrayLength;
                    _lastBitArray = 0;
                    _lastBitArrayLength = 0;
                }
                _totalBitLength = _firstBitArrayLength + _queue.Length * BIT_LENGTH_OF_UINT64 + _lastBitArrayLength;
            }

#if DEBUG
            private void Check()
            {
                if (_firstBitArrayLength < 0 || _firstBitArrayLength >= BIT_LENGTH_OF_UINT64)
                    throw new Exception();
                if (_lastBitArrayLength < 0 || _lastBitArrayLength >= BIT_LENGTH_OF_UINT64)
                    throw new Exception();
                if ((_firstBitArray & (UInt64.MaxValue << _firstBitArrayLength)) != 0)
                    throw new Exception();
                if ((_lastBitArray & (UInt64.MaxValue << _lastBitArrayLength)) != 0)
                    throw new Exception();
                if (_totalBitLength != _firstBitArrayLength + _queue.Length * BIT_LENGTH_OF_UINT64 + _lastBitArrayLength)
                    throw new Exception();

            }
#endif
        }

        public const int BIT_LENGTH_OF_BYTE = sizeof(Byte) << 3;
        public const int BIT_LENGTH_OF_UINT16 = sizeof(UInt16) << 3;
        public const int BIT_LENGTH_OF_UINT32 = sizeof(UInt32) << 3;
        public const int BIT_LENGTH_OF_UINT64 = sizeof(UInt64) << 3;
        public const int RecommendedMaxCount = BIT_LENGTH_OF_UINT64;

        //
        // internal bit packing of _bitArray:
        //
        //              _bitArray
        //             [LSB.........MSB]
        //  bit index:  0............63
        //
        private UInt64 _bitArray;
        private int _bitLength;
        private RandomAccessBitQueue _additionalBitArray;
        private int _totalBitLength;

        public InternalBitQueue()
            : this(0, 0, null)
        {
        }

        public InternalBitQueue(IEnumerable<bool> bitPattern)
        {
            _bitLength = 0;
            _bitArray = 0;
            _additionalBitArray = null;
            _totalBitLength = 0;
            using (var enumerator = bitPattern.GetEnumerator())
            {
                var endOFSequence = false;
                for (var index = 0; index < BIT_LENGTH_OF_UINT64; ++index)
                {
                    if (enumerator.MoveNext() == false)
                    {
                        endOFSequence = true;
                        break;
                    }
                    if (enumerator.Current)
                        _bitArray |= 1UL << index;
                    ++_bitLength;
                }
                if (endOFSequence == false)
                {
                    while (enumerator.MoveNext())
                    {
                        if (_additionalBitArray == null)
                            _additionalBitArray = new RandomAccessBitQueue();
                        _additionalBitArray.Enqueue(enumerator.Current ? 1UL : 0UL, 1);
                    }
                }
            }
            Normalize();
#if DEBUG
            CheckArray();
#endif
        }

        public InternalBitQueue(string bitPattern)
            : this(
                bitPattern
                  .Where(c => c != '-')
                  .Select(c =>
                    {
                        switch (c)
                        {
                            case '0':
                                return false;
                            case '1':
                                return true;
                            default:
                                throw new ArgumentException();
                        }
                    }))
        {
        }

        private InternalBitQueue(UInt64 bitArray, int bitLength, RandomAccessBitQueue additionalBitArray)
        {
            _bitArray = bitArray;
            _bitLength = bitLength;
            _additionalBitArray = additionalBitArray != null && additionalBitArray.Any() ? additionalBitArray : null;
            _totalBitLength = 0;
            Normalize();
        }

        public static InternalBitQueue FromBoolean(bool value)
        {
            return new InternalBitQueue(value ? 1UL : 0UL, 1, null);
        }

        public static InternalBitQueue FromInteger(UInt64 value, int bitCount, BitPackingDirection packingDirection)
        {
            if (bitCount < 1)
                throw new ArgumentException();
            if (bitCount > BIT_LENGTH_OF_UINT64)
                throw new ArgumentException();
            return new InternalBitQueue(value.ConvertBitOrder(bitCount, packingDirection), bitCount, null);
        }

        public void Enqueue(bool value)
        {
            if (_totalBitLength < BIT_LENGTH_OF_UINT64)
            {
                if (value)
                    _bitArray |= 1UL << _bitLength;
                ++_bitLength;
            }
            else
            {
                if (_additionalBitArray == null)
                    _additionalBitArray = new RandomAccessBitQueue();
                _additionalBitArray.Enqueue(value ? 1UL : 0UL, 1);
            }
            Normalize();
        }

        public void Enqueue(UInt64 value, int bitCount, BitPackingDirection packingDirection)
        {
            if (bitCount < 1)
                throw new ArgumentException();

            if (_bitLength >= BIT_LENGTH_OF_UINT64)
            {
                if (_additionalBitArray == null)
                    _additionalBitArray = new RandomAccessBitQueue();
                var data = value.ConvertBitOrder(bitCount, packingDirection);
                _additionalBitArray.Enqueue(data, bitCount);
            }
            else if (_bitLength + bitCount > BIT_LENGTH_OF_UINT64)
            {
                //var bitLength1 = BIT_LENGTH_OF_UINT64 -_bitLength;
                var bitLength2 = bitCount + _bitLength - BIT_LENGTH_OF_UINT64;
                var data = value.ConvertBitOrder(bitCount, packingDirection);
                var data1 = data << _bitLength;
                var data2 = data >> (BIT_LENGTH_OF_UINT64 - _bitLength);
                _bitArray |= data1;
                _bitLength = BIT_LENGTH_OF_UINT64;
                if (_additionalBitArray == null)
                    _additionalBitArray = new RandomAccessBitQueue();
                _additionalBitArray.Enqueue(data2, bitLength2);
            }
            else
            {
                _bitArray |= value.ConvertBitOrder(bitCount, packingDirection) << _bitLength;
                _bitLength += bitCount;
            }
            Normalize();
#if DEBUG
            CheckArray();
#endif
        }

        public void Enqueue(InternalBitQueue bitQueue)
        {
            if (bitQueue == null)
                throw new ArgumentNullException();

            if (_bitLength >= BIT_LENGTH_OF_UINT64)
            {
                foreach (var value in bitQueue)
                {
                    if (_additionalBitArray == null)
                        _additionalBitArray = new RandomAccessBitQueue();
                    _additionalBitArray.Enqueue(value ? 1UL : 0UL, 1);
                }
            }
            else if (_bitLength + bitQueue.Length > BIT_LENGTH_OF_UINT64)
            {
                foreach (var value in bitQueue.GetSequence(BIT_LENGTH_OF_UINT64 - _bitLength))
                {
                    if (_additionalBitArray == null)
                        _additionalBitArray = new RandomAccessBitQueue();
                    _additionalBitArray.Enqueue(value ? 1UL : 0UL, 1);
                }
                _bitArray |= bitQueue._bitArray << _bitLength;
                _bitLength = BIT_LENGTH_OF_UINT64;
            }
            else
            {
                _bitArray |= bitQueue._bitArray << _bitLength;
                _bitLength += bitQueue._bitLength;
            }
            Normalize();
#if DEBUG
            CheckArray();
#endif
        }

        public bool DequeueBoolean()
        {
            if (_bitLength < 1)
                throw new ArgumentException();

            var value = (_bitArray & 1) != 0;
            _bitArray >>= 1;
            _bitLength -= 1;
            Normalize();
            return value;
        }

        public UInt64 DequeueInteger(int bitCount, BitPackingDirection packingDirection)
        {
            if (bitCount < 1)
                throw new ArgumentException();
            if (bitCount > BIT_LENGTH_OF_UINT64)
                throw new ArgumentException();
            if (bitCount > _totalBitLength)
                throw new ArgumentException();
#if DEBUG
            if (bitCount > _bitLength)
                throw new Exception();
#endif

            var value = _bitArray.ConvertBitOrder(bitCount, packingDirection);
            _bitArray >>= bitCount;
            _bitLength -= bitCount;
            Normalize();
            return value;
        }

        public InternalBitQueue DequeueBitQueue(int bitCount)
        {
            if (bitCount < 1)
                throw new ArgumentException();
            if (bitCount > _totalBitLength)
                throw new ArgumentException();
            if (bitCount < BIT_LENGTH_OF_UINT64)
            {
                var mask = UInt64.MaxValue >> (BIT_LENGTH_OF_UINT64 - bitCount);
                var value = new InternalBitQueue(_bitArray & mask, bitCount, null);
                _bitArray >>= bitCount;
                _bitLength -= bitCount;
                Normalize();
                return value;
            }
            else if (_additionalBitArray != null && _additionalBitArray.Length > 0)
            {
                var newAdditionalBitArray = new RandomAccessBitQueue();
                var count = bitCount - _bitLength;
                while (count > 0)
                {
                    var actualCount = count.Minimum(BIT_LENGTH_OF_UINT64);
                    var data = _additionalBitArray.Dequeue(actualCount);
                    newAdditionalBitArray.Enqueue(data, actualCount);
                    count -= actualCount;
                }
                var value = new InternalBitQueue(_bitArray, _bitLength, newAdditionalBitArray);
                _bitArray = 0;
                _bitLength = 0;
                Normalize();
                return value;
            }
            else
            {
                var value = new InternalBitQueue(_bitArray, _bitLength, null);
                _bitArray = 0;
                _bitLength = 0;
                Normalize();
                return value;
            }
        }

        public bool ToBoolean()
        {
            if (_totalBitLength < 1)
                throw new InvalidOperationException();
            if (_totalBitLength > 1)
                throw new OverflowException();
            return (_bitArray & 1) != 0;
        }

        public UInt64 ToInteger(int bitCount, BitPackingDirection packingDirection)
        {
            if (_totalBitLength < 1)
                throw new InvalidOperationException();
            if (bitCount < 1)
                throw new ArgumentException();
            if (bitCount > BIT_LENGTH_OF_UINT64)
                throw new ArgumentException();
            if (_totalBitLength > bitCount)
                throw new OverflowException();
            return _bitArray.ConvertBitOrder(bitCount, packingDirection);
        }

        public string ToString(string format)
        {
            var sb = new StringBuilder();
            switch (format.ToUpperInvariant())
            {
                case "R":
                    foreach (var value in GetSequenceSource())
                        sb.Append(value ? '1' : '0');
                    break;
                case "G":
                    sb.Append("{");
                    foreach (var item in GetSequenceSource().Select((value, index) => new { value, index }))
                    {
                        if (item.index > 0 && item.index % 8 == 0)
                            sb.Append('-');
                        sb.Append(item.value ? '1' : '0');
                    }
                    sb.Append("}");
                    break;
                default:
                    throw new FormatException();
            }
            return sb.ToString();
        }

        public int Length => _totalBitLength;

        public bool this[int index] =>
            index < _bitLength
            ? (_bitArray & (1UL << index)) != 0
            : _additionalBitArray != null
                ? _additionalBitArray[index - _bitLength]
                : throw new IndexOutOfRangeException();

        public void CopyTo(bool[] destinationArray, int destinationOffset)
        {
            if (destinationArray == null)
                throw new ArgumentNullException();
            if (destinationOffset < 0)
                throw new IndexOutOfRangeException();
            if (destinationOffset > destinationArray.Length)
                throw new IndexOutOfRangeException();
            if (destinationArray.Length - destinationOffset > _totalBitLength)
                throw new IndexOutOfRangeException();
            InternalCopyTo(0, destinationArray, destinationOffset, destinationArray.Length - destinationOffset);
        }

        public void CopyTo(int sourceOffset, bool[] destinationArray, int destinationOffset, int count)
        {
            if (sourceOffset < 0)
                throw new IndexOutOfRangeException();
            if (destinationArray == null)
                throw new ArgumentNullException();
            if (destinationOffset < 0)
                throw new IndexOutOfRangeException();
            if (count < 0)
                throw new IndexOutOfRangeException();
            if (sourceOffset + count > _totalBitLength)
                throw new IndexOutOfRangeException();
            if (destinationOffset + count > destinationArray.Length)
                throw new IndexOutOfRangeException();
            InternalCopyTo(sourceOffset, destinationArray, destinationOffset, count);
        }

        public bool[] DuplicateAsWritableArray()
        {
            var buffer = new bool[_totalBitLength];
            CopyTo(buffer, 0);
            return buffer;
        }

        public IEnumerator<bool> GetEnumerator()
        {
            return GetSequenceSource().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(InternalBitQueue other)
        {
            if (other == null)
                return false;
            if (_bitArray != other._bitArray)
                return false;
            if (_bitLength != other._bitLength)
                return false;
            if (_additionalBitArray == null)
                return other._additionalBitArray == null;
            if (_additionalBitArray.Equals(_additionalBitArray) == false)
                return false;
            return true;
        }

        public InternalBitQueue Clone()
        {
            return new InternalBitQueue(_bitArray, _bitLength, _additionalBitArray);
        }

        public override string ToString()
        {
            return ToString("G");
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            return Equals((InternalBitQueue)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = _bitArray.GetHashCode() ^ _bitLength.GetHashCode();
            if (_additionalBitArray != null && _additionalBitArray.Length > 0)
                hashCode ^= _additionalBitArray.GetHashCode();
            return hashCode;
        }

        [Obsolete]
        bool[] IReadOnlyArray<bool>.ToArray() => throw new NotSupportedException();

        private IEnumerable<bool> GetSequenceSource()
        {
            var sequence =
                Enumerable.Range(0, _bitLength)
                .Select(index => (_bitArray & (1UL << index)) != 0);
            if (_additionalBitArray != null)
                sequence = sequence.Concat(_additionalBitArray);
            return sequence;
        }

        private void InternalCopyTo(int sourceOffset, bool[] destinationArray, int destinationOffset, int count)
        {
            var sourceIndex = sourceOffset;
            var destinationIndex = destinationOffset;
            var index = 0;
            while (sourceIndex < BIT_LENGTH_OF_UINT64 && index < count)
            {
                destinationArray[destinationIndex] = (1UL << sourceIndex) != 0;
                ++sourceIndex;
                ++destinationIndex;
                ++index;
            }
            if (_additionalBitArray != null && index < count)
                _additionalBitArray.CopyTo(0, destinationArray, destinationIndex, count - index);
        }

        private void Normalize()
        {
            _totalBitLength = _additionalBitArray != null ? _bitLength + _additionalBitArray.Length : _bitLength;
            if (_bitLength < BIT_LENGTH_OF_UINT64 && _additionalBitArray != null && _additionalBitArray.Length > 0)
            {
                var actualBitCount = (BIT_LENGTH_OF_UINT64 - _bitLength).Minimum(_additionalBitArray.Length);
                _bitArray |= _additionalBitArray.Dequeue(actualBitCount) << _bitLength;
                _bitLength += actualBitCount;
            }
        }

#if DEBUG
        private void CheckArray()
        {
            if (_bitLength < 0)
                throw new Exception();
            if (_bitLength > BIT_LENGTH_OF_UINT64)
                throw new Exception();
            if (_bitLength < BIT_LENGTH_OF_UINT64)
            {
                var mask = UInt64.MaxValue << _bitLength;
                if ((_bitArray & mask) != 0)
                    throw new Exception();
            }
            if (_bitLength < BIT_LENGTH_OF_UINT64 && _additionalBitArray != null && _additionalBitArray.Length > 0)
                throw new Exception();
        }
#endif

    }
}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utility
{
    class InternalBitQueue
        : IEnumerable<bool>, IEquatable<InternalBitQueue>, ICloneable<InternalBitQueue>

    {
        private class RandomAccessBitQueue
                : IEnumerable<bool>, ICloneable<RandomAccessBitQueue>, IEquatable<RandomAccessBitQueue>
        {
            //
            //          _firstBitArray                   _queue                     _LastBitArray
            //  [LSB..........................MSB]([LSB...MSB][LSB...MSB]...)[LSB..........................MSB]
            //  |<=_firstBitArrayLength=>|<============== array of UInt64 ===========>|<=_LastBitArrayLength=>|
            //  <- FIRST................................................................................LAST ->
            //

            private readonly RandomAccessQueue<UInt64> _queue;

            private UInt64 _firstBitArray;
            private Int32 _firstBitArrayLength;
            private UInt64 _lastBitArray;
            private Int32 _lastBitArrayLength;
            private Int32 _totalBitLength;

            public RandomAccessBitQueue()
                : this(0, 0, new RandomAccessQueue<UInt64>(), 0, 0)
            {
            }

            public RandomAccessBitQueue(IEnumerable<bool> sequence)
            {
                var uint64Array = Chunk(sequence).ToArray();
                if (uint64Array.Length <= 0)
                {
                    _firstBitArray = 0;
                    _firstBitArrayLength = 0;
                    _queue = new RandomAccessQueue<UInt64>();
                    _lastBitArray = 0;
                    _lastBitArrayLength = 0;
                    _totalBitLength = 0;
                }
                else if (uint64Array[^1].bitLength >= BIT_LENGTH_OF_UINT64)
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
                    _lastBitArray = uint64Array[^1].uint64Array;
                    _lastBitArrayLength = uint64Array[^1].bitLength;
                    _totalBitLength = _queue.Length * BIT_LENGTH_OF_UINT64 + _lastBitArrayLength;
                }
                Normalize();
#if DEBUG
                Check();
#endif
            }

            private RandomAccessBitQueue(UInt64 firstBitArray, Int32 firstBitArrayLength, RandomAccessQueue<UInt64> queue, UInt64 lastBitArray, Int32 lastBitArrayLength)
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

            public void Enqueue(UInt64 bitArray, Int32 bitCount)
            {
                while (bitCount > 0)
                {
                    var length = InternalEnqueue(bitArray, bitCount);
                    bitCount -= length;
                    bitArray >>= length;
                }
            }

            public UInt64 Dequeue(Int32 bitCount)
            {
                var result = 0UL;
                var bitIndex = 0;
                while (bitIndex < bitCount)
                {
                    var (length, value) = InternalDequeue(bitCount - bitIndex);
                    result |= value << bitIndex;
                    bitIndex += length;
                }
                return result;
            }

            public Int32 Length => _totalBitLength;

            public bool this[Int32 index]
            {
                get
                {
                    if (index < 0)
                        throw new ArgumentOutOfRangeException(nameof(index));
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
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public IEnumerator<bool> GetEnumerator()
            {
                {
                    var firstBitArray = _firstBitArray;
                    var firstBitArrayLength = _firstBitArrayLength;
                    while (firstBitArrayLength-- > 0)
                    {
                        yield return (firstBitArray & 1U) != 0;
                        firstBitArray >>= 1;
                    }
                }
                foreach (var queueElement in _queue)
                {
                    yield return (queueElement & (1UL << 0)) != 0;
                    yield return (queueElement & (1UL << 1)) != 0;
                    yield return (queueElement & (1UL << 2)) != 0;
                    yield return (queueElement & (1UL << 3)) != 0;
                    yield return (queueElement & (1UL << 4)) != 0;
                    yield return (queueElement & (1UL << 5)) != 0;
                    yield return (queueElement & (1UL << 6)) != 0;
                    yield return (queueElement & (1UL << 7)) != 0;
                    yield return (queueElement & (1UL << 8)) != 0;
                    yield return (queueElement & (1UL << 9)) != 0;
                    yield return (queueElement & (1UL << 10)) != 0;
                    yield return (queueElement & (1UL << 11)) != 0;
                    yield return (queueElement & (1UL << 12)) != 0;
                    yield return (queueElement & (1UL << 13)) != 0;
                    yield return (queueElement & (1UL << 14)) != 0;
                    yield return (queueElement & (1UL << 15)) != 0;
                    yield return (queueElement & (1UL << 16)) != 0;
                    yield return (queueElement & (1UL << 17)) != 0;
                    yield return (queueElement & (1UL << 18)) != 0;
                    yield return (queueElement & (1UL << 19)) != 0;
                    yield return (queueElement & (1UL << 20)) != 0;
                    yield return (queueElement & (1UL << 21)) != 0;
                    yield return (queueElement & (1UL << 22)) != 0;
                    yield return (queueElement & (1UL << 23)) != 0;
                    yield return (queueElement & (1UL << 24)) != 0;
                    yield return (queueElement & (1UL << 25)) != 0;
                    yield return (queueElement & (1UL << 26)) != 0;
                    yield return (queueElement & (1UL << 27)) != 0;
                    yield return (queueElement & (1UL << 28)) != 0;
                    yield return (queueElement & (1UL << 29)) != 0;
                    yield return (queueElement & (1UL << 30)) != 0;
                    yield return (queueElement & (1UL << 31)) != 0;
                    yield return (queueElement & (1UL << 32)) != 0;
                    yield return (queueElement & (1UL << 33)) != 0;
                    yield return (queueElement & (1UL << 34)) != 0;
                    yield return (queueElement & (1UL << 35)) != 0;
                    yield return (queueElement & (1UL << 36)) != 0;
                    yield return (queueElement & (1UL << 37)) != 0;
                    yield return (queueElement & (1UL << 38)) != 0;
                    yield return (queueElement & (1UL << 39)) != 0;
                    yield return (queueElement & (1UL << 40)) != 0;
                    yield return (queueElement & (1UL << 41)) != 0;
                    yield return (queueElement & (1UL << 42)) != 0;
                    yield return (queueElement & (1UL << 43)) != 0;
                    yield return (queueElement & (1UL << 44)) != 0;
                    yield return (queueElement & (1UL << 45)) != 0;
                    yield return (queueElement & (1UL << 46)) != 0;
                    yield return (queueElement & (1UL << 47)) != 0;
                    yield return (queueElement & (1UL << 48)) != 0;
                    yield return (queueElement & (1UL << 49)) != 0;
                    yield return (queueElement & (1UL << 50)) != 0;
                    yield return (queueElement & (1UL << 51)) != 0;
                    yield return (queueElement & (1UL << 52)) != 0;
                    yield return (queueElement & (1UL << 53)) != 0;
                    yield return (queueElement & (1UL << 54)) != 0;
                    yield return (queueElement & (1UL << 55)) != 0;
                    yield return (queueElement & (1UL << 56)) != 0;
                    yield return (queueElement & (1UL << 57)) != 0;
                    yield return (queueElement & (1UL << 58)) != 0;
                    yield return (queueElement & (1UL << 59)) != 0;
                    yield return (queueElement & (1UL << 60)) != 0;
                    yield return (queueElement & (1UL << 61)) != 0;
                    yield return (queueElement & (1UL << 62)) != 0;
                    yield return (queueElement & (1UL << 63)) != 0;
                }
                {
                    var lastBitArray = _lastBitArray;
                    var lastBitArrayLength = _lastBitArrayLength;
                    while (lastBitArrayLength-- > 0)
                    {
                        yield return (lastBitArray & 1U) != 0;
                        lastBitArray >>= 1;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public RandomAccessBitQueue Clone() => new(_firstBitArray, _firstBitArrayLength, _queue.Clone(), _lastBitArray, _lastBitArrayLength);
            public bool Equals(RandomAccessBitQueue? other) => other is not null && this.SequenceEqual(other);
            public override bool Equals(object? obj) => obj != null && GetType() == obj.GetType() && Equals((RandomAccessBitQueue)obj);

            public override Int32 GetHashCode()
            {
                var hashCode = 0UL;
                foreach (var value in this)
                {
                    hashCode = (hashCode << 1) | (hashCode >> 63);
                    if (value)
                        hashCode ^= 1;
                }
                return hashCode.GetHashCode();
            }

            private static IEnumerable<(UInt64 uint64Array, Int32 bitLength)> Chunk(IEnumerable<bool> source)
            {
                var value = 0UL;
                var index = 0;
                foreach (var element in source)
                {
                    value <<= 1;
                    if (element)
                        value |= 1;
                    if (++index >= BIT_LENGTH_OF_UINT64)
                    {
                        yield return (value, index);
                        index = 0;
                    }
                }
                yield return (value, index);
            }

            private Int32 InternalEnqueue(UInt64 bitArray, Int32 bitCount)
            {
                if (!bitCount.IsBetween(1, BIT_LENGTH_OF_UINT64))
                    throw new ArgumentOutOfRangeException(nameof(bitCount));
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

            private (Int32, UInt64) InternalDequeue(Int32 bitCount)
            {
                if (!bitCount.IsBetween(1, BIT_LENGTH_OF_UINT64))
                    throw new ArgumentOutOfRangeException(nameof(bitCount));
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
                    var resultBitArray = _firstBitArray;
                    _firstBitArray = 0;
                    _firstBitArrayLength = 0;
                    Normalize();
#if DEBUG
                    Check();
#endif
                    return (actualBitCount, resultBitArray);
                }
                else
                {
                    var resultBitArray = _firstBitArray & (UInt64.MaxValue >> (BIT_LENGTH_OF_UINT64 - actualBitCount));
                    _firstBitArray >>= actualBitCount;
                    _firstBitArrayLength -= actualBitCount;
                    Normalize();
#if DEBUG
                    Check();
#endif
                    return (actualBitCount, resultBitArray);
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
                if (!_firstBitArrayLength.InRange(0, BIT_LENGTH_OF_UINT64))
                    throw new Exception();
                if (!_lastBitArrayLength.InRange(0, BIT_LENGTH_OF_UINT64))
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

        public const Int32 BIT_LENGTH_OF_BYTE = sizeof(Byte) << 3;
        public const Int32 BIT_LENGTH_OF_UINT16 = sizeof(UInt16) << 3;
        public const Int32 BIT_LENGTH_OF_UINT32 = sizeof(UInt32) << 3;
        public const Int32 BIT_LENGTH_OF_UINT64 = sizeof(UInt64) << 3;
        public const Int32 RecommendedMaxCount = BIT_LENGTH_OF_UINT64;

        //
        // internal bit packing of _bitArray:
        //
        //              _bitArray
        //             [LSB.........MSB]
        //  bit index:  0............63
        //
        private UInt64 _bitArray;
        private Int32 _bitLength;
        private RandomAccessBitQueue? _additionalBitArray;
        private Int32 _totalBitLength;

        public InternalBitQueue()
            : this(0, 0, null)
        {
        }

        public InternalBitQueue(ReadOnlySpan<bool> bitPattern)
        {
            _bitLength = 0;
            _bitArray = 0;
            _additionalBitArray = null;
            _totalBitLength = 0;
            var index = 0;
            while (index < BIT_LENGTH_OF_UINT64)
            {
                if (index >= bitPattern.Length)
                    break;
                if (bitPattern[index])
                    _bitArray |= 1UL << index;
                ++_bitLength;
                ++index;
            }
            while (index < bitPattern.Length)
            {
                if (_additionalBitArray is null)
                    _additionalBitArray = new RandomAccessBitQueue();
                _additionalBitArray.Enqueue(bitPattern[index] ? 1UL : 0UL, 1);
                ++index;
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
                        return
                            c switch
                            {
                                '0' => false,
                                '1' => true,
                                _ => throw new ArgumentException("Contained illegal character", nameof(bitPattern)),
                            };
                    })
                  .ToArray()
                  .AsReadOnlySpan())
        {
        }

        private InternalBitQueue(UInt64 bitArray, Int32 bitLength, RandomAccessBitQueue? additionalBitArray)
        {
            _bitArray = bitArray;
            _bitLength = bitLength;
            _additionalBitArray = additionalBitArray is not null && additionalBitArray.Any() ? additionalBitArray : null;
            _totalBitLength = 0;
            Normalize();
        }

        public static InternalBitQueue FromBoolean(bool value)
        {
            return new InternalBitQueue(value ? 1UL : 0UL, 1, null);
        }

        public static InternalBitQueue FromInteger(UInt64 value, Int32 bitCount, BitPackingDirection bitPackingDirection)
        {
            if (bitCount < 1)
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            if (bitCount > BIT_LENGTH_OF_UINT64)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            return new InternalBitQueue(value.ConvertBitOrder(bitCount, bitPackingDirection), bitCount, null);
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
                if (_additionalBitArray is null)
                    _additionalBitArray = new RandomAccessBitQueue();
                _additionalBitArray.Enqueue(value ? 1UL : 0UL, 1);
            }
            Normalize();
        }

        public void Enqueue(UInt64 value, Int32 bitCount, BitPackingDirection bitPackingDirection)
        {
            if (bitCount < 1)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (_bitLength >= BIT_LENGTH_OF_UINT64)
            {
                if (_additionalBitArray is null)
                    _additionalBitArray = new RandomAccessBitQueue();
                var data = value.ConvertBitOrder(bitCount, bitPackingDirection);
                _additionalBitArray.Enqueue(data, bitCount);
            }
            else if (_bitLength + bitCount > BIT_LENGTH_OF_UINT64)
            {
                //var bitLength1 = BIT_LENGTH_OF_UINT64 -_bitLength;
                var bitLength2 = bitCount + _bitLength - BIT_LENGTH_OF_UINT64;
                var data = value.ConvertBitOrder(bitCount, bitPackingDirection);
                var data1 = data << _bitLength;
                var data2 = data >> (BIT_LENGTH_OF_UINT64 - _bitLength);
                _bitArray |= data1;
                _bitLength = BIT_LENGTH_OF_UINT64;
                if (_additionalBitArray is null)
                    _additionalBitArray = new RandomAccessBitQueue();
                _additionalBitArray.Enqueue(data2, bitLength2);
            }
            else
            {
                _bitArray |= value.ConvertBitOrder(bitCount, bitPackingDirection) << _bitLength;
                _bitLength += bitCount;
            }
            Normalize();
#if DEBUG
            CheckArray();
#endif
        }

        public void Enqueue(InternalBitQueue bitQueue)
        {
            if (bitQueue is null)
                throw new ArgumentNullException(nameof(bitQueue));

            if (_bitLength >= BIT_LENGTH_OF_UINT64)
            {
                foreach (var value in bitQueue)
                {
                    if (_additionalBitArray is null)
                        _additionalBitArray = new RandomAccessBitQueue();
                    _additionalBitArray.Enqueue(value ? 1UL : 0UL, 1);
                }
            }
            else if (_bitLength + bitQueue.Length > BIT_LENGTH_OF_UINT64)
            {
                foreach (var value in bitQueue.Skip(BIT_LENGTH_OF_UINT64 - _bitLength))
                {
                    if (_additionalBitArray is null)
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
                throw new InvalidOperationException();

            var value = (_bitArray & 1) != 0;
            _bitArray >>= 1;
            _bitLength -= 1;
            Normalize();
            return value;
        }

        public UInt64 DequeueInteger(Int32 bitCount, BitPackingDirection bitPackingDirection)
        {
            if (bitCount < 1)
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            if (bitCount > BIT_LENGTH_OF_UINT64)
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            if (bitCount > _totalBitLength)
                throw new ArgumentOutOfRangeException(nameof(bitCount));
#if DEBUG
            if (bitCount > _bitLength)
                throw new Exception();
#endif

            var value = _bitArray.ConvertBitOrder(bitCount, bitPackingDirection);
            _bitArray >>= bitCount;
            _bitLength -= bitCount;
            Normalize();
            return value;
        }

        public InternalBitQueue DequeueBitQueue(Int32 bitCount)
        {
            if (bitCount < 1)
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            if (bitCount > _totalBitLength)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (bitCount < BIT_LENGTH_OF_UINT64)
            {
                var mask = UInt64.MaxValue >> (BIT_LENGTH_OF_UINT64 - bitCount);
                var value = new InternalBitQueue(_bitArray & mask, bitCount, null);
                _bitArray >>= bitCount;
                _bitLength -= bitCount;
                Normalize();
                return value;
            }
            else if (_additionalBitArray is not null && _additionalBitArray.Length > 0)
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

        public UInt64 ToInteger(Int32 bitCount, BitPackingDirection bitPackingDirection)
        {
            if (_totalBitLength < 1)
                throw new InvalidOperationException();
            if (bitCount < 1)
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            if (bitCount > BIT_LENGTH_OF_UINT64)
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            if (_totalBitLength > bitCount)
                throw new OverflowException();

            return _bitArray.ConvertBitOrder(bitCount, bitPackingDirection);
        }

        public void Clear()
        {
            _bitArray = 0;
            _bitLength = 0;
            _additionalBitArray = null;
            _totalBitLength = 0;
            Normalize();
        }

        public string ToString(string? format)
        {
            var sb = new StringBuilder();
            switch ((format ?? "G").ToUpperInvariant())
            {
                case "R":
                    foreach (var value in GetSequenceSource())
                        sb.Append(value ? '1' : '0');
                    break;
                case "G":
                    sb.Append('{');
                    foreach (var item in GetSequenceSource().Select((value, index) => new { value, index }))
                    {
                        if (item.index > 0 && item.index % 8 == 0)
                            sb.Append('-');
                        sb.Append(item.value ? '1' : '0');
                    }
                    sb.Append('}');
                    break;
                default:
                    throw new FormatException();
            }
            return sb.ToString();
        }

        public Int32 Length => _totalBitLength;

        public bool this[Int32 index] =>
            index < _bitLength
            ? (_bitArray & (1UL << index)) != 0
            : _additionalBitArray is not null
                ? _additionalBitArray[index - _bitLength]
                : throw new ArgumentOutOfRangeException(nameof(index));

        public bool this[UInt32 index] => this[checked((Int32)index)];

        public IEnumerator<bool> GetEnumerator() => GetSequenceSource().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Equals(InternalBitQueue? other)
        {
            if (other is null)
                return false;
            if (_bitArray != other._bitArray)
                return false;
            if (_bitLength != other._bitLength)
                return false;
            if (_additionalBitArray is null)
                return other._additionalBitArray is null;
            if (!_additionalBitArray.Equals(_additionalBitArray))
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

        public override bool Equals(object? obj)
        {
            if (obj is null || GetType() != obj.GetType())
                return false;
            return Equals((InternalBitQueue)obj);
        }

        public override Int32 GetHashCode()
        {
            var hashCode = _bitArray.GetHashCode() ^ _bitLength.GetHashCode();
            if (_additionalBitArray is not null && _additionalBitArray.Length > 0)
                hashCode ^= _additionalBitArray.GetHashCode();
            return hashCode;
        }

        private IEnumerable<bool> GetSequenceSource()
        {
            var bitArray = _bitArray;
            var bitLength = _bitLength;
            while (bitLength-- > 0)
            {
                yield return (bitArray & 1U) != 0;
                bitArray >>= 1;
            }
            if (_additionalBitArray is not null)
            {
                foreach (var bit in _additionalBitArray)
                    yield return bit;
            }
        }

        private void Normalize()
        {
            _totalBitLength = _additionalBitArray is not null ? _bitLength + _additionalBitArray.Length : _bitLength;
            if (_bitLength < BIT_LENGTH_OF_UINT64 && _additionalBitArray is not null && _additionalBitArray.Length > 0)
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
            if (_bitLength < BIT_LENGTH_OF_UINT64 && _additionalBitArray is not null && _additionalBitArray.Length > 0)
                throw new Exception();
        }
#endif

    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utility
{
    /// <summary>
    /// <see cref="bool"/>で表現されたビットを要素とする配列のクラスです。
    /// 配列内のランダムアクセスや、ビット集合を整数とみなした相互変換もサポートされています。
    /// </summary>
    /// <remarks>
    /// このクラスは比較的少ないビット数の集合を扱う際にパフォーマンスが向上するように設計されています。
    /// <see cref="RecommendedMaxLength"/>を超える大きさのビットを保持するとパフォーマンスが低下することがありますので注意してください。
    /// </remarks>
    public class TinyBitArray
        : IReadOnlyArray<bool>, IEquatable<TinyBitArray>, ICloneable<TinyBitArray>, IInternalBitArray
    {
        private const int _BIT_LENGTH_OF_BYTE = InternalBitQueue.BIT_LENGTH_OF_BYTE;
        private const int _BIT_LENGTH_OF_UINT16 = InternalBitQueue.BIT_LENGTH_OF_UINT16;
        private const int _BIT_LENGTH_OF_UINT32 = InternalBitQueue.BIT_LENGTH_OF_UINT32;
        private const int _BIT_LENGTH_OF_UINT64 = InternalBitQueue.BIT_LENGTH_OF_UINT64;

        public const int RecommendedMaxLength = _BIT_LENGTH_OF_UINT64;

        private InternalBitQueue _bitArray;

        public TinyBitArray()
            : this(new InternalBitQueue())
        {
        }

        public TinyBitArray(IReadOnlyArray<bool> bitPattern)
            : this(new InternalBitQueue(bitPattern))
        {
        }

        public TinyBitArray(string bitPattern)
            : this(new InternalBitQueue(bitPattern))
        {
        }

        internal TinyBitArray(InternalBitQueue bitAarray)
        {
            _bitArray = bitAarray;
        }

        public static TinyBitArray FromBoolean(Boolean value)
        {
            return new TinyBitArray(InternalBitQueue.FromBoolean(value));
        }

        public static TinyBitArray FromByte(Byte value, int bitCount = _BIT_LENGTH_OF_BYTE, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, _BIT_LENGTH_OF_BYTE) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", _BIT_LENGTH_OF_BYTE), "bitCount");
            return new TinyBitArray(InternalBitQueue.FromInteger(value, bitCount, packingDirection));
        }

        public static TinyBitArray FromUInt16(UInt16 value, int bitCount = _BIT_LENGTH_OF_UINT16, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, _BIT_LENGTH_OF_UINT16) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", _BIT_LENGTH_OF_UINT16), "bitCount");
            return new TinyBitArray(InternalBitQueue.FromInteger(value, bitCount, packingDirection));
        }

        public static TinyBitArray FromUInt32(UInt32 value, int bitCount = _BIT_LENGTH_OF_UINT32, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, _BIT_LENGTH_OF_UINT32) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", _BIT_LENGTH_OF_UINT32), "bitCount");
            return new TinyBitArray(InternalBitQueue.FromInteger(value, bitCount, packingDirection));
        }

        public static TinyBitArray FromUInt64(UInt64 value, int bitCount = _BIT_LENGTH_OF_UINT64, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, _BIT_LENGTH_OF_UINT64) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", _BIT_LENGTH_OF_UINT64), "bitCount");
            return new TinyBitArray(InternalBitQueue.FromInteger(value, bitCount, packingDirection));
        }

        public Boolean ToBoolean()
        {
            if (_bitArray.Length < 1)
                throw new InvalidOperationException();
            if (_bitArray.Length > 1)
                throw new OverflowException();
            return _bitArray.ToBoolean();
        }

        public Byte ToByte(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (_bitArray.Length < 1)
                throw new InvalidOperationException();
            if (_bitArray.Length > _BIT_LENGTH_OF_BYTE)
                throw new OverflowException();
            return (Byte)_bitArray.ToInteger(_BIT_LENGTH_OF_BYTE.Minimum(_bitArray.Length), packingDirection);
        }

        public UInt16 ToUInt16(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (_bitArray.Length < 1)
                throw new InvalidOperationException();
            if (_bitArray.Length > _BIT_LENGTH_OF_UINT16)
                throw new OverflowException();
            return (UInt16)_bitArray.ToInteger(_BIT_LENGTH_OF_UINT16.Minimum(_bitArray.Length), packingDirection);
        }

        public UInt32 ToUInt32(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (_bitArray.Length < 1)
                throw new InvalidOperationException();
            if (_bitArray.Length > _BIT_LENGTH_OF_UINT32)
                throw new OverflowException();
            return (UInt32)_bitArray.ToInteger(_BIT_LENGTH_OF_UINT32.Minimum(_bitArray.Length), packingDirection);
        }

        public UInt64 ToUInt64(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (_bitArray.Length < 1)
                throw new InvalidOperationException();
            if (_bitArray.Length > _BIT_LENGTH_OF_UINT64)
                throw new OverflowException();
            return (UInt64)_bitArray.ToInteger(_BIT_LENGTH_OF_UINT64.Minimum(_bitArray.Length), packingDirection);
        }

        public TinyBitArray Concat(Boolean value)
        {
            var bitArray = _bitArray.Clone();
            bitArray.Enqueue(value);
            return new TinyBitArray(bitArray);
        }

        public TinyBitArray Concat(Byte value, int bitCount = _BIT_LENGTH_OF_BYTE, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount < 1)
                throw new ArgumentException();
            if (bitCount > _BIT_LENGTH_OF_BYTE)
                throw new ArgumentException();
            var bitArray = _bitArray.Clone();
            bitArray.Enqueue(value, bitCount, packingDirection);
            return new TinyBitArray(bitArray);
        }

        public TinyBitArray Concat(UInt16 value, int bitCount = _BIT_LENGTH_OF_UINT16, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount < 1)
                throw new ArgumentException();
            if (bitCount > _BIT_LENGTH_OF_UINT16)
                throw new ArgumentException();
            return ConcatInterger(value, bitCount, packingDirection);
        }

        public TinyBitArray Concat(UInt32 value, int bitCount = _BIT_LENGTH_OF_UINT32, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount < 1)
                throw new ArgumentException();
            if (bitCount > _BIT_LENGTH_OF_UINT32)
                throw new ArgumentException();
            return ConcatInterger(value, bitCount, packingDirection);
        }

        public TinyBitArray Concat(UInt64 value, int bitCount = _BIT_LENGTH_OF_UINT64, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount < 1)
                throw new ArgumentException();
            if (bitCount > _BIT_LENGTH_OF_UINT64)
                throw new ArgumentException();
            return ConcatInterger(value, bitCount, packingDirection);
        }

        public TinyBitArray Concat(TinyBitArray other)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            var newBitArray = _bitArray.Clone();
            newBitArray.Enqueue(other._bitArray);
            return new TinyBitArray(newBitArray);
        }

        public TinyBitArray Divide(int count, out TinyBitArray other)
        {
            if (count < 0)
                throw new ArgumentException();
            else if (count == 0)
            {
                other = Clone();
                return new TinyBitArray();
            }
            else if (count < _bitArray.Length)
            {
                var newBitArray = _bitArray.Clone();
                var result = newBitArray.DequeueBitQueue(count);
                other = new TinyBitArray(newBitArray);
                return new TinyBitArray(result);
            }
            else if (count == _bitArray.Length)
            {
                other = new TinyBitArray(); ;
                return Clone();
            }
            else
                throw new ArgumentException();

        }

        public string ToString(string format) => _bitArray.ToString(format);

        public static TinyBitArray operator +(TinyBitArray x, TinyBitArray y)
        {
            if (x == null)
                throw new ArgumentNullException("x");
            if (y == null)
                throw new ArgumentNullException("y");
            return x.Concat(y);
        }

        public bool this[int index] => _bitArray[index];
        public int Length => _bitArray.Length;
        public bool[] DuplicateAsWritableArray() => _bitArray.ToArray();

        public void CopyTo(bool[] destinationArray, int destinationOffset)
        {
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (destinationOffset < 0)
                throw new IndexOutOfRangeException();
            if (destinationOffset > destinationArray.Length)
                throw new IndexOutOfRangeException();
            if (destinationArray.Length - destinationOffset > _bitArray.Length)
                throw new IndexOutOfRangeException();
            _bitArray.CopyTo(destinationArray, destinationOffset);
        }

        public void CopyTo(int sourceOffset, bool[] destinationArray, int destinationOffset, int count)
        {
            if (sourceOffset < 0)
                throw new IndexOutOfRangeException();
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (destinationOffset < 0)
                throw new IndexOutOfRangeException();
            if (count < 0)
                throw new IndexOutOfRangeException();
            if (sourceOffset + count > _bitArray.Length)
                throw new IndexOutOfRangeException();
            if (destinationOffset + count > destinationArray.Length)
                throw new IndexOutOfRangeException();
            _bitArray.CopyTo(sourceOffset, destinationArray, destinationOffset, count);
        }

        public TinyBitArray Clone() => new TinyBitArray(_bitArray.Clone());
        public bool Equals(TinyBitArray other) => other != null && _bitArray.Equals(other._bitArray);
        public override bool Equals(object obj) => obj != null && GetType() == obj.GetType() && Equals((TinyBitArray)obj);
        public override int GetHashCode() => _bitArray.GetHashCode();
        public override string ToString() => ToString("G");
        public IEnumerator<bool> GetEnumerator() => _bitArray.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        InternalBitQueue IInternalBitArray.BitArray => _bitArray;

        [Obsolete]
        bool[] IReadOnlyArray<bool>.ToArray() => throw new NotSupportedException();

        private TinyBitArray ConcatInterger(UInt64 value, int bitCount, BitPackingDirection packingDirection)
        {
#if DEBUG
            if (bitCount < 1)
                throw new Exception();
            if (bitCount > _BIT_LENGTH_OF_UINT64)
                throw new Exception();
#endif
            var bitArray = _bitArray.Clone();
            bitArray.Enqueue(value, bitCount, packingDirection);
            return new TinyBitArray(bitArray);
        }
    }
}

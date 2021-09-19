using System;
using System.Collections;
using System.Collections.Generic;

namespace Utility
{
    public class ReadOnlySerializedBitArray
        : IReadOnlyArray<bool>, IEquatable<ReadOnlySerializedBitArray>, ICloneable<ReadOnlySerializedBitArray>
    {
        private SerializedBitArray _internalArray;

        public ReadOnlySerializedBitArray()
            : this(new SerializedBitArray())
        {
        }

        internal ReadOnlySerializedBitArray(SerializedBitArray internalArray)
        {
            _internalArray = internalArray;
        }

        public static ReadOnlySerializedBitArray Empty { get; }

        public static ReadOnlySerializedBitArray FromBooleanSequence(IEnumerable<bool> booleanSequence)
        {
            return new ReadOnlySerializedBitArray(SerializedBitArray.FromBooleanSequence(booleanSequence));
        }

        public static ReadOnlySerializedBitArray FromBooleanSequence(bool[] booleanSequence)
        {
            return new ReadOnlySerializedBitArray(SerializedBitArray.FromBooleanSequence(booleanSequence));
        }

        public static ReadOnlySerializedBitArray FromBooleanSequence(IReadOnlyArray<bool> booleanSequence)
        {
            return new ReadOnlySerializedBitArray(SerializedBitArray.FromBooleanSequence(booleanSequence));
        }

        public static ReadOnlySerializedBitArray FromByteSequence(IEnumerable<byte> byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(SerializedBitArray.FromByteSequence(byteSequence, packingDirection));
        }

        public static ReadOnlySerializedBitArray FromByteSequence(byte[] byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(SerializedBitArray.FromByteSequence(byteSequence, packingDirection));
        }

        public static ReadOnlySerializedBitArray FromByteSequence(IReadOnlyArray<byte> byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(SerializedBitArray.FromByteSequence(byteSequence, packingDirection));
        }

        public static ReadOnlySerializedBitArray FromByte(Byte value, int bitCount = SerializedBitArray.BIT_LENGTH_OF_BYTE, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(SerializedBitArray.FromByte(value, bitCount, packingDirection));
        }

        public static ReadOnlySerializedBitArray FromUInt16(UInt16 value, int bitCount = SerializedBitArray.BIT_LENGTH_OF_UINT16, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(SerializedBitArray.FromUInt16(value, bitCount, packingDirection));
        }

        public static ReadOnlySerializedBitArray FromUInt32(UInt32 value, int bitCount = SerializedBitArray.BIT_LENGTH_OF_UINT32, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(SerializedBitArray.FromUInt32(value, bitCount, packingDirection));
        }

        public static ReadOnlySerializedBitArray FromUInt64(UInt64 value, int bitCount = SerializedBitArray.BIT_LENGTH_OF_UINT64, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(SerializedBitArray.FromUInt64(value, bitCount, packingDirection));
        }

        public static ReadOnlySerializedBitArray Parse(string s)
        {
            return new ReadOnlySerializedBitArray(SerializedBitArray.Parse(s));
        }

        public IReadOnlyArray<Byte> ToByteArray(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return _internalArray.ToByteArray(packingDirection);
        }

        public Byte ToByte(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return _internalArray.ToByte(packingDirection);
        }

        public UInt16 ToUInt16(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return _internalArray.ToUInt16(packingDirection);
        }

        public UInt32 ToUInt32(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return _internalArray.ToUInt32(packingDirection);
        }

        public UInt64 ToUInt64(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return _internalArray.ToUInt64(packingDirection);
        }

        public ReadOnlySerializedBitArray Concat(IEnumerable<bool> booleanSequence)
        {
            return new ReadOnlySerializedBitArray(_internalArray.Concat(booleanSequence));
        }

        public ReadOnlySerializedBitArray Concat(bool[] booleanSequence)
        {
            return new ReadOnlySerializedBitArray(_internalArray.Concat(booleanSequence));
        }

        public ReadOnlySerializedBitArray Concat(IReadOnlyArray<bool> booleanSequence)
        {
            return new ReadOnlySerializedBitArray(_internalArray.Concat(booleanSequence));
        }

        public ReadOnlySerializedBitArray Concat(IEnumerable<byte> byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(_internalArray.Concat(byteSequence, packingDirection));
        }

        public ReadOnlySerializedBitArray Concat(byte[] byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(_internalArray.Concat(byteSequence, packingDirection));
        }

        public ReadOnlySerializedBitArray Concat(IReadOnlyArray<Byte> byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(_internalArray.Concat(byteSequence, packingDirection));
        }

        public ReadOnlySerializedBitArray Concat(Byte value, int bitCount = SerializedBitArray.BIT_LENGTH_OF_BYTE, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(_internalArray.Concat(value, bitCount, packingDirection));
        }

        public ReadOnlySerializedBitArray Concat(UInt16 value, int bitCount = SerializedBitArray.BIT_LENGTH_OF_UINT16, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(_internalArray.Concat(value, bitCount, packingDirection));
        }

        public ReadOnlySerializedBitArray Concat(UInt32 value, int bitCount = SerializedBitArray.BIT_LENGTH_OF_UINT32, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(_internalArray.Concat(value, bitCount, packingDirection));
        }

        public ReadOnlySerializedBitArray Concat(UInt64 value, int bitCount = SerializedBitArray.BIT_LENGTH_OF_UINT64, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ReadOnlySerializedBitArray(_internalArray.Concat(value, bitCount, packingDirection));
        }

        public ReadOnlySerializedBitArray Concat(ReadOnlySerializedBitArray bitArray)
        {
            return new ReadOnlySerializedBitArray(_internalArray.Concat(bitArray._internalArray));
        }

        public ReadOnlySerializedBitArray Divide(int count, out ReadOnlySerializedBitArray remaining)
        {
            SerializedBitArray remainingInternalValue;
            var value = _internalArray.Divide(count, out remainingInternalValue);
            remaining = new ReadOnlySerializedBitArray(remainingInternalValue);
            return new ReadOnlySerializedBitArray(value);
        }

        public SerializedBitArray ToBitArray()
        {
            return _internalArray.Clone();
        }

        public string ToString(string format)
        {
            return _internalArray.ToString(format);
        }

        public bool this[int index]
        {
            get
            {
                return _internalArray[index];
            }
        }

        public int Length => _internalArray.Length;

        public bool[] ToArray()
        {
            return _internalArray.ToArray();
        }

        public void CopyTo(bool[] destinationArray, int destinationOffset)
        {
            _internalArray.CopyTo(destinationArray, destinationOffset);
        }

        public void CopyTo(int sourceIndex, bool[] destinationArray, int destinationOffset, int count)
        {
            _internalArray.CopyTo(sourceIndex, destinationArray, destinationOffset, count);
        }

        public IEnumerator<bool> GetEnumerator()
        {
            return _internalArray.GetEnumerator();
        }

        public ReadOnlySerializedBitArray Clone()
        {
            return new ReadOnlySerializedBitArray(_internalArray.Clone());
        }

        public bool Equals(ReadOnlySerializedBitArray other)
        {
            if (other == null)
                return false;
            if (!_internalArray.Equals(other._internalArray))
                return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            return Equals((ReadOnlySerializedBitArray)obj);
        }

        public override int GetHashCode()
        {
            return _internalArray.GetHashCode();
        }

        public override string ToString()
        {
            return _internalArray.ToString();
        }

        internal void AppendTo(SerializedBitArray bitArray)
        {
            bitArray.Append(_internalArray);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

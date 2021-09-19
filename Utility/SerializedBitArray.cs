using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utility
{
    /// <summary>
    /// ビットの集合を表すクラスです。
    /// このクラスはビットを表す<see cref="bool"/>値の配列として扱うことができます。
    /// </summary>
    public class SerializedBitArray
        : IEnumerable<bool>, IEquatable<SerializedBitArray>, ICloneable<SerializedBitArray>
    {
        private class Enumerator
            : IEnumerator<bool>
        {
            private bool _isDisposed;
            private UInt64[] _array;
            private int _bitLength;
            private int _bitIndex;

            public Enumerator(UInt64[] array, int bitLength)
            {
                _isDisposed = false;
                _array = array;
                _bitLength = bitLength;
                _bitIndex = -1;
            }

            public bool Current
            {
                get
                {
                    if (_isDisposed)
                        throw new ObjectDisposedException(GetType().FullName);
                    if (_bitIndex < 0)
                        throw new InvalidOperationException();
                    if (_bitIndex >= _bitLength)
                        throw new InvalidOperationException();
                    return GetValue(_array, _bitIndex);
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                ++_bitIndex;
                return _bitIndex < _bitLength;
            }

            public void Reset()
            {
                _bitIndex = 0;
            }

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

            public void Dispose()
            {
                // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        internal const int BIT_LENGTH_OF_BYTE = sizeof(Byte) << 3;
        internal const int BIT_LENGTH_OF_UINT16 = sizeof(UInt16) << 3;
        internal const int BIT_LENGTH_OF_UINT32 = sizeof(UInt32) << 3;
        internal const int BIT_LENGTH_OF_UINT64 = sizeof(UInt64) << 3;

        //
        // internal bit packing of _uint64Array:
        //
        //              _uint64Array[0]  _uint64Array[1]  _uint64Array[2]  ...
        //             [LS..........MSB][LSB.........MSB][LSB.........MSB][LSB...
        //  bit index:  0............63  64..........127  128.........191  192...
        //
        private UInt64[] _uint64Array;
        private int _bitLength;

        public SerializedBitArray()
            : this(new UInt64[0], 0)
        {
        }

        private SerializedBitArray(UInt64[] uint64Array, int bitLength)
        {
            _uint64Array = uint64Array;
            _bitLength = bitLength;
#if DEBUG
            CheckArray();
#endif
        }

        public static SerializedBitArray FromBooleanSequence(IEnumerable<bool> booleanSequence)
        {
            return FromBooleanSequence(booleanSequence?.ToArray());
        }

        public static SerializedBitArray FromBooleanSequence(bool[] booleanSequence)
        {
            return FromBooleanSequence(booleanSequence?.AsReadOnly());
        }

        public static SerializedBitArray FromBooleanSequence(IReadOnlyArray<bool> booleanSequence)
        {
            if (booleanSequence == null)
                throw new ArgumentNullException("booleanSequence");

            var sourceBitLength = booleanSequence.Length;
            var destinationUInt64Array = new UInt64[(sourceBitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64];
#if DEBUG
            if (destinationUInt64Array.Any(item => item != 0))
                throw new Exception();
#endif
            for (var sourceIndex = 0; sourceIndex < sourceBitLength; ++sourceIndex)
            {
                var destinationArrayIndex = sourceIndex / BIT_LENGTH_OF_UINT64;
                var destinationShiftCount = sourceIndex % BIT_LENGTH_OF_UINT64;
                if (booleanSequence[sourceIndex] == true)
                    destinationUInt64Array[destinationArrayIndex] |= 1UL << destinationShiftCount;
            }
            return new SerializedBitArray(destinationUInt64Array, sourceBitLength);
        }

        public static SerializedBitArray FromByteSequence(IEnumerable<byte> byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return FromByteSequence(byteSequence?.ToArray()?.AsReadOnly(), packingDirection);
        }

        public static SerializedBitArray FromByteSequence(byte[] byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return FromByteSequence(byteSequence?.ToArray()?.AsReadOnly(), packingDirection);
        }

        public static SerializedBitArray FromByteSequence(IReadOnlyArray<byte> byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (byteSequence == null)
                throw new ArgumentNullException("byteSequence");

            var sourceBitLength = byteSequence.Length * BIT_LENGTH_OF_BYTE;
            var destinationUInt64Array = new UInt64[(sourceBitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64];
#if DEBUG
            if (destinationUInt64Array.Any(item => item != 0))
                throw new Exception();
#endif
            for (var sourceIndex = 0; sourceIndex < byteSequence.Length; ++sourceIndex)
            {
                var destinationArrayIndex = sourceIndex / (BIT_LENGTH_OF_UINT64 / BIT_LENGTH_OF_BYTE);
                var destinationShiftCount = sourceIndex % (BIT_LENGTH_OF_UINT64 / BIT_LENGTH_OF_BYTE) * BIT_LENGTH_OF_BYTE;
                destinationUInt64Array[destinationArrayIndex] |= (UInt64)ConvertBitOrder(byteSequence[sourceIndex], BIT_LENGTH_OF_BYTE, packingDirection) << destinationShiftCount;
            }
            return new SerializedBitArray(destinationUInt64Array, sourceBitLength);
        }

        public static SerializedBitArray FromByte(Byte value, int bitCount = BIT_LENGTH_OF_BYTE, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_BYTE) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", BIT_LENGTH_OF_BYTE), "bitCount");
            return new SerializedBitArray(new UInt64[] { ConvertBitOrder(value, bitCount, packingDirection) }, bitCount);
        }

        public static SerializedBitArray FromUInt16(UInt16 value, int bitCount = BIT_LENGTH_OF_UINT16, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT16) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", BIT_LENGTH_OF_UINT16), "bitCount");
            return new SerializedBitArray(new UInt64[] { ConvertBitOrder(value, bitCount, packingDirection) }, bitCount);
        }

        public static SerializedBitArray FromUInt32(UInt32 value, int bitCount = BIT_LENGTH_OF_UINT32, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT32) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", BIT_LENGTH_OF_UINT32), "bitCount");
            return new SerializedBitArray(new UInt64[] { ConvertBitOrder(value, bitCount, packingDirection) }, bitCount);
        }

        public static SerializedBitArray FromUInt64(UInt64 value, int bitCount = BIT_LENGTH_OF_UINT64, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT64) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", BIT_LENGTH_OF_UINT64), "bitCount");
            return new SerializedBitArray(new UInt64[] { ConvertBitOrder(value, bitCount, packingDirection) }, bitCount);
        }

        public static SerializedBitArray Parse(string s)
        {
            return
                FromBooleanSequence(
                    s
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
                                throw new FormatException();
                        }
                    }));
        }

        public void Append(IEnumerable<bool> booleanSequence)
        {
            Append(booleanSequence?.ToArray());
        }

        public void Append(bool[] booleanSequence)
        {
            Append(booleanSequence?.AsReadOnly());
        }

        public void Append(IReadOnlyArray<bool> booleanSequence)
        {
            if (booleanSequence == null)
                throw new ArgumentNullException();

            var newBitLength = _bitLength + booleanSequence.Length;
            var newArrayLength = (newBitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
            if (newArrayLength != _uint64Array.Length)
                Array.Resize(ref _uint64Array, newArrayLength);
            for (var sourceBitIndex = 0; sourceBitIndex < booleanSequence.Length; ++sourceBitIndex)
            {
                var destinationBitIndex = _bitLength + sourceBitIndex;
                var destinationArrayIndex = destinationBitIndex / BIT_LENGTH_OF_UINT64;
                var destinationShiftCount = destinationBitIndex % BIT_LENGTH_OF_UINT64;
                if (booleanSequence[sourceBitIndex] == true)
                    _uint64Array[destinationArrayIndex] |= 1UL << destinationShiftCount;
            }
            _bitLength = newBitLength;
#if DEBUG
            CheckArray();
#endif
        }

        public void Append(IEnumerable<byte> byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            Append(byteSequence?.ToArray()?.AsReadOnly(), packingDirection);
        }

        public void Append(byte[] byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            Append(byteSequence?.ToArray()?.AsReadOnly(), packingDirection);
        }

        public void Append(IReadOnlyArray<byte> byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (byteSequence == null)
                throw new ArgumentNullException("byteSequence");

            var newBitLength = _bitLength + (byteSequence.Length * BIT_LENGTH_OF_BYTE);
            var newArrayLength = (newBitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
            if (newArrayLength != _uint64Array.Length)
                Array.Resize(ref _uint64Array, newArrayLength);
            for (var sourceByteIndex = 0; sourceByteIndex < byteSequence.Length; ++sourceByteIndex)
            {
                var destinationBitIndex = _bitLength + (sourceByteIndex * BIT_LENGTH_OF_BYTE);
                Append(
                    _uint64Array,
                    destinationBitIndex,
                    new UInt64[] { ConvertBitOrder(byteSequence[sourceByteIndex], BIT_LENGTH_OF_BYTE, packingDirection) }.AsReadOnly(),
                    BIT_LENGTH_OF_BYTE);
            }
            _bitLength = newBitLength;
#if DEBUG
            CheckArray();
#endif
        }

        public void Append(Byte value, int bitCount = BIT_LENGTH_OF_BYTE, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_BYTE) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", BIT_LENGTH_OF_BYTE), "bitCount");
            AppendInteger(value, bitCount, packingDirection);
        }

        public void Append(UInt16 value, int bitCount = BIT_LENGTH_OF_UINT16, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT16) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", BIT_LENGTH_OF_UINT16), "bitCount");
            AppendInteger(value, bitCount, packingDirection);
        }

        public void Append(UInt32 value, int bitCount = BIT_LENGTH_OF_UINT32, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT32) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", BIT_LENGTH_OF_UINT32), "bitCount");
            AppendInteger(value, bitCount, packingDirection);
        }

        public void Append(UInt64 value, int bitCount = BIT_LENGTH_OF_UINT64, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT64) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", BIT_LENGTH_OF_UINT64), "bitCount");
            AppendInteger(value, bitCount, packingDirection);
        }

        public void Append(SerializedBitArray bitArray)
        {
            if (bitArray == null)
                throw new ArgumentNullException("bitArray");

            var newBitLength = _bitLength + bitArray._bitLength;
            var newArrayLength = (newBitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
            if (newArrayLength != _uint64Array.Length)
                Array.Resize(ref _uint64Array, newArrayLength);
            Append(
                _uint64Array,
                _bitLength,
                bitArray._uint64Array.AsReadOnly(),
                bitArray._bitLength);
            _bitLength = newBitLength;
#if DEBUG
            CheckArray();
#endif
        }

        public void Append(ReadOnlySerializedBitArray bitArray)
        {
            bitArray.AppendTo(this);
        }

        public IReadOnlyArray<Byte> ToByteArray(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            var destinationByteLength = (_bitLength + BIT_LENGTH_OF_BYTE - 1) / BIT_LENGTH_OF_BYTE;
            var destinationByteArray = new Byte[destinationByteLength];
#if DEBUG
            if (destinationByteArray.Any(item => item != 0))
                throw new Exception();
#endif
            var sourceBitCount = _bitLength;
            for (var destinationByteIndex = 0; destinationByteIndex < destinationByteLength; ++destinationByteIndex)
            {
                var sourceArrayIndex = destinationByteIndex / (BIT_LENGTH_OF_UINT64 / BIT_LENGTH_OF_BYTE);
                var sourceShiftCount = destinationByteIndex % (BIT_LENGTH_OF_UINT64 / BIT_LENGTH_OF_BYTE) * BIT_LENGTH_OF_BYTE;
                var actualBitCount = sourceBitCount.Minimum(BIT_LENGTH_OF_BYTE);
                destinationByteArray[destinationByteIndex] = ConvertBitOrder((Byte)(_uint64Array[sourceArrayIndex] >> sourceShiftCount), actualBitCount, packingDirection);
                sourceBitCount -= actualBitCount;
            }
            return destinationByteArray.AsReadOnly();
        }

        public Byte ToByte(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (_bitLength < 1)
                throw new InvalidOperationException();
            if (_bitLength > BIT_LENGTH_OF_BYTE)
                throw new OverflowException();
            return ConvertBitOrder((Byte)_uint64Array[0], _bitLength.Minimum(BIT_LENGTH_OF_BYTE), packingDirection);
        }

        public UInt16 ToUInt16(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (_bitLength < 1)
                throw new InvalidOperationException();
            if (_bitLength > BIT_LENGTH_OF_UINT16)
                throw new OverflowException();
            return ConvertBitOrder((UInt16)_uint64Array[0], _bitLength.Minimum(BIT_LENGTH_OF_UINT16), packingDirection);
        }

        public UInt32 ToUInt32(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (_bitLength < 1)
                throw new InvalidOperationException();
            if (_bitLength > BIT_LENGTH_OF_UINT32)
                throw new OverflowException();
            return ConvertBitOrder((UInt32)_uint64Array[0], _bitLength.Minimum(BIT_LENGTH_OF_UINT32), packingDirection);
        }

        public UInt64 ToUInt64(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (_bitLength < 1)
                throw new InvalidOperationException();
            if (_bitLength > BIT_LENGTH_OF_UINT64)
                throw new OverflowException();
            return ConvertBitOrder((UInt64)_uint64Array[0], _bitLength.Minimum(BIT_LENGTH_OF_UINT64), packingDirection);
        }

        public SerializedBitArray Concat(IEnumerable<bool> booleanSequence)
        {
            return Concat(booleanSequence?.ToArray());
        }

        public SerializedBitArray Concat(bool[] booleanSequence)
        {
            return Concat(booleanSequence?.AsReadOnly());
        }

        public SerializedBitArray Concat(IReadOnlyArray<bool> booleanSequence)
        {
            if (booleanSequence == null)
                throw new ArgumentNullException("booleanSequence");

            if (_bitLength <= 0)
                return FromBooleanSequence(booleanSequence);
            else if (booleanSequence.Length <= 0)
                return Clone();
            else
            {
                var newBitLength = _bitLength + booleanSequence.Length;
                var thisArrayLength = (_bitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
                var newArray = new UInt64[(newBitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64];
#if DEBUG
                if (newArray.Any(item => item != 0))
                    throw new Exception();
#endif
                _uint64Array.CopyTo(newArray, 0);
                for (var sourceBitIndex = 0; sourceBitIndex < booleanSequence.Length; ++sourceBitIndex)
                {
                    var destinationBitIndex = _bitLength + sourceBitIndex;
                    var destinationArrayIndex = destinationBitIndex / BIT_LENGTH_OF_UINT64;
                    var destinationShiftCount = destinationBitIndex % BIT_LENGTH_OF_UINT64;
                    if (booleanSequence[sourceBitIndex] == true)
                        newArray[destinationArrayIndex] |= 1UL << destinationShiftCount;
                }
                return new SerializedBitArray(newArray, newBitLength);
            }
        }

        public SerializedBitArray Concat(IEnumerable<byte> byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return Concat(byteSequence?.ToArray()?.AsReadOnly(), packingDirection);
        }

        public SerializedBitArray Concat(byte[] byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return Concat(byteSequence?.ToArray()?.AsReadOnly(), packingDirection);
        }

        public SerializedBitArray Concat(IReadOnlyArray<Byte> byteSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (byteSequence == null)
                throw new ArgumentNullException("byteSequence");

            if (_bitLength <= 0)
                return FromByteSequence(byteSequence, packingDirection);
            else if (byteSequence.Length <= 0)
                return Clone();
            else
            {
                var newBitLength = _bitLength + (byteSequence.Length * BIT_LENGTH_OF_BYTE);
                var thisArrayLength = (_bitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
                var newArray = new UInt64[(newBitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64];
#if DEBUG
                if (newArray.Any(item => item != 0))
                    throw new Exception();
#endif
                _uint64Array.CopyTo(newArray, 0);
                for (var sourceByteIndex = 0; sourceByteIndex < byteSequence.Length; ++sourceByteIndex)
                {
                    var destinationBitIndex = _bitLength + (sourceByteIndex * BIT_LENGTH_OF_BYTE);
                    Append(
                        newArray,
                        destinationBitIndex,
                        new UInt64[] { ConvertBitOrder(byteSequence[sourceByteIndex], BIT_LENGTH_OF_BYTE, packingDirection) }.AsReadOnly(),
                        BIT_LENGTH_OF_BYTE);
                }
                return new SerializedBitArray(newArray, newBitLength);
            }
        }

        public SerializedBitArray Concat(Byte value, int bitCount = BIT_LENGTH_OF_BYTE, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_BYTE) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", BIT_LENGTH_OF_BYTE), "bitCount");
            return Concat(new UInt64[] { ConvertBitOrder(value, bitCount, packingDirection) }.AsReadOnly(), bitCount);
        }

        public SerializedBitArray Concat(UInt16 value, int bitCount = BIT_LENGTH_OF_UINT16, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT16) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", BIT_LENGTH_OF_UINT16), "bitCount");
            return Concat(new UInt64[] { ConvertBitOrder(value, bitCount, packingDirection) }.AsReadOnly(), bitCount);
        }

        public SerializedBitArray Concat(UInt32 value, int bitCount = BIT_LENGTH_OF_UINT32, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT32) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", BIT_LENGTH_OF_UINT32), "bitCount");
            return Concat(new UInt64[] { ConvertBitOrder(value, bitCount, packingDirection) }.AsReadOnly(), bitCount);
        }

        public SerializedBitArray Concat(UInt64 value, int bitCount = BIT_LENGTH_OF_UINT64, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT64) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", BIT_LENGTH_OF_UINT64), "bitCount");
            return Concat(new UInt64[] { ConvertBitOrder(value, bitCount, packingDirection) }.AsReadOnly(), bitCount);
        }

        public SerializedBitArray Concat(SerializedBitArray bitArray)
        {
            return Concat(bitArray._uint64Array.AsReadOnly(), bitArray._bitLength);
        }

        public SerializedBitArray Divide(int count, out SerializedBitArray remaining)
        {
            if (count.IsBetween(0, _bitLength) == false)
                throw new ArgumentException();

            var newBitLength1 = count;
            var newBitLength2 = _bitLength - count;
            var sourceArrayLength = (_bitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
            var newArrayLength1 = (newBitLength1 + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
            var newArrayLength2 = (newBitLength2 + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
            var newArray1 = new UInt64[newArrayLength1];
            var newArray2 = new UInt64[newArrayLength2];
            Array.Copy(_uint64Array, newArray1, newArrayLength1);
            var shiftBits = (newArrayLength1 * BIT_LENGTH_OF_UINT64) - newBitLength1;
#if DEBUG
            if (shiftBits.IsBetween(0, BIT_LENGTH_OF_UINT64 - 1) == false)
                throw new Exception();
#endif
            if (shiftBits == 0)
                Array.Copy(_uint64Array, newArrayLength1, newArray2, 0, newArrayLength2);
            else
            {
#if DEBUG
                if (shiftBits.IsBetween(1, BIT_LENGTH_OF_UINT64 - 1) == false)
                    throw new Exception();
#endif
                newArray1[newArrayLength1 - 1] &= (1UL << (BIT_LENGTH_OF_UINT64 - shiftBits)) - 1;
                if (newBitLength2 > 0)
                {
                    var bitCount = newBitLength2;
                    var remain = _uint64Array[newArrayLength1 - 1] >> (BIT_LENGTH_OF_UINT64 - shiftBits);
                    var destinationArrayIndex = 0;
                    while (destinationArrayIndex < sourceArrayLength - newArrayLength1)
                    {
                        var value = _uint64Array[newArrayLength1 + destinationArrayIndex];
                        newArray2[destinationArrayIndex] = remain | (value << shiftBits);
                        bitCount -= BIT_LENGTH_OF_UINT64;
                        remain = value >> (BIT_LENGTH_OF_UINT64 - shiftBits);
                        ++destinationArrayIndex;
                    }
                    if (bitCount > 0)
                        newArray2[destinationArrayIndex] = remain;
                }
            }
            remaining = new SerializedBitArray(newArray2, newBitLength2);
            return new SerializedBitArray(newArray1, newBitLength1);
        }

        public string ToString(string format)
        {
            var sb = new StringBuilder();
            switch (format.ToUpperInvariant())
            {
                case "R":
                    for (var index = 0; index < _bitLength; ++index)
                        sb.Append(GetValue(_uint64Array, index) ? "1" : "0");
                    break;
                case "G":
                    sb.Append("{");
                    for (var index = 0; index < _bitLength; ++index)
                    {
                        if (index > 0 && (index % BIT_LENGTH_OF_BYTE) == 0)
                            sb.Append("-");
                        sb.Append(GetValue(_uint64Array, index) ? "1" : "0");
                    }
                    sb.Append("}");
                    break;
                default:
                    throw new FormatException();
            }
            return sb.ToString();
        }

        public bool this[int index]
        {
            get
            {
                if (index >= _bitLength)
                    throw new IndexOutOfRangeException();
                return GetValue(_uint64Array, index);
            }

            set
            {
                if (index >= _bitLength)
                    throw new IndexOutOfRangeException();
                SetValue(_uint64Array, index, value);
            }
        }

        public int Length => _bitLength;

        public bool[] ToArray()
        {
            return
                Enumerable.Range(0, _bitLength)
                .Select(index => (_uint64Array[index / BIT_LENGTH_OF_UINT64] & (1UL << (index % BIT_LENGTH_OF_UINT64))) != 0)
                .ToArray();
        }

        public void CopyTo(bool[] destinationArray, int destinationOffset)
        {
            if (destinationOffset + _bitLength > destinationArray.Length)
                throw new IndexOutOfRangeException();
            for (var index = 0; index < _bitLength; ++index)
                destinationArray.SetValue(GetValue(_uint64Array, index), +destinationOffset + index);
        }

        public void CopyTo(int sourceIndex, bool[] destinationArray, int destinationOffset, int count)
        {
            if (sourceIndex + count > _bitLength)
                throw new IndexOutOfRangeException();
            if (destinationOffset + count > destinationArray.Length)
                throw new IndexOutOfRangeException();
            for (var index = 0; index < count; ++index)
                destinationArray.SetValue(GetValue(_uint64Array, sourceIndex + index), destinationOffset + index);
        }

        public ReadOnlySerializedBitArray AsReadOnly()
        {
            return new ReadOnlySerializedBitArray(this);
        }

        public IEnumerator<bool> GetEnumerator()
        {
            return new Enumerator(_uint64Array, _bitLength);
        }

        public SerializedBitArray Clone()
        {
            return new SerializedBitArray(_uint64Array.Duplicate(), _bitLength);
        }

        public bool Equals(SerializedBitArray other)
        {
            if (other == null)
                return false;
            if (!_bitLength.Equals(other._bitLength))
                return false;
            if (!_uint64Array.SequenceEqual(other._uint64Array))
                return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            return Equals((SerializedBitArray)obj);
        }

        public override int GetHashCode()
        {
            return
                _bitLength.GetHashCode() ^
                _uint64Array.Length.GetHashCode() ^
                _uint64Array.Aggregate(0, (x, y) => x ^ y.GetHashCode());
        }

        public override string ToString()
        {
            return ToString("G");
        }

        private SerializedBitArray Concat(IReadOnlyArray<UInt64> otherUInt64Array, int otherBitLength)
        {
            if (_bitLength <= 0)
                return new SerializedBitArray(otherUInt64Array.ToArray(), otherBitLength);
            else if (otherBitLength <= 0)
                return Clone();
            else
            {
                var newBitLength = _bitLength + otherBitLength;
                var thisArrayLength = (_bitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
                var newArray = new UInt64[(newBitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64];
#if DEBUG
                if (newArray.Any(item => item != 0))
                    throw new Exception();
#endif
                _uint64Array.CopyTo(newArray, 0);
                Append(newArray, _bitLength, otherUInt64Array, otherBitLength);
                return new SerializedBitArray(newArray, newBitLength);
            }
        }

        private static Byte ConvertBitOrder(Byte value, int bitCount, BitPackingDirection packingDirection)
        {
#if DEBUG
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_BYTE) == false)
                throw new Exception();
#endif
            switch (packingDirection)
            {
                case BitPackingDirection.MsbToLsb:
                    value = ReverseInteger(value);
                    if (bitCount < BIT_LENGTH_OF_BYTE)
                        value >>= BIT_LENGTH_OF_BYTE - bitCount;
                    break;
                case BitPackingDirection.LsbToMsb:
                    break;
                default:
                    throw new ArgumentException();
            }
            if (bitCount < BIT_LENGTH_OF_BYTE)
            {
                var mask = bitCount == BIT_LENGTH_OF_BYTE ? Byte.MaxValue : (Byte)((1 << bitCount) - 1);
#if DEBUG
                if (bitCount == BIT_LENGTH_OF_BYTE && mask != Byte.MaxValue)
                    throw new Exception();
#endif
                value &= mask;
            }
            return value;
        }

        private static UInt16 ConvertBitOrder(UInt16 value, int bitCount, BitPackingDirection packingDirection)
        {
#if DEBUG
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT16) == false)
                throw new Exception();
#endif
            switch (packingDirection)
            {
                case BitPackingDirection.MsbToLsb:
                    value = ReverseInteger(value);
                    if (bitCount < BIT_LENGTH_OF_UINT16)
                        value >>= BIT_LENGTH_OF_UINT16 - bitCount;
                    break;
                case BitPackingDirection.LsbToMsb:
                    break;
                default:
                    throw new ArgumentException();
            }
            if (bitCount < BIT_LENGTH_OF_UINT16)
            {
                var mask = (UInt16)((1 << bitCount) - 1);
#if DEBUG
                if (bitCount == BIT_LENGTH_OF_UINT16 && mask != UInt16.MaxValue)
                    throw new Exception();
#endif
                value &= mask;
            }
            return value;
        }

        private static UInt32 ConvertBitOrder(UInt32 value, int bitCount, BitPackingDirection packingDirection)
        {
#if DEBUG
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT32) == false)
                throw new Exception();
#endif
            switch (packingDirection)
            {
                case BitPackingDirection.MsbToLsb:
                    value = ReverseInteger(value);
                    if (bitCount < BIT_LENGTH_OF_UINT32)
                        value >>= BIT_LENGTH_OF_UINT32 - bitCount;
                    break;
                case BitPackingDirection.LsbToMsb:
                    break;
                default:
                    throw new ArgumentException();
            }
            if (bitCount < BIT_LENGTH_OF_UINT32)
            {
                var mask = (UInt32)((1U << bitCount) - 1);
#if DEBUG
                if (bitCount == BIT_LENGTH_OF_UINT32 && mask != UInt32.MaxValue)
                    throw new Exception();
#endif
                value &= mask;
            }
            return value;
        }

        private static UInt64 ConvertBitOrder(UInt64 value, int bitCount, BitPackingDirection packingDirection)
        {
#if DEBUG
            if (bitCount.IsBetween(1, BIT_LENGTH_OF_UINT64) == false)
                throw new Exception();
#endif
            switch (packingDirection)
            {
                case BitPackingDirection.MsbToLsb:
                    value = ReverseInteger(value);
                    if (bitCount < BIT_LENGTH_OF_UINT64)
                        value >>= BIT_LENGTH_OF_UINT64 - bitCount;
                    break;
                case BitPackingDirection.LsbToMsb:
                    break;
                default:
                    throw new ArgumentException();
            }
            if (bitCount < BIT_LENGTH_OF_UINT64)
            {
                var mask = (UInt64)((1UL << bitCount) - 1);
#if DEBUG
                if (bitCount == BIT_LENGTH_OF_UINT64 && mask != UInt64.MaxValue)
                    throw new Exception();
#endif
                value &= mask;
            }
            return value;
        }

        private static byte ReverseInteger(byte value)
        {
            value = (Byte)(((value /* & 0xf0*/) >> 4) | ((value /* & 0x0f*/) << 4));
            value = (Byte)(((value & 0xcc) >> 2) | ((value & 0x33) << 2));
            value = (Byte)(((value & 0xaa) >> 1) | ((value & 0x55) << 1));
            return value;
        }

        private static ushort ReverseInteger(ushort value)
        {
            value = (UInt16)(((value /* & 0xff00*/) >> 8) | ((value /* & 0x00ff*/) << 8));
            value = (UInt16)(((value & 0xf0f0) >> 4) | ((value & 0x0f0f) << 4));
            value = (UInt16)(((value & 0xcccc) >> 2) | ((value & 0x3333) << 2));
            value = (UInt16)(((value & 0xaaaa) >> 1) | ((value & 0x5555) << 1));
            return value;
        }

        private static uint ReverseInteger(uint value)
        {
            value = ((value /* & 0xffff0000U*/) >> 16) | ((value /* & 0x0000ffffU*/) << 16);
            value = ((value & 0xff00ff00U) >> 08) | ((value & 0x00ff00ffU) << 08);
            value = ((value & 0xf0f0f0f0U) >> 04) | ((value & 0x0f0f0f0fU) << 04);
            value = ((value & 0xccccccccU) >> 02) | ((value & 0x33333333U) << 02);
            value = ((value & 0xaaaaaaaaU) >> 01) | ((value & 0x55555555U) << 01);
            return value;
        }

        private static ulong ReverseInteger(ulong value)
        {
            value = ((value /* & 0xffffffff00000000UL*/) >> 32) | ((value /* & 0x00000000ffffffffUL*/) << 32);
            value = ((value & 0xffff0000ffff0000UL) >> 16) | ((value & 0x0000ffff0000ffffUL) << 16);
            value = ((value & 0xff00ff00ff00ff00UL) >> 08) | ((value & 0x00ff00ff00ff00ffUL) << 08);
            value = ((value & 0xf0f0f0f0f0f0f0f0UL) >> 04) | ((value & 0x0f0f0f0f0f0f0f0fUL) << 04);
            value = ((value & 0xccccccccccccccccUL) >> 02) | ((value & 0x3333333333333333UL) << 02);
            value = ((value & 0xaaaaaaaaaaaaaaaaUL) >> 01) | ((value & 0x5555555555555555UL) << 01);
            return value;
        }

        private static bool GetValue(UInt64[] uint64Array, int index)
        {
            return (uint64Array[index / BIT_LENGTH_OF_UINT64] & (1UL << (index % BIT_LENGTH_OF_UINT64))) != 0;
        }

        private static void SetValue(UInt64[] uint64Array, int index, bool value)
        {
            if (value)
                uint64Array[index / BIT_LENGTH_OF_UINT64] |= 1UL << (index % BIT_LENGTH_OF_UINT64);
            else
                uint64Array[index / BIT_LENGTH_OF_UINT64] &= ~(1UL << (index % BIT_LENGTH_OF_UINT64));
        }

        private void AppendInteger(UInt64 value, int bitCount, BitPackingDirection packingDirection)
        {
            var newBitLength = _bitLength + bitCount;
            var newArrayLength = (newBitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
            if (newArrayLength != _uint64Array.Length)
                Array.Resize(ref _uint64Array, newArrayLength);
            Append(_uint64Array, _bitLength, new UInt64[] { ConvertBitOrder(value, bitCount, packingDirection) }.AsReadOnly(), bitCount);
            _bitLength = newBitLength;
#if DEBUG
            CheckArray();
#endif
        }

        private static void Append(UInt64[] destinationUInt64Array, int destinationBitLength, IReadOnlyArray<UInt64> sourceUInt64Array, int sourceBitLength)
        {
            if ((destinationBitLength % BIT_LENGTH_OF_UINT64) == 0)
            {
                var destinationArrayLength = (destinationBitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
                var sourceArrayLength = (sourceBitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
                sourceUInt64Array.CopyTo(0, destinationUInt64Array, destinationArrayLength, sourceArrayLength);
            }
            else
            {
                var destinationArrayLength = (destinationBitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
                var sourceArrayLength = (sourceBitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64;
                var shiftBits = (destinationArrayLength << 6) - destinationBitLength;
#if DEBUG
                if (shiftBits.IsBetween(1, BIT_LENGTH_OF_UINT64 - 1) == false)
                    throw new Exception();
#endif
                var bitCount = sourceBitLength + (BIT_LENGTH_OF_UINT64 - shiftBits);
                var remain = 0UL;
                var sourceArrayIndex = 0;
                while (sourceArrayIndex < sourceUInt64Array.Length)
                {
                    var value = sourceUInt64Array[sourceArrayIndex];
                    destinationUInt64Array[destinationArrayLength - 1 + sourceArrayIndex] |= remain | (value << (BIT_LENGTH_OF_UINT64 - shiftBits));
                    bitCount -= BIT_LENGTH_OF_UINT64;
                    remain = value >> shiftBits;
                    ++sourceArrayIndex;
                }
                if (bitCount > 0)
                    destinationUInt64Array[destinationArrayLength - 1 + sourceArrayIndex] |= remain;
            }
        }

#if DEBUG
        private void CheckArray()
        {
            if (_uint64Array.Length != (_bitLength + BIT_LENGTH_OF_UINT64 - 1) / BIT_LENGTH_OF_UINT64)
                throw new Exception();
            var lastWordBitCount = _bitLength % BIT_LENGTH_OF_UINT64;
            if (lastWordBitCount > 0)
            {
                var mask = ~((1UL << lastWordBitCount) - 1);
                if ((_uint64Array[_uint64Array.Length - 1] & mask) != 0)
                    throw new Exception();
            }
        }
#endif

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

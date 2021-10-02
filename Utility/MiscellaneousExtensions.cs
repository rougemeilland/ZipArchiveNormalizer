using System;

namespace Utility
{
    public static class MiscellaneousExtensions
    {
        private const int _BIT_LENGTH_OF_BYTE = sizeof(Byte) << 3;
        private const int _BIT_LENGTH_OF_UINT16 = sizeof(UInt16) << 3;
        private const int _BIT_LENGTH_OF_UINT32 = sizeof(UInt32) << 3;
        private const int _BIT_LENGTH_OF_UINT64 = sizeof(UInt64) << 3;

        public static bool SignEquals(this int value, int other)
        {
            if (value > 0)
                return other > 0;
            else if (value < 0)
                return other < 0;
            else
                return other == 0;
        }

        public static Byte ReverseBitOrder(this Byte value)
        {
            value = (Byte)(((value /* & 0xf0*/) >> 4) | ((value /* & 0x0f*/) << 4));
            value = (Byte)(((value & 0xcc) >> 2) | ((value & 0x33) << 2));
            value = (Byte)(((value & 0xaa) >> 1) | ((value & 0x55) << 1));
            return value;
        }

        public static UInt16 ReverseBitOrder(this UInt16 value)
        {
            value = (UInt16)(((value /* & 0xff00*/) >> 8) | ((value /* & 0x00ff*/) << 8));
            value = (UInt16)(((value & 0xf0f0) >> 4) | ((value & 0x0f0f) << 4));
            value = (UInt16)(((value & 0xcccc) >> 2) | ((value & 0x3333) << 2));
            value = (UInt16)(((value & 0xaaaa) >> 1) | ((value & 0x5555) << 1));
            return value;
        }

        public static UInt32 ReverseBitOrder(this UInt32 value)
        {
            value = ((value /* & 0xffff0000U*/) >> 16) | ((value /* & 0x0000ffffU*/) << 16);
            value = ((value & 0xff00ff00U) >> 08) | ((value & 0x00ff00ffU) << 08);
            value = ((value & 0xf0f0f0f0U) >> 04) | ((value & 0x0f0f0f0fU) << 04);
            value = ((value & 0xccccccccU) >> 02) | ((value & 0x33333333U) << 02);
            value = ((value & 0xaaaaaaaaU) >> 01) | ((value & 0x55555555U) << 01);
            return value;
        }

        public static UInt64 ReverseBitOrder(this UInt64 value)
        {
            value = ((value /* & 0xffffffff00000000UL*/) >> 32) | ((value /* & 0x00000000ffffffffUL*/) << 32);
            value = ((value & 0xffff0000ffff0000UL) >> 16) | ((value & 0x0000ffff0000ffffUL) << 16);
            value = ((value & 0xff00ff00ff00ff00UL) >> 08) | ((value & 0x00ff00ff00ff00ffUL) << 08);
            value = ((value & 0xf0f0f0f0f0f0f0f0UL) >> 04) | ((value & 0x0f0f0f0f0f0f0f0fUL) << 04);
            value = ((value & 0xccccccccccccccccUL) >> 02) | ((value & 0x3333333333333333UL) << 02);
            value = ((value & 0xaaaaaaaaaaaaaaaaUL) >> 01) | ((value & 0x5555555555555555UL) << 01);
            return value;
        }

        public static UInt16 ReverseByteOrder(this UInt16 value)
        {
            value = (UInt16)(((value /* & 0xff00*/) >> 8) | ((value /* & 0x00ff*/) << 8));
            return value;
        }

        public static UInt32 ReverseByteOrder(this UInt32 value)
        {
            value = ((value /* & 0xffff0000U*/) >> 16) | ((value /* & 0x0000ffffU*/) << 16);
            value = ((value & 0xff00ff00U) >> 08) | ((value & 0x00ff00ffU) << 08);
            return value;
        }

        public static UInt64 ReverseByteOrder(this UInt64 value)
        {
            value = ((value /* & 0xffffffff00000000UL*/) >> 32) | ((value /* & 0x00000000ffffffffUL*/) << 32);
            value = ((value & 0xffff0000ffff0000UL) >> 16) | ((value & 0x0000ffff0000ffffUL) << 16);
            value = ((value & 0xff00ff00ff00ff00UL) >> 08) | ((value & 0x00ff00ff00ff00ffUL) << 08);
            return value;
        }

        public static IReadOnlyArray<byte> GetBytesLE(this Int16 value) => ((UInt16)value).GetBytesLE();

        public static IReadOnlyArray<byte> GetBytesLE(this UInt16 value)
        {
#if DEBUG
            if (sizeof(UInt16) != 2)
                throw new Exception();
#endif
            return
                new[]
                {
                    (byte)((value >> 0) & 0xff),
                    (byte)((value >> 8) & 0xff),
                }
                .AsReadOnly();
        }

        public static IReadOnlyArray<byte> GetBytesLE(this Int32 value) => ((UInt32)value).GetBytesLE();

        public static IReadOnlyArray<byte> GetBytesLE(this UInt32 value)
        {
#if DEBUG
            if (sizeof(UInt32) != 4)
                throw new Exception();
#endif
            return
                new[]
                {
                    (byte)((value >> 00) & 0xff),
                    (byte)((value >> 08) & 0xff),
                    (byte)((value >> 16) & 0xff),
                    (byte)((value >> 24) & 0xff),
                }
                .AsReadOnly();
        }

        public static IReadOnlyArray<byte> GetBytesLE(this Int64 value) => ((UInt64)value).GetBytesLE();

        public static IReadOnlyArray<byte> GetBytesLE(this UInt64 value)
        {
#if DEBUG
            if (sizeof(UInt64) != 8)
                throw new Exception();
#endif
            return
                new[]
                {
                    (byte)((value >> 00) & 0xff),
                    (byte)((value >> 08) & 0xff),
                    (byte)((value >> 16) & 0xff),
                    (byte)((value >> 24) & 0xff),
                    (byte)((value >> 32) & 0xff),
                    (byte)((value >> 40) & 0xff),
                    (byte)((value >> 48) & 0xff),
                    (byte)((value >> 56) & 0xff),
                }
                .AsReadOnly();
        }

        public static IReadOnlyArray<byte> GetBytesLE(this Single value)
        {
            if (BitConverter.IsLittleEndian)
                return BitConverter.GetBytes(value).AsReadOnly();
            else
                return BitConverter.GetBytes(value).ReverseArray().AsReadOnly();
        }

        public static IReadOnlyArray<byte> GetBytesLE(this Double value)
        {
            if (BitConverter.IsLittleEndian)
                return BitConverter.GetBytes(value).AsReadOnly();
            else
                return BitConverter.GetBytes(value).ReverseArray().AsReadOnly();
        }

        public static IReadOnlyArray<byte> GetBytesBE(this Int16 value) => ((UInt16)value).GetBytesBE();

        public static IReadOnlyArray<byte> GetBytesBE(this UInt16 value)
        {
#if DEBUG
            if (sizeof(UInt16) != 2)
                throw new Exception();
#endif
            return
                new[]
                {
                    (byte)((value >> 8) & 0xff),
                    (byte)((value >> 0) & 0xff),
                }
                .AsReadOnly();
        }

        public static IReadOnlyArray<byte> GetBytesBE(this Int32 value) => ((UInt32)value).GetBytesBE();

        public static IReadOnlyArray<byte> GetBytesBE(this UInt32 value)
        {
#if DEBUG
            if (sizeof(UInt32) != 4)
                throw new Exception();
#endif
            return
                new[]
                {
                    (byte)((value >> 24) & 0xff),
                    (byte)((value >> 16) & 0xff),
                    (byte)((value >> 08) & 0xff),
                    (byte)((value >> 00) & 0xff),
                }
                .AsReadOnly();
        }

        public static IReadOnlyArray<byte> GetBytesBE(this Int64 value) => ((UInt64)value).GetBytesBE();

        public static IReadOnlyArray<byte> GetBytesBE(this UInt64 value)
        {
#if DEBUG
            if (sizeof(UInt64) != 8)
                throw new Exception();
#endif
            return
                new[]
                {
                    (byte)((value >> 56) & 0xff),
                    (byte)((value >> 48) & 0xff),
                    (byte)((value >> 40) & 0xff),
                    (byte)((value >> 32) & 0xff),
                    (byte)((value >> 24) & 0xff),
                    (byte)((value >> 16) & 0xff),
                    (byte)((value >> 08) & 0xff),
                    (byte)((value >> 00) & 0xff),
                }
                .AsReadOnly();
        }

        public static IReadOnlyArray<byte> GetBytesBE(this Single value)
        {
            if (BitConverter.IsLittleEndian)
                return BitConverter.GetBytes(value).ReverseArray().AsReadOnly();
            else
                return BitConverter.GetBytes(value).AsReadOnly();
        }

        public static IReadOnlyArray<byte> GetBytesBE(this Double value)
        {
            if (BitConverter.IsLittleEndian)
                return BitConverter.GetBytes(value).ReverseArray().AsReadOnly();
            else
                return BitConverter.GetBytes(value).AsReadOnly();
        }

        internal static Byte ConvertBitOrder(this Byte value, int bitCount, BitPackingDirection packingDirection)
        {
#if DEBUG
            if (bitCount.IsBetween(1, _BIT_LENGTH_OF_BYTE) == false)
                throw new Exception();
#endif
            switch (packingDirection)
            {
                case BitPackingDirection.MsbToLsb:
                    value = value.ReverseBitOrder();
                    if (bitCount < _BIT_LENGTH_OF_BYTE)
                        value >>= _BIT_LENGTH_OF_BYTE - bitCount;
                    break;
                case BitPackingDirection.LsbToMsb:
                    break;
                default:
                    throw new ArgumentException();
            }
            if (bitCount < _BIT_LENGTH_OF_BYTE)
            {
                var mask = bitCount == _BIT_LENGTH_OF_BYTE ? Byte.MaxValue : (Byte)((1 << bitCount) - 1);
#if DEBUG
                if (bitCount == _BIT_LENGTH_OF_BYTE && mask != Byte.MaxValue)
                    throw new Exception();
#endif
                value &= mask;
            }
            return value;
        }

        internal static UInt16 ConvertBitOrder(this UInt16 value, int bitCount, BitPackingDirection packingDirection)
        {
#if DEBUG
            if (bitCount.IsBetween(1, _BIT_LENGTH_OF_UINT16) == false)
                throw new Exception();
#endif
            switch (packingDirection)
            {
                case BitPackingDirection.MsbToLsb:
                    value = value.ReverseBitOrder();
                    if (bitCount < _BIT_LENGTH_OF_UINT16)
                        value >>= _BIT_LENGTH_OF_UINT16 - bitCount;
                    break;
                case BitPackingDirection.LsbToMsb:
                    break;
                default:
                    throw new ArgumentException();
            }
            if (bitCount < _BIT_LENGTH_OF_UINT16)
            {
                var mask = (UInt16)((1 << bitCount) - 1);
#if DEBUG
                if (bitCount == _BIT_LENGTH_OF_UINT16 && mask != UInt16.MaxValue)
                    throw new Exception();
#endif
                value &= mask;
            }
            return value;
        }

        internal static UInt32 ConvertBitOrder(this UInt32 value, int bitCount, BitPackingDirection packingDirection)
        {
#if DEBUG
            if (bitCount.IsBetween(1, _BIT_LENGTH_OF_UINT32) == false)
                throw new Exception();
#endif
            switch (packingDirection)
            {
                case BitPackingDirection.MsbToLsb:
                    value = value.ReverseBitOrder();
                    if (bitCount < _BIT_LENGTH_OF_UINT32)
                        value >>= _BIT_LENGTH_OF_UINT32 - bitCount;
                    break;
                case BitPackingDirection.LsbToMsb:
                    break;
                default:
                    throw new ArgumentException();
            }
            if (bitCount < _BIT_LENGTH_OF_UINT32)
            {
                var mask = (UInt32)((1U << bitCount) - 1);
#if DEBUG
                if (bitCount == _BIT_LENGTH_OF_UINT32 && mask != UInt32.MaxValue)
                    throw new Exception();
#endif
                value &= mask;
            }
            return value;
        }

        internal static UInt64 ConvertBitOrder(this UInt64 value, int bitCount, BitPackingDirection packingDirection)
        {
#if DEBUG
            if (bitCount.IsBetween(1, _BIT_LENGTH_OF_UINT64) == false)
                throw new Exception();
#endif
            switch (packingDirection)
            {
                case BitPackingDirection.MsbToLsb:
                    value = value.ReverseBitOrder();
                    if (bitCount < _BIT_LENGTH_OF_UINT64)
                        value >>= _BIT_LENGTH_OF_UINT64 - bitCount;
                    break;
                case BitPackingDirection.LsbToMsb:
                    break;
                default:
                    throw new ArgumentException();
            }
            if (bitCount < _BIT_LENGTH_OF_UINT64)
            {
                var mask = (UInt64)((1UL << bitCount) - 1);
#if DEBUG
                if (bitCount == _BIT_LENGTH_OF_UINT64 && mask != UInt64.MaxValue)
                    throw new Exception();
#endif
                value &= mask;
            }
            return value;
        }

        /// <summary>
        /// 与えられた配列の要素を逆順に並べ替えます。
        /// </summary>
        /// <typeparam name="ELEMENT_T">
        /// 配列の要素の型です。
        /// </typeparam>
        /// <param name="array">
        /// 並び替える配列です。
        /// </param>
        /// <returns>
        /// 並び替えられた配列です。この配列は<paramref name="array"/>と同じ参照です。
        /// </returns>
        /// <remarks>
        /// このメソッドは<paramref name="array"/>で与えられた配列の内容を変更します。
        /// </remarks>
        internal static ELEMENT_T[] ReverseArray<ELEMENT_T>(this ELEMENT_T[] array)
        {
            var index1 = 0;
            var index2 = array.Length - 1;
            while (index2 > index1)
            {
                var t = array[index1];
                array[index1] = array[index2];
                array[index2] = t;
                ++index1;
                --index2;
            }
            return array;
        }
    }
}

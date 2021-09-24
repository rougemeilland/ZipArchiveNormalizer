using System;

namespace Utility
{
    /// <summary>
    /// <see cref="bool"/>で表現されたビットを要素とするキューのクラスです。
    /// キュー操作はビット単位だけではなく、整数をビット集合とみなして操作することもできます。
    /// </summary>
    /// <remarks>
    /// このクラスは比較的少ないビット数の集合を扱う際にパフォーマンスが向上するように設計されています。
    /// <see cref="RecommendedMaxCount"/>を超える大きさのビットを保持するとパフォーマンスが低下することがありますので注意してください。
    /// </remarks>
    public class BitQueue
        : ICloneable<BitQueue>
    {
        private const int _BIT_LENGTH_OF_BYTE = sizeof(Byte) << 3;
        private const int _BIT_LENGTH_OF_UINT16 = sizeof(UInt16) << 3;
        private const int _BIT_LENGTH_OF_UINT32 = sizeof(UInt32) << 3;
        private const int _BIT_LENGTH_OF_UINT64 = sizeof(UInt64) << 3;

        public const int RecommendedMaxCount = _BIT_LENGTH_OF_UINT64;

        private InternalBitQueue _bitQueue;

        public BitQueue()
            : this(new InternalBitQueue())
        {
        }

        public BitQueue(IReadOnlyArray<bool> bitPattern)
            : this(new InternalBitQueue(bitPattern))
        {
        }

        public BitQueue(string bitPattern)
            : this(new InternalBitQueue(bitPattern))
        {
        }

        private BitQueue(InternalBitQueue bitQueue)
        {
            _bitQueue = bitQueue;
        }

        public void Enqueue(bool value)
        {
            _bitQueue.Enqueue(value);
        }

        public void Enqueue(Byte value, int bitCount = _BIT_LENGTH_OF_BYTE, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, _BIT_LENGTH_OF_BYTE) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", _BIT_LENGTH_OF_BYTE), "bitCount");
            _bitQueue.Enqueue(value, bitCount, packingDirection);
        }

        public void Enqueue(UInt16 value, int bitCount = _BIT_LENGTH_OF_UINT16, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, _BIT_LENGTH_OF_UINT16) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", _BIT_LENGTH_OF_UINT16), "bitCount");
            _bitQueue.Enqueue(value, bitCount, packingDirection);
        }

        public void Enqueue(UInt32 value, int bitCount = _BIT_LENGTH_OF_UINT32, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, _BIT_LENGTH_OF_UINT32) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", _BIT_LENGTH_OF_UINT32), "bitCount");
            _bitQueue.Enqueue(value, bitCount, packingDirection);
        }

        public void Enqueue(UInt64 value, int bitCount = _BIT_LENGTH_OF_UINT64, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (bitCount.IsBetween(1, _BIT_LENGTH_OF_UINT64) == false)
                throw new ArgumentException(string.Format("'bitCount' must be greater than or equal to 1 and less than or equal to {0}.", _BIT_LENGTH_OF_UINT64), "bitCount");
            _bitQueue.Enqueue(value, bitCount, packingDirection);
        }

        public void Enqueue(TinyBitArray bitArray)
        {
            if (bitArray == null)
                throw new ArgumentNullException("bitArray");
#if DEBUG
            if (bitArray as IInternalBitArray == null)
                throw new Exception();
#endif
            _bitQueue.Enqueue((bitArray as IInternalBitArray).BitArray);
        }

        public bool DequeueBoolean()
        {
            if (_bitQueue.Length < 1)
                throw new InvalidOperationException();
            return _bitQueue.DequeueBoolean();
        }

        public Byte DequeueByte(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (_bitQueue.Length < 1)
                throw new InvalidOperationException();
            return (Byte)_bitQueue.DequeueInteger(_BIT_LENGTH_OF_BYTE.Minimum(_bitQueue.Length), packingDirection);
        }

        public UInt16 DequeueUInt16(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (_bitQueue.Length < 1)
                throw new InvalidOperationException();
            return (UInt16)_bitQueue.DequeueInteger(_BIT_LENGTH_OF_UINT16.Minimum(_bitQueue.Length), packingDirection);
        }

        public UInt32 DequeueUInt32(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (_bitQueue.Length < 1)
                throw new InvalidOperationException();
            return (UInt32)_bitQueue.DequeueInteger(_BIT_LENGTH_OF_UINT32.Minimum(_bitQueue.Length), packingDirection);
        }

        public UInt64 DequeueUInt64(BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            if (_bitQueue.Length < 1)
                throw new InvalidOperationException();
            return (UInt64)_bitQueue.DequeueInteger(_BIT_LENGTH_OF_UINT64.Minimum(_bitQueue.Length), packingDirection);
        }

        public TinyBitArray DequeueBitArray(int bitCount)
        {
            if (_bitQueue.Length < 1)
                throw new InvalidOperationException();
            return new TinyBitArray(_bitQueue.DequeueBitQueue(bitCount.Minimum(_bitQueue.Length)));
        }

        public string ToString(string format) => _bitQueue.ToString(format);
        public int Count => _bitQueue.Length;
        public BitQueue Clone() => new BitQueue(_bitQueue.Clone());
        public override string ToString() => ToString("G");
    }
}

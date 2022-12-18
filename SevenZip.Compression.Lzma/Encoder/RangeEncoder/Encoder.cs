// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using System;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Encoder.RangeEncoder
{
    class Encoder
    {
        private const UInt32 kTopValue = 1 << 24;
        private const Int32 kNumBitModelTotalBits = 11;
        private const UInt32 kBitModelTotal = 1 << kNumBitModelTotalBits;
        private const Int32 kNumMoveBits = 5;

        private UInt64 _low;
        private UInt32 _range;
        private UInt32 _cacheSize;
        private Byte _cache;
        private UInt64 _processedCount;

        public Encoder()
        {
            _processedCount = 0;
            _low = 0;
            _range = UInt32.MaxValue;
            _cacheSize = 1;
            _cache = 0;
        }

        public UInt64 ProcessedSizeAdd => _cacheSize + _processedCount + 4;

        public void Encode(IBasicOutputByteStream outStream, UInt32 symbol, ref UInt16 prob)
        {
#if DEBUG
            if (symbol.IsNoneOf(0U, 1U))
                throw new Exception();
#endif
            var newBound = (_range >> kNumBitModelTotalBits) * prob;
            if (symbol == 0)
            {
                _range = newBound;
                prob += (UInt16)((kBitModelTotal - prob) >> kNumMoveBits);
            }
            else
            {
                _low += newBound;
                _range -= newBound;
                prob -= (UInt16)(prob >> kNumMoveBits);
            }
            if (_range < kTopValue)
            {
                _range <<= 8;
                ShiftLow(outStream);
            }
        }

        public void EncodeDirectBits(IBasicOutputByteStream outStream, UInt32 v, Int32 numTotalBits)
        {
            for (var i = numTotalBits - 1; i >= 0; i--)
            {
                _range >>= 1;
                if (((v >> i) & 1) == 1)
                    _low += _range;
                if (_range < kTopValue)
                {
                    _range <<= 8;
                    ShiftLow(outStream);
                }
            }
        }

        public void FlushData(IBasicOutputByteStream outStream)
        {
            for (var i = 0; i < 5; i++)
                ShiftLow(outStream);
        }

        public void ShiftLow(IBasicOutputByteStream outStream)
        {
            if ((UInt32)_low < 0xff000000U || (UInt32)(_low >> 32) == 1U)
            {
                var temp = _cache;
                do
                {
                    outStream.WriteByte((Byte)(temp + (_low >> 32)));
                    temp = 0xFF;
                    ++_processedCount;
                }
                while (--_cacheSize > 0);
                _cache = (Byte)(((UInt32)_low) >> 24);
            }
            _cacheSize++;
            _low = ((UInt32)_low) << 8;
        }
    }
}

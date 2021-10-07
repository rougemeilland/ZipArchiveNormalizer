using System;

namespace Utility.IO.Compression.Lzma.RangeCoder
{
    class RangeEncoder
    {
        public const UInt32 kTopValue = 1U << 24;

        private IBasicOutputByteStream _stream;

        public UInt64 Low;
        public UInt32 Range;
        private UInt32 _cacheSize;
        private byte _cache;
        private UInt64 _writtenCount;

        public RangeEncoder()
        {
            Low = 0;
            Range = 0;
            _cacheSize = 0;
            _cache = 0;
            _writtenCount = 0;
        }

        public void SetStream(IBasicOutputByteStream stream)
        {
            _stream = stream;
        }

        public void ReleaseStream()
        {
            _stream = null;
        }

        public void Init()
        {
            Low = 0;
            Range = 0xFFFFFFFFU;
            _cacheSize = 1;
            _cache = 0;
        }

        public void FlushData()
        {
            for (var i = 0; i < 5; i++)
                ShiftLow();
        }

        public void ShiftLow()
        {
            if ((UInt32)Low < 0xFF000000U || (UInt32)(Low >> 32) == 1)
            {
                var temp = _cache;
                do
                {
                    _stream.WriteBytes(new[] { (byte)(temp + (Low >> 32)) }.AsReadOnly(), 0, 1);
                    ++_writtenCount;
                    temp = 0xFF;
                }
                while (--_cacheSize != 0);
                _cache = (byte)(((UInt32)Low) >> 24);
            }
            _cacheSize++;
            Low = ((UInt32)Low) << 8;
        }

        public void EncodeDirectBits(UInt32 v, int numTotalBits)
        {
            for (var i = numTotalBits - 1; i >= 0; i--)
            {
                Range >>= 1;
                if (((v >> i) & 1) == 1)
                    Low += Range;
                if (Range < kTopValue)
                {
                    Range <<= 8;
                    ShiftLow();
                }
            }
        }

        public UInt64 GetProcessedSizeAdd()
        {
#if DEBUG
            checked
#endif
            {
                return _cacheSize + _writtenCount + 4;
            }
        }
    }
}

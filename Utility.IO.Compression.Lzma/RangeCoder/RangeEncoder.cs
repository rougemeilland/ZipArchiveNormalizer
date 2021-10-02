using System;

namespace Utility.IO.Compression.RangeCoder
{
    class RangeEncoder
    {
        public const UInt32 kTopValue = 1U << 24;

        private  IOutputByteStream<UInt64> _stream;

        public UInt64 Low;
        public uint Range;
        private uint _cacheSize;
        private byte _cache;
        private long _writtenCount;

        public RangeEncoder()
        {
            Low = 0;
            Range = 0;
            _cacheSize = 0;
            _cache = 0;
            _writtenCount = 0;
        }

        public void SetStream(IOutputByteStream<UInt64> stream)
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
            for (int i = 0; i < 5; i++)
                ShiftLow();
        }

        public void ShiftLow()
        {
            if ((uint)Low < 0xFF000000U || (uint)(Low >> 32) == 1)
            {
                byte temp = _cache;
                do
                {
                    _stream.WriteBytes(new[] { (byte)(temp + (Low >> 32)) }.AsReadOnly(), 0, 1);
                    ++_writtenCount;
                    temp = 0xFF;
                }
                while (--_cacheSize != 0);
                _cache = (byte)(((uint)Low) >> 24);
            }
            _cacheSize++;
            Low = ((uint)Low) << 8;
        }

        public void EncodeDirectBits(uint v, int numTotalBits)
        {
            for (int i = numTotalBits - 1; i >= 0; i--)
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

        public long GetProcessedSizeAdd()
        {
            return _cacheSize + _writtenCount + 4;
        }
    }
}

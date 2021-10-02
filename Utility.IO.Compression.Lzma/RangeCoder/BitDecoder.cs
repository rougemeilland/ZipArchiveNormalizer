namespace Utility.IO.Compression.RangeCoder
{
    struct BitDecoder
    {
        public const int kNumBitModelTotalBits = 11;
        public const uint kBitModelTotal = 1 << kNumBitModelTotalBits;
        private const int kNumMoveBits = 5;

        private uint _prob;

        public void Init() { _prob = kBitModelTotal >> 1; }

        public bool Decode(RangeCoder.RangeDecoder rangeDecoder)
        {
            uint newBound = (rangeDecoder.Range >> kNumBitModelTotalBits) * _prob;
            if (rangeDecoder.Code < newBound)
            {
                rangeDecoder.Range = newBound;
                _prob += (kBitModelTotal - _prob) >> kNumMoveBits;
                if (rangeDecoder.Range < RangeDecoder.kTopValue)
                {
                    rangeDecoder.Code = (rangeDecoder.Code << 8) | rangeDecoder.ReadByte();
                    rangeDecoder.Range <<= 8;
                }
                return false;
            }
            else
            {
                rangeDecoder.Range -= newBound;
                rangeDecoder.Code -= newBound;
                _prob -= _prob >> kNumMoveBits;
                if (rangeDecoder.Range < RangeDecoder.kTopValue)
                {
                    rangeDecoder.Code = (rangeDecoder.Code << 8) | rangeDecoder.ReadByte();
                    rangeDecoder.Range <<= 8;
                }
                return true;
            }
        }
    }
}

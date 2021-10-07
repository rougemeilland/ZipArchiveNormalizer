using System;

namespace Utility.IO.Compression.Lzma
{
    class LzmaLenPriceTableEncoder
        : LzmaLenEncoder
    {
        private UInt32[] _prices;
        private UInt32 _tableSize;
        private UInt32[] _counters;

        public LzmaLenPriceTableEncoder()
        {
            _prices = new UInt32[LzmaCoder.kNumLenSymbols << LzmaCoder.kNumPosStatesBitsEncodingMax];
            _tableSize = 0;
            _counters = new UInt32[LzmaCoder.kNumPosStatesEncodingMax];
        }

        public void SetTableSize(UInt32 tableSize) { _tableSize = tableSize; }

        public UInt32 GetPrice(UInt32 symbol, UInt32 posState)
        {
            return _prices[posState * LzmaCoder.kNumLenSymbols + symbol];
        }

        public void UpdateTables(UInt32 numPosStates)
        {
            for (UInt32 posState = 0; posState < numPosStates; posState++)
                UpdateTable(posState);
        }

        public override void Encode(RangeCoder.RangeEncoder rangeEncoder, UInt32 symbol, UInt32 posState)
        {
            base.Encode(rangeEncoder, symbol, posState);
            if (--_counters[posState] == 0)
                UpdateTable(posState);
        }

        private void UpdateTable(UInt32 posState)
        {
            SetPrices(posState, _tableSize, _prices, posState * LzmaCoder.kNumLenSymbols);
            _counters[posState] = _tableSize;
        }
    }
}

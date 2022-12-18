// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using System;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Encoder
{
    class LenPriceTableEncoder
        : LenEncoder
    {
        private readonly UInt32[] _prices;
        private readonly UInt32 _tableSize;
        private readonly UInt32[] _counters;

        public LenPriceTableEncoder(UInt32 numPosStates, UInt32 tableSize)
            : base(numPosStates)
        {
            _prices = new UInt32[LzmaConstants.kNumLenSymbols << LzmaConstants.kNumPosStatesBitsEncodingMax];
            _counters = new UInt32[LzmaConstants.kNumPosStatesEncodingMax];
            _tableSize = tableSize;
        }

        public UInt32 GetPrice(UInt32 symbol, UInt32 posState) => _prices[posState * LzmaConstants.kNumLenSymbols + symbol];

        public void UpdateTables(UInt32 numPosStates)
        {
            for (UInt32 posState = 0; posState < numPosStates; posState++)
                UpdateTable(posState);
        }

        public override void Encode(IBasicOutputByteStream outStream, RangeEncoder.Encoder rangeEncoder, UInt32 symbol, UInt32 posState)
        {
            base.Encode(outStream, rangeEncoder, symbol, posState);
            if (--_counters[posState] == 0)
                UpdateTable(posState);
        }

        private void UpdateTable(UInt32 posState)
        {
            SetPrices(posState, _tableSize, _prices, posState * LzmaConstants.kNumLenSymbols);
            _counters[posState] = _tableSize;
        }
    }
}

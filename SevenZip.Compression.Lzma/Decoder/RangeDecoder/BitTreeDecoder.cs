// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using System;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Decoder.RangeDecoder
{
    class BitTreeDecoder
    {
        private readonly BitDecoder[] _models;
        private readonly Int32 _numBitLevels;

        public BitTreeDecoder(Int32 numBitLevels)
        {
            _numBitLevels = numBitLevels;
            _models = new BitDecoder[1 << numBitLevels];
            _models.FillArray(_ => new BitDecoder());
        }

        public UInt32 Decode(IBasicInputByteStream inStream, Decoder rangeDecoder)
        {
            var m = 1U;
            for (var bitIndex = _numBitLevels; bitIndex > 0; bitIndex--)
                m = (m << 1) + _models[m].Decode(inStream, rangeDecoder);
            return m - (1U << _numBitLevels);
        }

        public UInt32 ReverseDecode(IBasicInputByteStream inStream, Decoder rangeDecoder)
        {
            var m = 1U;
            var symbol = 0U;
            for (var bitIndex = 0; bitIndex < _numBitLevels; bitIndex++)
            {
                var bit = _models[m].Decode(inStream, rangeDecoder);
                m <<= 1;
                m += bit;
                symbol |= bit << bitIndex;
            }
            return symbol;
        }

        public static UInt32 ReverseDecode(IBasicInputByteStream inStream, BitDecoder[] Models, UInt32 startIndex, Decoder rangeDecoder, Int32 NumBitLevels)
        {
            var m = 1U;
            var symbol = 0U;
            for (var bitIndex = 0; bitIndex < NumBitLevels; bitIndex++)
            {
                var bit = Models[startIndex + m].Decode(inStream, rangeDecoder);
                m <<= 1;
                m += bit;
                symbol |= bit << bitIndex;
            }
            return symbol;
        }
    }
}

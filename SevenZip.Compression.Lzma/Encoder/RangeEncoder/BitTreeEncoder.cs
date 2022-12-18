// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using System;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Encoder.RangeEncoder
{
    class BitTreeEncoder
    {
        private readonly BitEncoder[] _models;
        private readonly Int32 _numBitLevels;

        public BitTreeEncoder(Int32 numBitLevels)
        {
            _numBitLevels = numBitLevels;
            _models = new BitEncoder[1 << numBitLevels];
            _models.FillArray(_ => new BitEncoder());
        }

        public void Encode(IBasicOutputByteStream outStream, Encoder rangeEncoder, UInt32 symbol)
        {
            var m = 1U;
            for (var bitIndex = _numBitLevels; bitIndex > 0;)
            {
                bitIndex--;
                var bit = (symbol >> bitIndex) & 1;
                _models[m].Encode(outStream, rangeEncoder, bit);
                m = (m << 1) | bit;
            }
        }

        public void ReverseEncode(IBasicOutputByteStream outStream, Encoder rangeEncoder, UInt32 symbol)
        {
            var m = 1U;
            for (var i = 0; i < _numBitLevels; i++)
            {
                var bit = symbol & 1;
                _models[m].Encode(outStream, rangeEncoder, bit);
                m = (m << 1) | bit;
                symbol >>= 1;
            }
        }

        public UInt32 GetPrice(UInt32 symbol)
        {
            var price = 0U;
            var m = 1U;
            for (var bitIndex = _numBitLevels; bitIndex > 0;)
            {
                bitIndex--;
                var bit = (symbol >> bitIndex) & 1;
                price += _models[m].GetPrice(bit);
                m = (m << 1) + bit;
            }
            return price;
        }

        public UInt32 ReverseGetPrice(UInt32 symbol)
        {
            var price = 0U;
            var m = 1U;
            for (var i = _numBitLevels; i > 0; i--)
            {
                var bit = symbol & 1;
                symbol >>= 1;
                price += _models[m].GetPrice(bit);
                m = (m << 1) | bit;
            }
            return price;
        }

        public static UInt32 ReverseGetPrice(BitEncoder[] Models, UInt32 startIndex, Int32 NumBitLevels, UInt32 symbol)
        {
            var price = 0U;
            var m = 1U;
            for (var i = NumBitLevels; i > 0; i--)
            {
                var bit = symbol & 1;
                symbol >>= 1;
                price += Models[startIndex + m].GetPrice(bit);
                m = (m << 1) | bit;
            }
            return price;
        }

        public static void ReverseEncode(IBasicOutputByteStream outStream, BitEncoder[] Models, UInt32 startIndex, Encoder rangeEncoder, Int32 NumBitLevels, UInt32 symbol)
        {
            var m = 1U;
            for (var i = 0; i < NumBitLevels; i++)
            {
                var bit = symbol & 1;
                Models[startIndex + m].Encode(outStream, rangeEncoder, bit);
                m = (m << 1) | bit;
                symbol >>= 1;
            }
        }
    }
}

// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using System;
using System.Runtime.CompilerServices;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Decoder.RangeDecoder
{
    class Decoder
    {
        private const UInt32 kTopValue = 1 << 24;
        private const Int32 kNumBitModelTotalBits = 11;
        private const UInt32 kBitModelTotal = 1 << kNumBitModelTotalBits;
        private const Int32 kNumMoveBits = 5;

        private UInt32 _range;
        private UInt32 _code;

        public Decoder()
        {
            _code = 0;
            _range = UInt32.MaxValue;
        }

        public void Init(IBasicInputByteStream inStream)
        {
            var firstByte = inStream.ReadByte();
            if (firstByte != 0)
                throw new SevenZipDataErrorException();
            _code = inStream.ReadUInt32BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32 Decode(IBasicInputByteStream inStream, ref UInt16 prob)
        {
            var newBound = (_range >> kNumBitModelTotalBits) * prob;
            if (_code < newBound)
            {
                _range = newBound;
                prob += (UInt16)((kBitModelTotal - prob) >> kNumMoveBits);
                if (_range < kTopValue)
                {
                    _code = (_code << 8) | inStream.ReadByte();
                    _range <<= 8;
                }
                return 0;
            }
            else
            {
                _range -= newBound;
                _code -= newBound;
                prob -= (UInt16)(prob >> kNumMoveBits);
                if (_range < kTopValue)
                {
                    _code = (_code << 8) | inStream.ReadByte();
                    _range <<= 8;
                }
                return 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32 DecodeDirectBits(IBasicInputByteStream inStream, Int32 numTotalBits)
        {
            var range = _range;
            var code = _code;
            var result = 0U;
            for (var i = numTotalBits; i > 0; i--)
            {
                range >>= 1;
                var t = (code - range) >> 31;
                code -= range & (t - 1);
                result = (result << 1) | (1 - t);

                if (range < kTopValue)
                {
                    code = (code << 8) | inStream.ReadByte();
                    range <<= 8;
                }
            }
            _range = range;
            _code = code;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize(IBasicInputByteStream inStream)
        {
            while (_range < kTopValue)
            {
                _code = (_code << 8) | inStream.ReadByte();
                _range <<= 8;
            }
        }
    }
}

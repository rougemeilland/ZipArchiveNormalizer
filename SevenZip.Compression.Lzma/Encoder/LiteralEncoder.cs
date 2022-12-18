// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using SevenZip.Compression.Lzma.Encoder.RangeEncoder;
using System;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Encoder
{
    class LiteralEncoder
    {
        private class Encoder2
            : ISubLiteralEncoder
        {
            private readonly BitEncoder[] _encoders;

            public Encoder2()
            {
                _encoders = new BitEncoder[0x300];
                _encoders.FillArray(_ => new BitEncoder());
            }

            public void Encode(IBasicOutputByteStream outStream, RangeEncoder.Encoder rangeEncoder, byte symbol)
            {
                var context = 1U;
                for (var i = 7; i >= 0; i--)
                {
                    var bit = (UInt32)((symbol >> i) & 1);
                    _encoders[context].Encode(outStream, rangeEncoder, bit);
                    context = (context << 1) | bit;
                }
            }

            public void EncodeMatched(IBasicOutputByteStream outStream, RangeEncoder.Encoder rangeEncoder, byte matchByte, byte symbol)
            {
                var context = 1U;
                bool same = true;
                for (var i = 7; i >= 0; i--)
                {
                    var bit = (UInt32)((symbol >> i) & 1);
                    var state = context;
                    if (same)
                    {
                        var matchBit = (UInt32)((matchByte >> i) & 1);
                        state += (1 + matchBit) << 8;
                        same = matchBit == bit;
                    }
                    _encoders[state].Encode(outStream, rangeEncoder, bit);
                    context = (context << 1) | bit;
                }
            }

            public UInt32 GetPrice(bool matchMode, byte matchByte, byte symbol)
            {
                var price = 0U;
                var context = 1U;
                var i = 7;
                if (matchMode)
                {
                    for (; i >= 0; i--)
                    {
                        var matchBit = (UInt32)(matchByte >> i) & 1;
                        var bit = (UInt32)(symbol >> i) & 1;
                        price += _encoders[((1 + matchBit) << 8) + context].GetPrice(bit);
                        context = (context << 1) | bit;
                        if (matchBit != bit)
                        {
                            i--;
                            break;
                        }
                    }
                }
                for (; i >= 0; i--)
                {
                    var bit = (UInt32)(symbol >> i) & 1;
                    price += _encoders[context].GetPrice(bit);
                    context = (context << 1) | bit;
                }
                return price;
            }
        }

        private readonly Encoder2[] _coders;
        private readonly Int32 _numPrevBits;
        private readonly Int32 _numPosBits;
        private readonly UInt32 _posMask;

        public LiteralEncoder(Int32 numPosBits, Int32 numPrevBits)
        {
            if (_coders != null && _numPrevBits == numPrevBits && _numPosBits == numPosBits)
                return;
            _numPosBits = numPosBits;
            _posMask = (1U << numPosBits) - 1;
            _numPrevBits = numPrevBits;
            var numStates = 1U << (_numPrevBits + _numPosBits);
            _coders = new Encoder2[numStates];
            _coders.FillArray(_ => new Encoder2());
        }

        public ISubLiteralEncoder GetSubCoder(UInt32 pos, Byte prevByte) =>
            _coders[((pos & _posMask) << _numPrevBits) + (UInt32)(prevByte >> (8 - _numPrevBits))];
    }
}

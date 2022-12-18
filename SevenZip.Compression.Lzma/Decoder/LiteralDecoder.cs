// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using SevenZip.Compression.Lzma.Decoder.RangeDecoder;
using System;
using System.Runtime.CompilerServices;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Decoder
{
    class LiteralDecoder
    {
        class Decoder2
        {
            private readonly BitDecoder[] _decoders;

            public Decoder2()
            {
                _decoders = new BitDecoder[0x300];
                _decoders.FillArray(_ => new BitDecoder());
            }

            public byte DecodeNormal(IBasicInputByteStream inStream, RangeDecoder.Decoder rangeDecoder)
            {
                var symbol = 1U;
                do
                    symbol = (symbol << 1) | _decoders[symbol].Decode(inStream, rangeDecoder);
                while (symbol < 0x100);
                return (byte)symbol;
            }

            public byte DecodeWithMatchByte(IBasicInputByteStream inStream, RangeDecoder.Decoder rangeDecoder, byte matchByte)
            {
                var symbol = 1U;
                do
                {
                    var matchBit = (UInt32)(matchByte >> 7) & 1;
                    matchByte <<= 1;
                    var bit = _decoders[((1 + matchBit) << 8) + symbol].Decode(inStream, rangeDecoder);
                    symbol = (symbol << 1) | bit;
                    if (matchBit != bit)
                    {
                        while (symbol < 0x100)
                            symbol = (symbol << 1) | _decoders[symbol].Decode(inStream, rangeDecoder);
                        break;
                    }
                }
                while (symbol < 0x100);
                return (byte)symbol;
            }
        }

        private readonly Decoder2[] _coders;
        private readonly Int32 _numPrevBits;
        private readonly Int32 _numPosBits;
        private readonly UInt32 _posMask;

        public LiteralDecoder(Int32 numPosBits, Int32 numPrevBits)
        {
            _numPosBits = numPosBits;
            _posMask = (1U << numPosBits) - 1;
            _numPrevBits = numPrevBits;
            _coders = new Decoder2[1U << (_numPrevBits + _numPosBits)];
            _coders.FillArray(_ => new Decoder2());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte DecodeNormal(IBasicInputByteStream inStream, RangeDecoder.Decoder rangeDecoder, UInt32 pos, byte prevByte) =>
            _coders[GetState(pos, prevByte)].DecodeNormal(inStream, rangeDecoder);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte DecodeWithMatchByte(IBasicInputByteStream inStream, RangeDecoder.Decoder rangeDecoder, UInt32 pos, byte prevByte, byte matchByte) =>
            _coders[GetState(pos, prevByte)].DecodeWithMatchByte(inStream, rangeDecoder, matchByte);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UInt32 GetState(UInt32 pos, byte prevByte) =>
            ((pos & _posMask) << _numPrevBits) + (UInt32)(prevByte >> (8 - _numPrevBits));
    }
}

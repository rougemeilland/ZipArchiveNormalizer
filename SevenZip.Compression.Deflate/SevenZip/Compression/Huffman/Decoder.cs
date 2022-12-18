// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using System.Runtime.CompilerServices;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Huffman
{
    class Decoder
    {
        private const Int32 _kNumPairLenBits = 4;
        private const Int32 _kPairLenMask = (1 << _kNumPairLenBits) - 1;
        private const Int32 _kNumTableBits_default = 9;

        private readonly Int32 _kNumBitsMax;
        private readonly UInt32 _numSymbols;
        private readonly Int32 _kNumTableBits;
        private readonly UInt32[] _limits;
        private readonly UInt32[] _poses;
        private readonly UInt16[] _lens;
        private readonly UInt16[] _symbols;

        public Decoder(Int32 kNumBitsMax, UInt32 numSymbols, Int32 kNumTableBits = _kNumTableBits_default)
        {
            _kNumBitsMax = kNumBitsMax;
            _numSymbols = numSymbols;
            _kNumTableBits = kNumTableBits;
            _limits = new UInt32[kNumBitsMax + 2];
            _poses = new UInt32[kNumBitsMax + 1];
            _lens = new UInt16[1 << kNumTableBits];
            _symbols = new UInt16[numSymbols];
        }

        public bool Build(ReadOnlySpan<Byte> lens)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            Span<UInt32> counts = stackalloc UInt32[_kNumBitsMax + 1];
            counts.Clear();
            for (var sym = 0; sym < _numSymbols; sym++)
                ++counts[lens[sym]];
            var kMaxValue = 1U << _kNumBitsMax;
            _limits[0] = 0;
            var startPos = 0U;
            var sum = 0U;
            for (var i = 1; i <= _kNumBitsMax; i++)
            {
                var cnt = counts[i];
                startPos += cnt << (_kNumBitsMax - i);
                if (startPos > kMaxValue)
                    return false;
                _limits[i] = startPos;
                counts[i] = sum;
                _poses[i] = sum;
                sum += cnt;
            }
            counts[0] = sum;
            _poses[0] = sum;
            _limits[_kNumBitsMax + 1] = kMaxValue;
            for (var sym = 0U; sym < _numSymbols; sym++)
            {
                var len = lens[(Int32)sym];
                if (len > 0)
                {
                    var offset = counts[len]++;
                    _symbols[offset] = (UInt16)sym;
                    if (len <= _kNumTableBits)
                    {
                        offset -= _poses[len];
                        _lens.FillArray(
                            (UInt16)((sym << _kNumPairLenBits) | len),
                            (_limits[len - 1] >> (_kNumBitsMax - _kNumTableBits)) + (offset << (_kNumTableBits - len)),
                            1U << (_kNumTableBits - len));
                    }
                }
            }
            return true;
        }

        public bool BuildFull(ReadOnlyArrayPointer<Byte> lens)
        {
            return BuildFull(lens, _numSymbols);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool BuildFull(ReadOnlyArrayPointer<Byte> lens, UInt32 numSymbols)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            Span<UInt32> counts = stackalloc UInt32[_kNumBitsMax + 1];
            for (var i = 0; i <= _kNumBitsMax; i++)
                counts[i] = 0;
            for (var sym = 0; sym < numSymbols; sym++)
                ++counts[lens[sym]];
            var kMaxValue = 1U << _kNumBitsMax;
            _limits[0] = 0;
            var startPos = 0U;
            var sum = 0U;
            for (var i = 1; i <= _kNumBitsMax; i++)
            {
                var cnt = counts[i];
                startPos += cnt << (_kNumBitsMax - i);
                if (startPos > kMaxValue)
                    return false;
                _limits[i] = startPos;
                counts[i] = sum;
                _poses[i] = sum;
                sum += cnt;
            }
            counts[0] = sum;
            _poses[0] = sum;
            _limits[_kNumBitsMax + 1] = kMaxValue;
            for (var sym = 0; sym < numSymbols; sym++)
            {
                var len = lens[sym];
                if (len != 0)
                {
                    var offset = counts[len]++;
                    _symbols[offset] = (UInt16)sym;
                    if (len <= _kNumTableBits)
                    {
                        offset -= _poses[len];
                        var num = 1U << (_kNumTableBits - len);
                        var val = (UInt16)((sym << _kNumPairLenBits) | len);
                        _lens.FillArray(
                            (UInt16)((sym << _kNumPairLenBits) | len),
                            (_limits[len - 1] >> (_kNumBitsMax - _kNumTableBits)) + (offset << (_kNumTableBits - len)),
                            1U << (_kNumTableBits - len));
                    }
                }
            }
            return startPos == kMaxValue;
        }

        public UInt32 Decode<BITDECODER_T>(IBasicInputByteStream inStream, BITDECODER_T bitStream)
            where BITDECODER_T : IBitDecoder
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));
            if (bitStream is null)
                throw new ArgumentNullException(nameof(bitStream));

            var val = bitStream.GetValue(inStream, _kNumBitsMax);
            if (val < _limits[_kNumTableBits])
            {
                var pair = _lens[val >> (_kNumBitsMax - _kNumTableBits)];
                bitStream.MovePos(pair & _kPairLenMask);
                return (UInt32)pair >> _kNumPairLenBits;
            }
            var numBits = _kNumTableBits + 1;
            while (val >= _limits[numBits])
                ++numBits;
            if (numBits > _kNumBitsMax)
                return UInt32.MaxValue;
            bitStream.MovePos(numBits);
            var index = _poses[numBits] + ((val - _limits[numBits - 1]) >> (_kNumBitsMax - numBits));
            return _symbols[index];
        }

        public UInt32 DecodeFull<BITDECODER_T>(IBasicInputByteStream inStream, BITDECODER_T bitStream)
            where BITDECODER_T : IBitDecoder
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));
            if (bitStream is null)
                throw new ArgumentNullException(nameof(bitStream));

            var val = bitStream.GetValue(inStream, _kNumBitsMax);
            if (val < _limits[_kNumTableBits])
            {
                var pair = _lens[val >> (_kNumBitsMax - _kNumTableBits)];
                bitStream.MovePos(pair & _kPairLenMask);
                return (UInt32)pair >> _kNumPairLenBits;
            }
            var numBits = _kNumTableBits + 1;
            while (val >= _limits[numBits])
                ++numBits;
            bitStream.MovePos(numBits);
            var index = _poses[numBits] + ((val - _limits[numBits - 1]) >> (_kNumBitsMax - numBits));
            return _symbols[index];
        }
    }
}

// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Huffman
{
    class Decoder7b
    {
        private readonly UInt32 _numSymbols;
        private readonly Byte[] _lens;
        public Decoder7b(UInt32 numSymbols)
        {
            _numSymbols = numSymbols;
            _lens = new Byte[1 << 7];
        }

        public bool Build(ReadOnlySpan<Byte> lens)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            var kNumBitsMax = 7;
            Span<UInt32> counts = stackalloc UInt32[kNumBitsMax + 1];
            Span<UInt32> _poses = stackalloc UInt32[kNumBitsMax + 1];
            Span<UInt32> limits = stackalloc UInt32[kNumBitsMax + 1];
            counts.Fill(0);
            for (var sym = 0; sym < _numSymbols; sym++)
                ++counts[lens[sym]];
            var kMaxValue = 1U << kNumBitsMax;
            limits[0] = 0;
            var startPos = 0U;
            var sum = 0U;
            for (var i = 1; i <= kNumBitsMax; i++)
            {
                var cnt = counts[i];
                startPos += cnt << (kNumBitsMax - i);
                if (startPos > kMaxValue)
                    return false;
                limits[i] = startPos;
                counts[i] = sum;
                _poses[i] = sum;
                sum += cnt;
            }
            counts[0] = sum;
            _poses[0] = sum;
            for (var sym = 0; sym < _numSymbols; sym++)
            {
                var len = lens[sym];
                if (len > 0)
                {
                    var offset = counts[len]++;
                    offset -= _poses[len];
                    _lens.FillArray(
                        (Byte)((sym << 3) | len),
                        limits[len - 1] + (offset << (kNumBitsMax - len)),
                        1U << (kNumBitsMax - len));
                }
            }
            var limit = limits[kNumBitsMax];
            _lens.FillArray(
                (Byte)(0x1F << 3),
                limit,
                (1U << kNumBitsMax) - limit);
            return true;
        }

        public UInt32 Decode<BITDECODER_T>(IBasicInputByteStream inStream, BITDECODER_T bitStream)
                  where BITDECODER_T : IBitDecoder
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));
            if (bitStream is null)
                throw new ArgumentNullException(nameof(bitStream));

            var val = bitStream.GetValue(inStream, 7);
            var pair = _lens[val];
            bitStream.MovePos(pair & 0x7);
            return (UInt32)pair >> 3;
        }
    }
}

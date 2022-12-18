// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility.IO;

namespace SevenZip.Compression.BitLsb
{
    class Decoder<TInByte>
        : BaseDecoder<TInByte>, Huffman.IBitDecoder
        where TInByte : IInBuffer
    {
        private UInt32 _normalValue;

        public Decoder(TInByte stream)
            : base(stream)
        {
            _normalValue = 0;

        }
        public UInt32 GetValue(IBasicInputByteStream inStream, Int32 numBits)
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));

            Normalize(inStream);
            return ((_value >> (8 - _bitPos)) & BitLsbConstants.kMask) >> (BitLsbConstants.kNumValueBits - numBits);
        }

        public override UInt32 ReadBits(IBasicInputByteStream inStream, Int32 numBits)
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));

            Normalize(inStream);
            var result = _normalValue & ((1U << numBits) - 1);
            MovePos(numBits);
            return result;
        }

        public override void Normalize(IBasicInputByteStream inStream)
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));

            var kInvertTable = BitLsbConstants.kInvertTable.Span;
            while (_bitPos >= 8)
            {
                var b = _stream.ReadByte(inStream);
                _normalValue = ((UInt32)b << (BitLsbConstants.kNumBigValueBits - _bitPos)) | _normalValue;
                _value = (_value << 8) | kInvertTable[b];
                _bitPos -= 8;
            }
        }

        public void AlignToByte() => MovePos((32 - _bitPos) & 7);

        public Byte ReadDirectByte(IBasicInputByteStream inStream)
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));

            return _stream.ReadByte(inStream);
        }

        public Byte ReadAlignedByte(IBasicInputByteStream inStream)
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));

            if (_bitPos == BitLsbConstants.kNumBigValueBits)
                return _stream.ReadByte(inStream);
            var b = (Byte)(_normalValue & Byte.MaxValue);
            MovePos(8);
            return b;
        }

        public bool ReadAlignedByteFromBuf(out Byte b)
        {
            if (_bitPos == BitLsbConstants.kNumBigValueBits)
                return _stream.ReadByteFromBuf(out b);
            b = (Byte)(_normalValue & Byte.MaxValue);
            MovePos(8);
            return true;
        }

        public void MovePos(Int32 numBits)
        {
            _bitPos += numBits;
            _normalValue >>= numBits;
        }
    }
}

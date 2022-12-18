// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility.IO;

namespace SevenZip.Compression.BitLsb
{
    class Encoder
    {
        private Int32 _bitPos;
        private Byte _curByte;
        private UInt64 _processedCount;

        public Encoder()
        {
            _bitPos = 8;
            _curByte = 0;
            _processedCount = 0;
        }

        public UInt64 ProcessedSize => _processedCount + (UInt32)((8 - _bitPos + 7) >> 3);

        public void Flush(IBasicOutputByteStream outStream)
        {
            if (outStream is null)
                throw new ArgumentNullException(nameof(outStream));

            FlushByte(outStream);
            outStream.Flush();
        }
        public void FlushByte(IBasicOutputByteStream outStream)
        {
            if (outStream is null)
                throw new ArgumentNullException(nameof(outStream));

            if (_bitPos < 8)
                WriteByte(outStream, _curByte);
            _bitPos = 8;
            _curByte = 0;
        }

        public void WriteBits(IBasicOutputByteStream outStream, UInt32 value, Int32 numBits)
        {
            if (outStream is null)
                throw new ArgumentNullException(nameof(outStream));

            while (numBits > 0)
            {
                if (numBits < _bitPos)
                {
                    _curByte |= (Byte)((value & ((1 << numBits) - 1)) << (8 - _bitPos));
                    _bitPos -= numBits;
                    return;
                }
                numBits -= _bitPos;
                WriteByte(outStream, (Byte)(_curByte | (value << (8 - _bitPos))));
                value >>= _bitPos;
                _bitPos = 8;
                _curByte = 0;
            }
        }

        public void WriteByte(IBasicOutputByteStream outStream, Byte b)
        {
            if (outStream is null)
                throw new ArgumentNullException(nameof(outStream));

            outStream.WriteByte(b);
            ++_processedCount;
        }
    }
}

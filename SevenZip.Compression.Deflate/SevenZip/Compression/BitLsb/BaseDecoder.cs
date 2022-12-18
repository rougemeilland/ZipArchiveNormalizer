// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility.IO;

namespace SevenZip.Compression.BitLsb
{
    abstract class BaseDecoder<IN_BUFFER_T>
        where IN_BUFFER_T : IInBuffer
    {
        protected Int32 _bitPos;
        protected UInt32 _value;
        protected IN_BUFFER_T _stream;

        protected BaseDecoder(IN_BUFFER_T inByteStream)
        {
            _stream = inByteStream ?? throw new ArgumentNullException(nameof(inByteStream));
            _bitPos = BitLsbConstants.kNumBigValueBits;
            _value = 0;
        }

        public UInt64 StreamSize => ExtraBitsWereRead ? _stream.StreamSize : ProcessedSize;
        public UInt64 ProcessedSize => _stream.ProcessedSize - ((UInt32)(BitLsbConstants.kNumBigValueBits - _bitPos) >> 3);
        public bool ThereAreDataInBitsBuffer => _bitPos != BitLsbConstants.kNumBigValueBits;
        public bool ExtraBitsWereRead => _stream.NumExtraBytes > 4 || BitLsbConstants.kNumBigValueBits - _bitPos < (_stream.NumExtraBytes << 3);
        public bool ExtraBitsWereReadFast => _stream.NumExtraBytes > 4;

        public virtual UInt32 ReadBits(IBasicInputByteStream inStream, Int32 numBits)
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));

            Normalize(inStream);
            var result = _value & ((1U << numBits) - 1);
            _bitPos += numBits;
            _value >>= numBits;
            return result;
        }

        public virtual void Normalize(IBasicInputByteStream inStream)
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));

            while (_bitPos >= 8)
            {
                _value |= (UInt32)_stream.ReadByte(inStream) << (BitLsbConstants.kNumBigValueBits - _bitPos);
                _bitPos -= 8;
            }
        }
    }
}

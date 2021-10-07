using System;

namespace Utility.IO.Compression.Lzma.RangeCoder
{
    class RangeDecoder
    {
        public const uint kTopValue = (1 << 24);
        public uint Range;
        public uint Code;
        private IBasicInputByteStream _stream;

        public void Init(IBasicInputByteStream stream)
        {
            _stream = stream;
            Code = 0;
            Range = UInt32.MaxValue;
            for (int i = 0; i < 5; i++)
                Code = (Code << 8) | ReadByte();
        }

        public void ReleaseStream()
        {
            _stream = null;
        }

        public uint DecodeDirectBits(int numTotalBits)
        {
            uint range = Range;
            uint code = Code;
            uint result = 0;
            for (int i = numTotalBits; i > 0; i--)
            {
                range >>= 1;
                uint t = (code - range) >> 31;
                code -= range & (t - 1);
                result = (result << 1) | (1 - t);

                if (range < kTopValue)
                {
                    code = (code << 8) | ReadByte();
                    range <<= 8;
                }
            }
            Range = range;
            Code = code;
            return result;
        }

        public byte ReadByte()
        {
            var buffer = new byte[1];
            var length = _stream.Read(buffer, 0, buffer.Length);
            if (length <= 0)
                throw new DataErrorException("Unexpected end of stream");
            return buffer[0];

        }
    }
}

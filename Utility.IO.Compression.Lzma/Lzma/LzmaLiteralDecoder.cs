using Utility.IO.Compression.RangeCoder;

namespace Utility.IO.Compression.Lzma
{
    class LzmaLiteralDecoder
    {
        struct Decoder2
        {
            private BitDecoder[] m_Decoders;
            public void Create() { m_Decoders = new BitDecoder[0x300]; }
            public void Init() { for (int i = 0; i < 0x300; i++) m_Decoders[i].Init(); }

            public byte DecodeNormal(RangeCoder.RangeDecoder rangeDecoder)
            {
                var symbol = 1U;
                do
                    symbol = symbol.ConcatBit(m_Decoders[symbol].Decode(rangeDecoder));
                while (symbol < 0x100);
                return (byte)symbol;
            }

            public byte DecodeWithMatchByte(RangeCoder.RangeDecoder rangeDecoder, byte matchByte)
            {
                uint symbol = 1;
                do
                {
                    var matchBit = ((matchByte >> 7) & 1) != 0;
                    matchByte <<= 1;
                    var bit = m_Decoders[(matchBit ? 0x200 : 0x100) + symbol].Decode(rangeDecoder);
                    symbol = symbol.ConcatBit(bit);
                    if (matchBit != bit)
                    {
                        while (symbol < 0x100)
                            symbol = symbol.ConcatBit(m_Decoders[symbol].Decode(rangeDecoder));
                        break;
                    }
                }
                while (symbol < 0x100);
                return (byte)symbol;
            }
        }

        private Decoder2[] m_Coders;
        private int m_NumPrevBits;
        private int m_NumPosBits;
        private uint m_PosMask;

        public void Create(int numPosBits, int numPrevBits)
        {
            if (m_Coders != null && m_NumPrevBits == numPrevBits &&
                m_NumPosBits == numPosBits)
                return;
            m_NumPosBits = numPosBits;
            m_PosMask = ((uint)1 << numPosBits) - 1;
            m_NumPrevBits = numPrevBits;
            uint numStates = (uint)1 << (m_NumPrevBits + m_NumPosBits);
            m_Coders = new Decoder2[numStates];
            for (uint i = 0; i < numStates; i++)
                m_Coders[i].Create();
        }

        public void Init()
        {
            uint numStates = (uint)1 << (m_NumPrevBits + m_NumPosBits);
            for (uint i = 0; i < numStates; i++)
                m_Coders[i].Init();
        }

        uint GetState(uint pos, byte prevByte)
        { return ((pos & m_PosMask) << m_NumPrevBits) + (uint)(prevByte >> (8 - m_NumPrevBits)); }

        public byte DecodeNormal(RangeCoder.RangeDecoder rangeDecoder, uint pos, byte prevByte)
        { return m_Coders[GetState(pos, prevByte)].DecodeNormal(rangeDecoder); }

        public byte DecodeWithMatchByte(RangeCoder.RangeDecoder rangeDecoder, uint pos, byte prevByte, byte matchByte)
        { return m_Coders[GetState(pos, prevByte)].DecodeWithMatchByte(rangeDecoder, matchByte); }
    };

}

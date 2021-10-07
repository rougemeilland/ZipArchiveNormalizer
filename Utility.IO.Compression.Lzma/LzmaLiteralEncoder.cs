using System;
using Utility.IO.Compression.Lzma.RangeCoder;

namespace Utility.IO.Compression.Lzma
{
    class LzmaLiteralEncoder
    {
        public struct Encoder2
        {
            BitEncoder[] m_Encoders;

            public void Create() { m_Encoders = new BitEncoder[0x300]; }

            public void Init() { for (int i = 0; i < 0x300; i++) m_Encoders[i].Init(); }

            public void Encode(RangeCoder.RangeEncoder rangeEncoder, byte symbol)
            {
                uint context = 1;
                for (int i = 7; i >= 0; i--)
                {
                    var bit = ((symbol >> i) & 1) != 0;
                    m_Encoders[context].Encode(rangeEncoder, bit);
                    context = context.ConcatBit(bit);
                }
            }

            public void EncodeMatched(RangeCoder.RangeEncoder rangeEncoder, byte matchByte, byte symbol)
            {
                var context = 1U;
                var same = true;
                for (int i = 7; i >= 0; i--)
                {
                    var bit = ((symbol >> i) & 1) != 0;
                    var state = context;
                    if (same)
                    {
                        var matchBit = ((matchByte >> i) & 1) != 0;
                        state += matchBit ? 0x200U : 0x100U;
                        same = matchBit == bit;
                    }
                    m_Encoders[state].Encode(rangeEncoder, bit);
                    context = context.ConcatBit(bit);
                }
            }

            public uint GetPrice(bool matchMode, byte matchByte, byte symbol)
            {
                var price = 0U;
                var context = 1U;
                var i = 7;
                if (matchMode)
                {
                    for (; i >= 0; i--)
                    {
                        var matchBit = ((matchByte >> i) & 1) != 0;
                        var bit = ((symbol >> i) & 1) != 0;
                        price += m_Encoders[(matchBit ? 0x200 : 0x100) + context].GetPrice(bit);
                        context = context.ConcatBit(bit);
                        if (matchBit != bit)
                        {
                            i--;
                            break;
                        }
                    }
                }
                for (; i >= 0; i--)
                {
                    var bit = ((symbol >> i) & 1) != 0;
                    price += m_Encoders[context].GetPrice(bit);
                    context = context.ConcatBit(bit);
                }
                return price;
            }
        }

        private Encoder2[] m_Coders;
        private int m_NumPrevBits;
        private int m_NumPosBits;
        private uint m_PosMask;

        public void Create(int numPosBits, int numPrevBits)
        {
            if (m_Coders != null && m_NumPrevBits == numPrevBits && m_NumPosBits == numPosBits)
                return;
            m_NumPosBits = numPosBits;
            m_PosMask = ((uint)1 << numPosBits) - 1;
            m_NumPrevBits = numPrevBits;
            uint numStates = (uint)1 << (m_NumPrevBits + m_NumPosBits);
            m_Coders = new Encoder2[numStates];
            for (uint i = 0; i < numStates; i++)
                m_Coders[i].Create();
        }

        public void Init()
        {
            uint numStates = (uint)1 << (m_NumPrevBits + m_NumPosBits);
            for (uint i = 0; i < numStates; i++)
                m_Coders[i].Init();
        }

        public Encoder2 GetSubCoder(UInt32 pos, Byte prevByte)
        { return m_Coders[((pos & m_PosMask) << m_NumPrevBits) + (uint)(prevByte >> (8 - m_NumPrevBits))]; }
    }
}

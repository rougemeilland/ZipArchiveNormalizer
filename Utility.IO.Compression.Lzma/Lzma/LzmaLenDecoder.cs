using Utility.IO.Compression.RangeCoder;

namespace Utility.IO.Compression.Lzma
{
    class LzmaLenDecoder
    {
        private BitDecoder m_Choice;
        private BitDecoder m_Choice2;
        private BitTreeDecoder[] m_LowCoder;
        private BitTreeDecoder[] m_MidCoder;
        private BitTreeDecoder m_HighCoder;
        private uint m_NumPosStates;

        public LzmaLenDecoder()
        {
            m_Choice = new BitDecoder();
            m_Choice2 = new BitDecoder();
            m_LowCoder = new BitTreeDecoder[LzmaCoder.kNumPosStatesMax];
            m_MidCoder = new BitTreeDecoder[LzmaCoder.kNumPosStatesMax];
            m_HighCoder = new BitTreeDecoder(LzmaCoder.kNumHighLenBits);
            m_NumPosStates = 0;
        }

        public void Create(uint numPosStates)
        {
            for (uint posState = m_NumPosStates; posState < numPosStates; posState++)
            {
                m_LowCoder[posState] = new BitTreeDecoder(LzmaCoder.kNumLowLenBits);
                m_MidCoder[posState] = new BitTreeDecoder(LzmaCoder.kNumMidLenBits);
            }
            m_NumPosStates = numPosStates;
        }

        public void Init()
        {
            m_Choice.Init();
            for (uint posState = 0; posState < m_NumPosStates; posState++)
            {
                m_LowCoder[posState].Init();
                m_MidCoder[posState].Init();
            }
            m_Choice2.Init();
            m_HighCoder.Init();
        }

        public uint Decode(RangeCoder.RangeDecoder rangeDecoder, uint posState)
        {
            if (m_Choice.Decode(rangeDecoder) == false)
                return m_LowCoder[posState].Decode(rangeDecoder);
            else
            {
                uint symbol = LzmaCoder.kNumLowLenSymbols;
                if (m_Choice2.Decode(rangeDecoder) == false)
                    symbol += m_MidCoder[posState].Decode(rangeDecoder);
                else
                {
                    symbol += LzmaCoder.kNumMidLenSymbols;
                    symbol += m_HighCoder.Decode(rangeDecoder);
                }
                return symbol;
            }
        }
    }

}

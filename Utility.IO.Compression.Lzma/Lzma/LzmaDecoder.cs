using System;
using Utility.IO.Compression.RangeCoder;

namespace Utility.IO.Compression.Lzma
{
    public class LzmaDecoder
        : ISetDecoderProperties
    {
        private const bool _solid = false;

        private Lz.OutWindow m_OutWindow;
        private RangeCoder.RangeDecoder m_RangeDecoder;

        private BitDecoder[] m_IsMatchDecoders;
        private BitDecoder[] m_IsRepDecoders;
        private BitDecoder[] m_IsRepG0Decoders;
        private BitDecoder[] m_IsRepG1Decoders;
        private BitDecoder[] m_IsRepG2Decoders;
        private BitDecoder[] m_IsRep0LongDecoders;

        private BitTreeDecoder[] m_PosSlotDecoder;
        private BitDecoder[] m_PosDecoders;

        private BitTreeDecoder m_PosAlignDecoder;

        private LzmaLenDecoder m_LenDecoder;
        private LzmaLenDecoder m_RepLenDecoder;

        private LzmaLiteralDecoder m_LiteralDecoder;

        private uint m_DictionarySize;
        private uint m_DictionarySizeCheck;

        private uint m_PosStateMask;

        public LzmaDecoder()
        {
            m_OutWindow = new Lz.OutWindow();
            m_RangeDecoder = new RangeCoder.RangeDecoder();
            m_IsMatchDecoders = new BitDecoder[LzmaCoder.kNumStates << LzmaCoder.kNumPosStatesBitsMax];
            m_IsRepDecoders = new BitDecoder[LzmaCoder.kNumStates];
            m_IsRepG0Decoders = new BitDecoder[LzmaCoder.kNumStates];
            m_IsRepG1Decoders = new BitDecoder[LzmaCoder.kNumStates];
            m_IsRepG2Decoders = new BitDecoder[LzmaCoder.kNumStates];
            m_IsRep0LongDecoders = new BitDecoder[LzmaCoder.kNumStates << LzmaCoder.kNumPosStatesBitsMax];
            m_PosSlotDecoder = new BitTreeDecoder[LzmaCoder.kNumLenToPosStates];
            m_PosDecoders = new BitDecoder[LzmaCoder.kNumFullDistances - LzmaCoder.kEndPosModelIndex];
            m_PosAlignDecoder = new BitTreeDecoder(LzmaCoder.kNumAlignBits);
            m_LenDecoder = new LzmaLenDecoder();
            m_RepLenDecoder = new LzmaLenDecoder();
            m_LiteralDecoder = new LzmaLiteralDecoder();
            m_DictionarySizeCheck = 0;
            m_PosStateMask = 0;
            m_DictionarySize = UInt32.MaxValue;
            for (int i = 0; i < LzmaCoder.kNumLenToPosStates; i++)
                m_PosSlotDecoder[i] = new BitTreeDecoder(LzmaCoder.kNumPosSlotBits);
        }

        public void SetDecoderProperties(IReadOnlyArray<byte> properties)
        {
            if (properties.Length < 5)
                throw new ArgumentException();
            int lc = properties[0] % 9;
            int remainder = properties[0] / 9;
            int lp = remainder % 5;
            int pb = remainder / 5;
            if (pb > LzmaCoder.kNumPosStatesBitsMax)
                throw new ArgumentException();
            UInt32 dictionarySize = 0;
            for (int i = 0; i < 4; i++)
                dictionarySize += ((UInt32)(properties[1 + i])) << (i * 8);
            SetDictionarySize(dictionarySize);
            SetLiteralProperties(lp, lc);
            SetPosBitsProperties(pb);
        }

        public void Code(IInputByteStream<UInt64> inStream, IOutputByteStream<UInt64> outStream, UInt64 outSize, ICodeProgress progress)
        {
            Init(inStream, outStream);

            LzmaCoder.State state = new LzmaCoder.State();
            state.Init();
            uint rep0 = 0, rep1 = 0, rep2 = 0, rep3 = 0;

            UInt64 nowPos64 = 0;
            UInt64 outSize64 = outSize;
            if (nowPos64 < outSize64)
            {
                if (m_IsMatchDecoders[state.Index << LzmaCoder.kNumPosStatesBitsMax].Decode(m_RangeDecoder))
                    throw new DataErrorException();
                state.UpdateChar();
                byte b = m_LiteralDecoder.DecodeNormal(m_RangeDecoder, 0, 0);
                m_OutWindow.PutByte(b);
                nowPos64++;
            }
            while (nowPos64 < outSize64)
            {
                // UInt64 next = Math.Min(nowPos64 + (1 << 18), outSize64);
                // while(nowPos64 < next)
                {
                    uint posState = (uint)nowPos64 & m_PosStateMask;
                    if (m_IsMatchDecoders[(state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].Decode(m_RangeDecoder) == false)
                    {
                        byte b;
                        byte prevByte = m_OutWindow.GetByte(0);
                        if (!state.IsCharState())
                            b = m_LiteralDecoder.DecodeWithMatchByte(m_RangeDecoder,
                                (uint)nowPos64, prevByte, m_OutWindow.GetByte(rep0));
                        else
                            b = m_LiteralDecoder.DecodeNormal(m_RangeDecoder, (uint)nowPos64, prevByte);
                        m_OutWindow.PutByte(b);
                        state.UpdateChar();
                        nowPos64++;
                    }
                    else
                    {
                        uint len;
                        if (m_IsRepDecoders[state.Index].Decode(m_RangeDecoder) == true)
                        {
                            if (m_IsRepG0Decoders[state.Index].Decode(m_RangeDecoder) == false)
                            {
                                if (m_IsRep0LongDecoders[(state.Index << LzmaCoder.kNumPosStatesBitsMax) + posState].Decode(m_RangeDecoder) == false)
                                {
                                    state.UpdateShortRep();
                                    m_OutWindow.PutByte(m_OutWindow.GetByte(rep0));
                                    nowPos64++;
                                    continue;
                                }
                            }
                            else
                            {
                                UInt32 distance;
                                if (m_IsRepG1Decoders[state.Index].Decode(m_RangeDecoder) == false)
                                {
                                    distance = rep1;
                                }
                                else
                                {
                                    if (m_IsRepG2Decoders[state.Index].Decode(m_RangeDecoder) == false)
                                        distance = rep2;
                                    else
                                    {
                                        distance = rep3;
                                        rep3 = rep2;
                                    }
                                    rep2 = rep1;
                                }
                                rep1 = rep0;
                                rep0 = distance;
                            }
                            len = m_RepLenDecoder.Decode(m_RangeDecoder, posState) + LzmaCoder.kMatchMinLen;
                            state.UpdateRep();
                        }
                        else
                        {
                            rep3 = rep2;
                            rep2 = rep1;
                            rep1 = rep0;
                            len = LzmaCoder.kMatchMinLen + m_LenDecoder.Decode(m_RangeDecoder, posState);
                            state.UpdateMatch();
                            uint posSlot = m_PosSlotDecoder[LzmaCoder.GetLenToPosState(len)].Decode(m_RangeDecoder);
                            if (posSlot >= LzmaCoder.kStartPosModelIndex)
                            {
                                int numDirectBits = (int)((posSlot >> 1) - 1);
                                rep0 = ((2 | (posSlot & 1)) << numDirectBits);
                                if (posSlot < LzmaCoder.kEndPosModelIndex)
                                    rep0 += BitTreeDecoder.ReverseDecode(m_PosDecoders,
                                            rep0 - posSlot - 1, m_RangeDecoder, numDirectBits);
                                else
                                {
                                    rep0 += (m_RangeDecoder.DecodeDirectBits(
                                        numDirectBits - LzmaCoder.kNumAlignBits) << LzmaCoder.kNumAlignBits);
                                    rep0 += m_PosAlignDecoder.ReverseDecode(m_RangeDecoder);
                                }
                            }
                            else
                                rep0 = posSlot;
                        }
                        if (rep0 >= nowPos64 || rep0 >= m_DictionarySizeCheck)
                        {
                            if (rep0 == UInt32.MaxValue)
                                break;
                            throw new DataErrorException();
                        }
                        m_OutWindow.CopyBlock(rep0, len);
                        nowPos64 += len;
                    }
                }
            }
            m_OutWindow.Flush();
            m_OutWindow.ReleaseStream();
            m_RangeDecoder.ReleaseStream();
            outStream.Close();
        }

        private void SetDictionarySize(uint dictionarySize)
        {
            if (m_DictionarySize != dictionarySize)
            {
                m_DictionarySize = dictionarySize;
                m_DictionarySizeCheck = Math.Max(m_DictionarySize, 1);
                uint blockSize = Math.Max(m_DictionarySizeCheck, (1 << 12));
                m_OutWindow.Create(blockSize);
            }
        }

        private void SetLiteralProperties(int lp, int lc)
        {
            if (lp > 8)
                throw new ArgumentException();
            if (lc > 8)
                throw new ArgumentException();
            m_LiteralDecoder.Create(lp, lc);
        }

        private void SetPosBitsProperties(int pb)
        {
            if (pb > LzmaCoder.kNumPosStatesBitsMax)
                throw new ArgumentException();
            uint numPosStates = (uint)1 << pb;
            m_LenDecoder.Create(numPosStates);
            m_RepLenDecoder.Create(numPosStates);
            m_PosStateMask = numPosStates - 1;
        }

        private void Init(IInputByteStream<UInt64> inStream, IOutputByteStream<UInt64> outStream)
        {
            m_RangeDecoder.Init(inStream);
            m_OutWindow.Init(outStream, _solid);

            uint i;
            for (i = 0; i < LzmaCoder.kNumStates; i++)
            {
                for (uint j = 0; j <= m_PosStateMask; j++)
                {
                    uint index = (i << LzmaCoder.kNumPosStatesBitsMax) + j;
                    m_IsMatchDecoders[index].Init();
                    m_IsRep0LongDecoders[index].Init();
                }
                m_IsRepDecoders[i].Init();
                m_IsRepG0Decoders[i].Init();
                m_IsRepG1Decoders[i].Init();
                m_IsRepG2Decoders[i].Init();
            }

            m_LiteralDecoder.Init();
            for (i = 0; i < LzmaCoder.kNumLenToPosStates; i++)
                m_PosSlotDecoder[i].Init();
            // m_PosSpecDecoder.Init();
            for (i = 0; i < LzmaCoder.kNumFullDistances - LzmaCoder.kEndPosModelIndex; i++)
                m_PosDecoders[i].Init();

            m_LenDecoder.Init();
            m_RepLenDecoder.Init();
            m_PosAlignDecoder.Init();
        }
    }
}

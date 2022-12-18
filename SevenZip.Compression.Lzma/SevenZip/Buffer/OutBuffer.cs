// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

#nullable disable

using Utility.IO;

namespace SevenZip.Buffer
{
    class OutBuffer
    {
        byte[] m_Buffer;
        uint m_Pos;
        uint m_BufferSize;
        IBasicOutputByteStream m_Stream;
        ulong m_ProcessedSize;

        public OutBuffer(uint bufferSize)
        {
            m_Buffer = new byte[bufferSize];
            m_BufferSize = bufferSize;
        }

        public void SetStream(IBasicOutputByteStream stream) { m_Stream = stream; }
        public void FlushStream() { m_Stream.Flush(); }
        public void CloseStream() { /*m_Stream.Close();*/ }
        public void ReleaseStream() { m_Stream = null; }

        public void Init()
        {
            m_ProcessedSize = 0;
            m_Pos = 0;
        }

        public void WriteByte(byte b)
        {
            m_Buffer[m_Pos++] = b;
            if (m_Pos >= m_BufferSize)
                FlushData();
        }

        public void FlushData()
        {
            if (m_Pos == 0)
                return;
            m_Stream.Write(m_Buffer, 0, (int)m_Pos);
            m_Pos = 0;
        }

        public ulong GetProcessedSize() { return m_ProcessedSize + m_Pos; }
    }
}

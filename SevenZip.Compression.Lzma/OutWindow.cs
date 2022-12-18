// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using System;
using Utility.IO;

namespace SevenZip.Compression.Lzma
{
    public class OutWindow
    {
        private Byte[] _buffer;
        private UInt32 _pos;
        private UInt32 _windowSize;
        private UInt32 _streamPos;

        public OutWindow(UInt32 windowSize)
        {
            _buffer = new Byte[windowSize];
            _windowSize = windowSize;
            _pos = 0;
            _streamPos = 0;
        }
        public void CopyBlock(IBasicOutputByteStream outStream, UInt32 distance, UInt32 len)
        {
            var pos = _pos - distance - 1;
            if (pos >= _windowSize)
                pos += _windowSize;
            while (len > 0)
            {
                if (pos >= _windowSize)
                    pos = 0;
                _buffer[_pos++] = _buffer[pos++];
                if (_pos >= _windowSize)
                    Flush(outStream);
                --len;
            }
        }

        public void PutByte(IBasicOutputByteStream outStream, Byte b)
        {
            _buffer[_pos++] = b;
            if (_pos >= _windowSize)
                Flush(outStream);
        }

        public Byte GetByte(UInt32 distance)
        {
            UInt32 pos = _pos - distance - 1;
            if (pos >= _windowSize)
                pos += _windowSize;
            return _buffer[pos];
        }

        public void Flush(IBasicOutputByteStream outStream)
        {
            var size = _pos - _streamPos;
            if (size == 0)
                return;
            outStream.WriteBytes(_buffer, (Int32)_streamPos, (Int32)size);
            if (_pos >= _windowSize)
                _pos = 0;
            _streamPos = _pos;
        }

    }
}

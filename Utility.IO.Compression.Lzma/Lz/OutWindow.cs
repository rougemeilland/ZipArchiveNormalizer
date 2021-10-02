using System;

namespace Utility.IO.Compression.Lz
{
    class OutWindow
    {
        private byte[] _buffer = null;
        private uint _pos;
        private uint _windowSize = 0;
        private uint _streamPos;
        private IOutputByteStream<UInt64> _stream;

        public void Create(uint windowSize)
        {
            if (_windowSize != windowSize)
                _buffer = new byte[windowSize];
            _windowSize = windowSize;
            _pos = 0;
            _streamPos = 0;
        }

        public void Init(IOutputByteStream<UInt64> stream, bool solid)
        {
            ReleaseStream();
            _stream = stream;
            if (!solid)
            {
                _streamPos = 0;
                _pos = 0;
            }
        }

        public void ReleaseStream()
        {
            Flush();
            _stream = null;
        }

        public void Flush()
        {
            uint size = _pos - _streamPos;
            if (size == 0)
                return;
            _stream.WriteBytes(_buffer.AsReadOnly(), (int)_streamPos, (int)size);
            if (_pos >= _windowSize)
                _pos = 0;
            _streamPos = _pos;
        }

        public void CopyBlock(uint distance, uint len)
        {
            uint pos = _pos - distance - 1;
            if (pos >= _windowSize)
                pos += _windowSize;
            for (; len > 0; len--)
            {
                if (pos >= _windowSize)
                    pos = 0;
                _buffer[_pos++] = _buffer[pos++];
                if (_pos >= _windowSize)
                    Flush();
            }
        }

        public void PutByte(byte b)
        {
            _buffer[_pos++] = b;
            if (_pos >= _windowSize)
                Flush();
        }

        public byte GetByte(uint distance)
        {
            uint pos = _pos - distance - 1;
            if (pos >= _windowSize)
                pos += _windowSize;
            return _buffer[pos];
        }
    }
}

using System;

namespace Utility.IO.Compression.Lzma.Lz
{
    class OutWindow
    {
        private byte[] _buffer = null;
        private uint _pos;
        private uint _windowSize = 0;
        private uint _streamPos;
        private IOutputBuffer _stream;

        public void Create(uint windowSize)
        {
            if (_windowSize != windowSize)
                _buffer = new byte[windowSize];
            _windowSize = windowSize;
            _pos = 0;
            _streamPos = 0;
        }

        public void Init(IOutputBuffer stream, bool solid)
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
#if DEBUG
            checked
#endif
            {
                uint size = _pos - _streamPos;
                if (size == 0)
                    return;
                _stream.Write(_buffer.AsReadOnly(), (int)_streamPos, (int)size);
                if (_pos >= _windowSize)
                    _pos = 0;
                _streamPos = _pos;
            }
        }

        public void CopyBlock(uint distance, uint len)
        {
#if DEBUG
            checked
#endif
            {
                var pos = _pos >= distance + 1 ? _pos - distance - 1 : _windowSize + _pos - distance - 1;
                for (; len > 0; len--)
                {
                    if (pos >= _windowSize)
                        pos = 0;
                    _buffer[_pos++] = _buffer[pos++];
                    if (_pos >= _windowSize)
                        Flush();
                }
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
#if DEBUG
            checked
#endif
            {
                var pos = _pos >= distance + 1 ? _pos - distance - 1 : _windowSize + _pos - distance - 1;
                return _buffer[pos];
            }
        }

        public UInt32 BlockSize => _windowSize;
    }
}

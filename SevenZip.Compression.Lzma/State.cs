// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using System;

namespace SevenZip.Compression.Lzma
{
    public struct State
    {
        private UInt32 _index;

        public State()
        {
            _index = 0;
        }

        public UInt32 Index => _index;

        public void UpdateChar()
        {
            if (_index < 4)
                _index = 0;
            else if (_index < 10)
                _index -= 3;
            else
                _index -= 6;
        }

        public void UpdateMatch() => _index = _index < 7 ? 7U : 10U;
        public void UpdateRep() => _index = _index < 7 ? 8U : 11U;
        public void UpdateShortRep() => _index = _index < 7 ? 9U : 11U;
        public bool IsCharState() => _index < 7;
    }
}

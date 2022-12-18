// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using System;

namespace SevenZip.Compression.Lzma.Encoder
{
    class Optimal
    {
        public State State;
        public bool Prev1IsChar;
        public bool Prev2;
        public UInt32 PosPrev2;
        public UInt32 BackPrev2;
        public UInt32 Price;
        public UInt32 PosPrev;
        public UInt32 BackPrev;
        public UInt32 Backs0;
        public UInt32 Backs1;
        public UInt32 Backs2;
        public UInt32 Backs3;

        public Optimal()
        {
            State = new State();
            Prev1IsChar = false;
            Prev2 = false;
            PosPrev2 = 0;
            BackPrev2 = 0;
            Price = 0;
            PosPrev = 0;
            BackPrev = 0;
            Backs0 = 0;
            Backs1 = 0;
            Backs2 = 0;
            Backs3 = 0;
        }

        public void MakeAsChar() { BackPrev = 0xFFFFFFFF; Prev1IsChar = false; }
        public void MakeAsShortRep() { BackPrev = 0; ; Prev1IsChar = false; }
        public bool IsShortRep() { return BackPrev == 0; }
    }
}

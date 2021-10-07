using System;

namespace Utility.IO.Compression.Lzma
{
    class LzmaOptimal
    {
        public LzmaCoder.State State;

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

        public void MakeAsChar() { BackPrev = UInt32.MaxValue; Prev1IsChar = false; }
        public void MakeAsShortRep() { BackPrev = 0; ; Prev1IsChar = false; }
        public bool IsShortRep() { return BackPrev == 0; }
    };
}

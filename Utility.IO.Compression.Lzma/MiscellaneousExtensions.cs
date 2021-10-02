using System;

namespace Utility.IO.Compression
{
    static class MiscellaneousExtensions
    {
        public static UInt32 ConcatBit(this UInt32 bitArray, bool bit)
        {
            bitArray <<= 1;
            if (bit)
                bitArray |= 1;
            return bitArray;
        }
    }
}

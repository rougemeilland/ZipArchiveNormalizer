using System;

namespace Utility.IO
{
    public interface IInputBitStream
        : IDisposable
    {
        bool? ReadBit();
        TinyBitArray ReadBits(int bitCount);
    }
}

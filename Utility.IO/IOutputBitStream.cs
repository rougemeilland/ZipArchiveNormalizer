using System;

namespace Utility.IO
{
    public interface IOutputBitStream
        : IDisposable
    {
        void Write(bool bit);
        void Write(TinyBitArray bitArray);
        void Flush();
    }
}

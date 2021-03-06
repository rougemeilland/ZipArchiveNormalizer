using System;

namespace SevenZip.Compression.Lz
{
    interface IMatchFinder : IInWindowStream
    {
        void Create(UInt32 historySize, UInt32 keepAddBufferBefore,
                UInt32 matchMaxLen, UInt32 keepAddBufferAfter);
        UInt32 GetMatches(UInt32[] distances);
        void Skip(UInt32 num);
    }
}

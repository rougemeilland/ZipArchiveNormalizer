using System;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.IO
{
    public interface IInputBitStream
        : IDisposable, IAsyncDisposable
    {
        bool? ReadBit();
        Task<bool?> ReadBitAsync(CancellationToken cancellationToken = default);
        TinyBitArray? ReadBits(Int32 bitCount);
        Task<TinyBitArray?> ReadBitsAsync(Int32 bitCount, CancellationToken cancellationToken = default);
    }
}

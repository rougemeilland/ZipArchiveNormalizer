using System;
using System.Threading;
using System.Threading.Tasks;
using Utility.IO;

namespace ZipUtility.IO
{
    public interface IDecoder
    {
        void Decode(IInputByteStream<UInt64> sourceStream, IOutputByteStream<UInt64> destinationStream, ICoderOption option, UInt64 unpackedSize, UInt64 packedSize, IProgress<UInt64>? progress = null);
        Task<Exception?> DecodeAsync(IInputByteStream<UInt64> sourceStream, IOutputByteStream<UInt64> destinationStream, ICoderOption option, UInt64 unpackedSize, UInt64 packedSize, IProgress<UInt64>? progress = null, CancellationToken cancellationToken = default);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Utility.IO;

namespace ZipUtility.IO
{
    public interface IEncoder
    {
        void Encode(IInputByteStream<UInt64> sourceStream, IOutputByteStream<UInt64> destinationStream, ICoderOption option, UInt64? sourceSize, IProgress<UInt64>? progress = null);
        Task<Exception?> EncodeAsync(IInputByteStream<UInt64> sourceStream, IOutputByteStream<UInt64> destinationStream, ICoderOption option, UInt64? sourceSize, IProgress<UInt64>? progress = null, CancellationToken cancellationToken = default);
    }
}

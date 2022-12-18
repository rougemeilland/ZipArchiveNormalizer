using System;
using Utility.IO;

namespace ZipUtility.IO
{
    public interface IHierarchicalEncoder
    {
        IOutputByteStream<UInt64> GetEncodingStream(IOutputByteStream<UInt64> baseStream, ICoderOption option, UInt64? unpackedStreamSize, IProgress<UInt64>? progress = null);
    }
}

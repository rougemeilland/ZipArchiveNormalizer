using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Stored
{
    public class StoredEncoderPlugin
        : StoredCoderPlugin, ICompressionHierarchicalEncoder
    {
        private class Encoder
            : HierarchicalEncoder
        {
            public Encoder(IOutputByteStream<UInt64> baseStream, UInt64? unpackedStreamSize, IProgress<UInt64>? progress)
                : base(baseStream, unpackedStreamSize, progress)
            {
            }
        }

        IOutputByteStream<UInt64> IHierarchicalEncoder.GetEncodingStream(IOutputByteStream<UInt64> baseStream, ICoderOption option, UInt64? unpackedStreamSize, IProgress<UInt64>? progress) =>
            new Encoder(baseStream, unpackedStreamSize, progress);
    }
}

using System;
using Utility.IO;

namespace ZipUtility.IO.Compression.Stored
{
    public class StoredDecoderPlugin
        : StoredCoderPlugin, ICompressionHierarchicalDecoder
    {
        private class Decoder
            : HierarchicalDecoder
        {
            public Decoder(IInputByteStream<UInt64> baseStream, UInt64 unpackedStreamSize, IProgress<UInt64>? progress)
                : base(baseStream, unpackedStreamSize, progress)
            {
            }
        }

        IInputByteStream<UInt64> IHierarchicalDecoder.GetDecodingStream(IInputByteStream<UInt64> baseStream, ICoderOption option, UInt64 unpackedStreamSize, UInt64 packedStreamSize, IProgress<UInt64>? progress) =>
            new Decoder(baseStream, unpackedStreamSize, progress);
    }
}

using System;
using System.IO;

namespace ZipUtility.Compression
{
    public interface ICompressionMethod
    {
        CompressionMethodId CompressionMethodId { get; }
        ICompressionOption CreateOptionFromGeneralPurposeFlag(bool bit1, bool bit2);
        Stream GetInputStream(Stream baseStream, ICompressionOption option, long? offset, long packedSize, long size, bool leaveOpen);
        Stream GetOutputStream(Stream baseStream, ICompressionOption option, long? offset, long? size, bool leaveOpen);
    }
}
using Utility.IO;
using System;

namespace ZipUtility.IO.Compression
{
    public interface ICompressionMethod
    {
        CompressionMethodId CompressionMethodId { get; }
        ICompressionOption CreateOptionFromGeneralPurposeFlag(bool bit1, bool bit2);
        IInputByteStream<UInt64> GetInputStream(IInputByteStream<UInt64> baseStream, ICompressionOption option, ulong size);
        IOutputByteStream<UInt64> GetOutputStream(IOutputByteStream<UInt64> baseStream, ICompressionOption option, ulong? size);
    }
}
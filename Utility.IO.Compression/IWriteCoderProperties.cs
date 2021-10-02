using System;

namespace Utility.IO.Compression
{
    public interface IWriteCoderProperties
    {
        void WriteCoderProperties(IOutputByteStream<UInt64> outStream);
    }
}

namespace Utility.IO.Compression.Lzma
{
    internal interface IOutputBuffer
    {
        void Write(IReadOnlyArray<byte> buffer, int offset, int count);
    }
}

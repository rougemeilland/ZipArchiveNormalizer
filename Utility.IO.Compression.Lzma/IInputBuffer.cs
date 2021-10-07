namespace Utility.IO.Compression.Lzma
{
    internal interface IInputBuffer
    {
        int Read(byte[] buffer, int offset, int count);
    }
}

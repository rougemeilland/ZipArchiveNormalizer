namespace Utility.IO
{
    interface IFIFOReadable
    {
        int Read(byte[] buffer, int offset, int count);
        void SetReadCount(long count);
        void Close();
    }
}
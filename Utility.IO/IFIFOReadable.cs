namespace Utility.IO
{
    interface IFifoReadable
    {
        int Read(byte[] buffer, int offset, int count);
        void SetReadCount(long count);
        void Close();
    }
}
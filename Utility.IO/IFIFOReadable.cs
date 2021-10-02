namespace Utility.IO
{
    interface IFifoReadable
    {
        int Read(byte[] buffer, int offset, int count);
        void SetReadCount(ulong count);
        void Close();
    }
}
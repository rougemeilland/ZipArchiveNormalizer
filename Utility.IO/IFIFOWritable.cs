using System.Threading;

namespace Utility.IO
{
    interface IFifoWritable
    {
        void Write(byte[] buffer, int offset, int count);
        void WaitForReadCount(long count, CancellationToken token);
        void Close();
    }
}
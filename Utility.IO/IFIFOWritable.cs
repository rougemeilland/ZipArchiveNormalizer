using System.Threading;

namespace Utility.IO
{
    interface IFifoWritable
    {
        int Write(IReadOnlyArray<byte> buffer, int offset, int count);
        void WaitForReadCount(ulong count, CancellationToken token);
        void Close();
    }
}
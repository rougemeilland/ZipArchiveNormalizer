using System;
using System.Threading.Tasks;

namespace Utility.Threading
{
    public interface IAsyncLockable
        : IDisposable
    {
        Task LockAsync();
        void Unlock();
    }
}

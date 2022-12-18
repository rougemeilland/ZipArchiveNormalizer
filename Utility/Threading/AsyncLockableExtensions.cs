using System;
using System.Threading.Tasks;

namespace Utility.Threading
{
    public static class AsyncLockableExtensions
    {
        private class AsyncLockable
            : IDisposable
        {
            private readonly IAsyncLockable _lockObject;

            private bool _isDisposed;

            public AsyncLockable(IAsyncLockable lockObject)
            {
                _isDisposed = false;
                _lockObject = lockObject;
            }

            public Task LockAsync() => _lockObject.LockAsync();

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_isDisposed)
                {
                    if (disposing)
                        _lockObject.Unlock();
                    _isDisposed = true;
                }
            }
        }

        public static async Task<IDisposable> LockAsync(this IAsyncLockable lockObject)
        {
            var asyncLock = new AsyncLockable(lockObject);
            await asyncLock.LockAsync().ConfigureAwait(false);
            return asyncLock;
        }
    }
}

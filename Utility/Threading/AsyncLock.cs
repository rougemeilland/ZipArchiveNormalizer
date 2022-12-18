using System;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.Threading
{
    public static class AsyncLock
    {
        private class AsyncLockObject
            : IAsyncLockable
        {
            private readonly SemaphoreSlim _semaphore;

            private bool _isDisposed;

            public AsyncLockObject()
            {
                _isDisposed = false;
                _semaphore = new SemaphoreSlim(1, 1);
            }

            public async Task LockAsync()
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);
            }

            public void Unlock()
            {
                _semaphore.Release();
            }

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
                    {
                        _semaphore.Dispose();
                    }
                    _isDisposed = true;
                }
            }
        }

        public static IAsyncLockable Create()
        {
            return new AsyncLockObject();
        }
    }
}

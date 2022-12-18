using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.Threading
{
    public class AsyncManualResetEvent
    {
        private class AsynchronousTaskCompletionSource
            : TaskCompletionSource<Byte>
        {
            private readonly Task<Byte> _copyOfTask;

            public AsynchronousTaskCompletionSource()
                : base(null, TaskCreationOptions.RunContinuationsAsynchronously)
            {
                _copyOfTask = Task;
            }

            public Task<Byte> AsynchronousTask
            {
                get
                {
                    var task = Task;
                    return task.IsCompleted ? task : _copyOfTask;
                }
            }
        }

        private AsynchronousTaskCompletionSource _taskCompletionSource;

        private bool _isSet;

        public AsyncManualResetEvent(bool initialState = false)
        {
            _taskCompletionSource = CreateAsynchronousTaskCompletionSource();
            _isSet = initialState;
            if (initialState)
                _taskCompletionSource.SetResult(0);
        }

        public Task WaitAsync()
        {
            lock (this)
            {
                return _taskCompletionSource.AsynchronousTask;
            }
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            var task = WaitAsync();
            if (!cancellationToken.CanBeCanceled || task.IsCompleted)
                return;
            cancellationToken.ThrowIfCancellationRequested();
            var taskCompletionSourceToWaitForCancellation = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(CancellationAction, taskCompletionSourceToWaitForCancellation))
            {
                if (await Task.WhenAny(task, taskCompletionSourceToWaitForCancellation.Task).ConfigureAwait(false) != task)
                    cancellationToken.ThrowIfCancellationRequested();
            }
            await task.ConfigureAwait(false);
        }

        public void Set()
        {
            AsynchronousTaskCompletionSource tcs;
            var requiredToSetResult = false;
            lock (this)
            {
                requiredToSetResult = !_isSet;
                tcs = _taskCompletionSource;
                _isSet = true;
            }
            if (requiredToSetResult)
            {
                tcs.TrySetResult(0);
            }
        }

        public void Reset()
        {
            lock (this)
            {
                if (_isSet)
                {
                    _taskCompletionSource = CreateAsynchronousTaskCompletionSource();
                    _isSet = false;
                }
            }
        }

        private static void CancellationAction(object? state)
        {
            if (state is TaskCompletionSource<bool> taskCompletionSource)
                taskCompletionSource.TrySetResult(true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AsynchronousTaskCompletionSource CreateAsynchronousTaskCompletionSource() => new();
    }
}

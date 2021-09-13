using System;

namespace Utility
{
    public class ConsoleBreakCancellale
        : IWorkerCancellable, IDisposable
    {
        private bool _isDisposed;
        private bool _canceled;

        public ConsoleBreakCancellale()
        {
            _isDisposed = false;
            _canceled = false;
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        public string Usage => "Ctrl+Cを押すと安全に中断できます";

        public bool IsRequestToCancel => _canceled;

        public void ResetCancellationStatus()
        {
            _canceled = false;
        }

        public void CheckCancellatio()
        {
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
                    Console.CancelKeyPress -= Console_CancelKeyPress;
                }
                _isDisposed = true;
            }
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _canceled = true;
            e.Cancel = true;
        }
    }
}
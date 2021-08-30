using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;

namespace Utility.FileWorker
{
    public abstract class FileWorker
        : IFileWorker
    {
        private class ExecutionResult
            : IFileWorkerExecutionResult
        {
            public ExecutionResult(IEnumerable<FileInfo> sourceFiles, IEnumerable<FileInfo> destinationFiles, long totalChangedFileCount)
            {
                SourceFiles = sourceFiles.ToReadOnlyCollection();
                DestinationFiles = destinationFiles.ToReadOnlyCollection();
                TotalChangedFileCount = totalChangedFileCount;
            }

            public IReadOnlyCollection<FileInfo> SourceFiles { get; }

            public IReadOnlyCollection<FileInfo> DestinationFiles { get; }

            public long TotalChangedFileCount { get; }
        }

        private IWorkerCancellable _canceller;
        private bool _walkingNow;
        private IEnumerable<FileInfo> _sourceFiles;
        private ICollection<FileInfo> _destinationFiles;
        private long _changedFileCount;

        public event EventHandler<FileMessageReportedEventArgs> InformationReported;
        public event EventHandler<FileMessageReportedEventArgs> WarningReported;
        public event EventHandler<FileMessageReportedEventArgs> ErrorReported;
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        protected FileWorker(IWorkerCancellable canceller)
        {
            _canceller = canceller;
            _walkingNow = false;
            _sourceFiles = new List<FileInfo>();
            _destinationFiles = new List<FileInfo>();
            _changedFileCount = 0;
        }

        public abstract string Description { get; }

        public IFileWorkerExecutionResult Execute(IEnumerable<string> args, IFileWorkerExecutionResult previousWorkerResult)
        {
            lock (this)
            {
                if (_walkingNow)
                    throw new InvalidOperationException();
                _walkingNow = true;
            }
            try
            {
                _destinationFiles.Clear();
                _changedFileCount = 0;
                _sourceFiles = args.EnumerateFilesFromArgument();
                if (IsRequestedToCancel)
                    throw new OperationCanceledException();
                ExecuteWork(_sourceFiles, previousWorkerResult);
                return new ExecutionResult(_sourceFiles, _destinationFiles, _changedFileCount);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (AggregateException ex)
            {
                foreach (var exception in ex.InnerExceptions)
                {
                    if (exception is OperationCanceledException)
                        throw exception;
                    throw exception;
                }
                throw new Exception("AggregateException.InnerExceptionsが空です。", ex);
            }
            finally
            {
                lock (this)
                {
                    _walkingNow = false;
                }
            }
        }

        public IFileWorkerExecutionResult Execute(IEnumerable<FileInfo> sourceFiles, IFileWorkerExecutionResult previousWorkerResult)
        {
            lock (this)
            {
                if (_walkingNow)
                    throw new InvalidOperationException();
                _walkingNow = true;
            }
            try
            {
                _destinationFiles.Clear();
                _changedFileCount = 0;
                if (IsRequestedToCancel)
                    throw new OperationCanceledException();
                var copyOfSourceFiles = sourceFiles.ToReadOnlyCollection();
                ExecuteWork(sourceFiles, previousWorkerResult);
                return new ExecutionResult(copyOfSourceFiles, _destinationFiles, _changedFileCount);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (AggregateException ex)
            {
                foreach (var exception in ex.InnerExceptions)
                {
                    if (exception is OperationCanceledException)
                        throw exception;
                    throw exception;
                }
                throw new Exception("AggregateException.InnerExceptionsが空です。", ex);
            }
            finally
            {
                lock (this)
                {
                    _walkingNow = false;
                }
            }
        }

        public bool IsRequestedToCancel
        {
            get
            {
                lock (this)
                {
                    _canceller.CheckCancellatio();
                    return _canceller.IsRequestToCancel;
                }
            }
        }

        public void Cancel()
        {
            lock (this)
            {
                if (_walkingNow)
                {
                    _canceller.Cancel();
                }
            }
        }

        protected abstract void ExecuteWork(IEnumerable<FileInfo> sourceFiles, IFileWorkerExecutionResult previousWorkerResult);

        protected void SetToSourceFiles(IEnumerable<FileInfo> sourceFiles)
        {
            lock (this)
            {
                _sourceFiles = sourceFiles;
            }
        }

        protected void AddToDestinationFiles(FileInfo destinationFile)
        {
            lock (this)
            {
                _destinationFiles.Add(destinationFile);
            }
        }

        protected void IncrementChangedFileCount() => Interlocked.Increment(ref _changedFileCount);

        protected void RaiseInformationReportedEvent(string messsage)
        {
            try
            {
                if (InformationReported != null)
                    InformationReported(this, new FileMessageReportedEventArgs(null, messsage));
            }
            catch (Exception)
            {
            }
            _canceller.CheckCancellatio();
        }

        protected void RaiseInformationReportedEvent(FileInfo sourceFile, string messsage)
        {
            try
            {
                if (InformationReported != null)
                    InformationReported(this, new FileMessageReportedEventArgs(sourceFile, messsage));
            }
            catch (Exception)
            {
            }
            _canceller.CheckCancellatio();
        }

        protected void RaiseWarningReportedEvent(string messsage)
        {
            try
            {
                if (WarningReported != null)
                    WarningReported(this, new FileMessageReportedEventArgs(null, messsage));
            }
            catch (Exception)
            {
            }
            _canceller.CheckCancellatio();
        }

        protected void RaiseWarningReportedEvent(FileInfo sourceFile, string messsage)
        {
            try
            {
                if (WarningReported != null)
                    WarningReported(this, new FileMessageReportedEventArgs(sourceFile, messsage));
            }
            catch (Exception)
            {
            }
            _canceller.CheckCancellatio();
        }

        protected void RaiseErrorReportedEvent(string messsage)
        {
            try
            {
                if (ErrorReported != null)
                    ErrorReported(this, new FileMessageReportedEventArgs(null, messsage));
            }
            catch (Exception)
            {
            }
            _canceller.CheckCancellatio();
        }

        protected void RaiseErrorReportedEvent(FileInfo sourceFile, string messsage)
        {
            try
            {
                if (ErrorReported != null)
                    ErrorReported(this, new FileMessageReportedEventArgs(sourceFile, messsage));
            }
            catch (Exception)
            {
            }
            _canceller.CheckCancellatio();
        }

        protected void UpdateProgress()
        {
            try
            {
                if (ProgressChanged != null)
                    ProgressChanged(this, new ProgressChangedEventArgs());
            }
            catch (Exception)
            {
            }
            _canceller.CheckCancellatio();
        }

        protected void UpdateProgress(long totalCount, long countOfDone)
        {
#if DEBUG
            if (countOfDone > totalCount)
                throw new Exception();
#endif
            try
            {
                if (ProgressChanged != null)
                    ProgressChanged(this, new ProgressChangedEventArgs(totalCount, countOfDone));
            }
            catch (Exception)
            {
            }
            _canceller.CheckCancellatio();
        }
    }
}
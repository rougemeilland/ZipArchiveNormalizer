using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Utility.FileWorker
{
    public abstract class FileWorker
        : IFileWorker
    {
        private class ExecutionResult
            : IFileWorkerExecutionResult
        {
            public ExecutionResult(IEnumerable<FileInfo> sourceFiles, IEnumerable<FileInfo> destinationFiles, Int64 totalChangedFileCount)
            {
                SourceFiles = sourceFiles.ToReadOnlyCollection();
                DestinationFiles = destinationFiles.ToReadOnlyCollection();
                TotalChangedFileCount = totalChangedFileCount;
            }

            public IReadOnlyCollection<FileInfo> SourceFiles { get; }

            public IReadOnlyCollection<FileInfo> DestinationFiles { get; }

            public Int64 TotalChangedFileCount { get; }
        }

        private readonly IWorkerCancellable _canceller;
        private readonly ICollection<FileInfo> _destinationFiles;

        private bool _walkingNow;
        private IEnumerable<FileInfo> _sourceFiles;
        private Int64 _changedFileCount;
        private bool _cancelledbyUser;
        private bool _aborted;

        public event EventHandler<FileMessageReportedEventArgs>? InformationReported;
        public event EventHandler<FileMessageReportedEventArgs>? WarningReported;
        public event EventHandler<FileMessageReportedEventArgs>? ErrorReported;
        public event EventHandler<ProgressChangedEventArgs>? ProgressChanged;

        protected FileWorker(IWorkerCancellable canceller)
        {
            _canceller = canceller ?? throw new ArgumentNullException(nameof(canceller));
            _walkingNow = false;
            _sourceFiles = new List<FileInfo>();
            _destinationFiles = new List<FileInfo>();
            _changedFileCount = 0;
            _cancelledbyUser = false;
            _aborted = false;
        }

        public abstract string Description { get; }

        public IFileWorkerExecutionResult Execute(IEnumerable<string> args, IFileWorkerExecutionResult? previousWorkerResult)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

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
                SafetyCancellationCheck();
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
            if (sourceFiles is null)
                throw new ArgumentNullException(nameof(sourceFiles));
            if (previousWorkerResult is null)
                throw new ArgumentNullException(nameof(previousWorkerResult));

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
                SafetyCancellationCheck();
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
                    CheckForCancellation();
                    return _cancelledbyUser | _aborted;
                }
            }
        }

        protected void SafetyCancellationCheck()
        {
            lock (this)
            {
                CheckForCancellation();
                if (_cancelledbyUser || _aborted)
                    throw new OperationCanceledException();
            }
        }

        protected void Abort()
        {
            lock (this)
            {
                if (_walkingNow)
                {
                    _aborted = true;
                }
            }
        }

        protected abstract void ExecuteWork(IEnumerable<FileInfo> sourceFiles, IFileWorkerExecutionResult? previousWorkerResult);

        protected void SetToSourceFiles(IEnumerable<FileInfo> sourceFiles)
        {
            if (sourceFiles is null)
                throw new ArgumentNullException(nameof(sourceFiles));

            lock (this)
            {
                _sourceFiles = sourceFiles;
            }
        }

        protected void AddToDestinationFiles(FileInfo destinationFile)
        {
            if (destinationFile is null)
                throw new ArgumentNullException(nameof(destinationFile));

            lock (this)
            {
                _destinationFiles.Add(destinationFile);
            }
        }

        protected void IncrementChangedFileCount() => Interlocked.Increment(ref _changedFileCount);

        protected void RaiseInformationReportedEvent(string messsage)
        {
            if (string.IsNullOrEmpty(messsage))
                throw new ArgumentException($"'{nameof(messsage)}' を NULL または空にすることはできません。", nameof(messsage));

            try
            {
                if (InformationReported is not null)
                    InformationReported(this, new FileMessageReportedEventArgs(null, messsage));
            }
            catch (Exception)
            {
            }
            lock (this)
            {
                CheckForCancellation();
            }
        }

        protected void RaiseInformationReportedEvent(FileInfo? sourceFile, string messsage)
        {
            if (string.IsNullOrEmpty(messsage))
                throw new ArgumentException($"'{nameof(messsage)}' を NULL または空にすることはできません。", nameof(messsage));

            try
            {
                if (InformationReported is not null)
                    InformationReported(this, new FileMessageReportedEventArgs(sourceFile, messsage));
            }
            catch (Exception)
            {
            }
            lock (this)
            {
                CheckForCancellation();
            }
        }

        protected void RaiseWarningReportedEvent(string messsage)
        {
            if (string.IsNullOrEmpty(messsage))
                throw new ArgumentException($"'{nameof(messsage)}' を NULL または空にすることはできません。", nameof(messsage));

            try
            {
                if (WarningReported is not null)
                    WarningReported(this, new FileMessageReportedEventArgs(null, messsage));
            }
            catch (Exception)
            {
            }
            lock (this)
            {
                CheckForCancellation();
            }
        }

        protected void RaiseWarningReportedEvent(FileInfo? sourceFile, string messsage)
        {
            if (string.IsNullOrEmpty(messsage))
                throw new ArgumentException($"'{nameof(messsage)}' を NULL または空にすることはできません。", nameof(messsage));

            try
            {
                if (WarningReported is not null)
                    WarningReported(this, new FileMessageReportedEventArgs(sourceFile, messsage));
            }
            catch (Exception)
            {
            }
            lock (this)
            {
                CheckForCancellation();
            }
        }

        protected void RaiseErrorReportedEvent(string messsage)
        {
            if (string.IsNullOrEmpty(messsage))
                throw new ArgumentException($"'{nameof(messsage)}' を NULL または空にすることはできません。", nameof(messsage));

            try
            {
                if (ErrorReported is not null)
                    ErrorReported(this, new FileMessageReportedEventArgs(null, messsage));
            }
            catch (Exception)
            {
            }
            lock (this)
            {
                CheckForCancellation();
            }
        }

        protected void RaiseErrorReportedEvent(FileInfo? sourceFile, string messsage)
        {
            if (string.IsNullOrEmpty(messsage))
                throw new ArgumentException($"'{nameof(messsage)}' を NULL または空にすることはできません。", nameof(messsage));

            try
            {
                if (ErrorReported is not null)
                    ErrorReported(this, new FileMessageReportedEventArgs(sourceFile, messsage));
            }
            catch (Exception)
            {
            }
            lock (this)
            {
                CheckForCancellation();
            }
        }

        protected void UpdateProgress()
        {
            try
            {
                if (ProgressChanged is not null)
                    ProgressChanged(this, new ProgressChangedEventArgs());
            }
            catch (Exception)
            {
            }
            lock (this)
            {
                CheckForCancellation();
            }
        }

        protected void UpdateProgress(UInt64 totalCount, UInt64 countOfDone)
        {
#if DEBUG
            if (countOfDone > totalCount)
                throw new Exception();
#endif
            try
            {
                if (ProgressChanged is not null)
                    ProgressChanged(this, new ProgressChangedEventArgs(totalCount, countOfDone));
            }
            catch (Exception)
            {
            }
            lock (this)
            {
                CheckForCancellation();
            }
        }

        private void CheckForCancellation()
        {
            _canceller.CheckCancellatio();
            _cancelledbyUser |= _canceller.IsRequestToCancel;
        }
    }
}

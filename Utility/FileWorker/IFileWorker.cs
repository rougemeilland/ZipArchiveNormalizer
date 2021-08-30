using System;
using System.Collections.Generic;

namespace Utility.FileWorker
{
    public interface IFileWorker
    {
        event EventHandler<FileMessageReportedEventArgs> InformationReported;
        event EventHandler<FileMessageReportedEventArgs> WarningReported;
        event EventHandler<FileMessageReportedEventArgs> ErrorReported;
        event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        string Description { get; }
        void Cancel();
        IFileWorkerExecutionResult Execute(IEnumerable<string> args, IFileWorkerExecutionResult previousWorkerResult);
        bool IsRequestedToCancel { get; }
    }
}
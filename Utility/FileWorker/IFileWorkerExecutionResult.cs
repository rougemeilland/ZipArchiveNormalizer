using System;
using System.Collections.Generic;
using System.IO;

namespace Utility.FileWorker
{
    public interface IFileWorkerExecutionResult
    {
        IReadOnlyCollection<FileInfo> SourceFiles { get; }
        IReadOnlyCollection<FileInfo> DestinationFiles { get; }
        Int64 TotalChangedFileCount { get; }
    }
}
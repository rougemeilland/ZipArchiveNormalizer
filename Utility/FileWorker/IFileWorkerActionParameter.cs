using System;

namespace Utility.FileWorker
{
    public interface IFileWorkerActionParameter
    {
        Int32 FileIndexOnSameDirectory { get; }
        IFileWorkerActionDirectoryParameter DirectoryParameter { get; }
        IFileWorkerActionFileParameter FileParameter { get; }
    }
}
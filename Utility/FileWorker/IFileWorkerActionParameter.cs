namespace Utility.FileWorker
{
    public interface IFileWorkerActionParameter
    {
        int FileIndexOnSameDirectory { get; }
        IFileWorkerActionDirectoryParameter DirectoryParameter { get; }
        IFileWorkerActionFileParameter FileParameter { get; }
    }
}
using System;
using Utility.FileWorker;

namespace ZipArchiveNormalizer
{
    interface IPhaseWorker
        : IFileWorker
    {
        event EventHandler<BadFileFoundEventArgs> BadFileFound;
    }
}
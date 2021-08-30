using System;
using System.IO;

namespace ZipArchiveNormalizer
{
    class BadFileFoundEventArgs
        : EventArgs
    {
        public BadFileFoundEventArgs(FileInfo targetFile)
        {
            TargetFile = targetFile;
        }

        public FileInfo TargetFile { get; }
    }
}

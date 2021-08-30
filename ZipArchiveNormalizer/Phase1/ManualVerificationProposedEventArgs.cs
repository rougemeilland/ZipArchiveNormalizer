using System;
using System.IO;

namespace ZipArchiveNormalizer.Phase1
{
    class ManualVerificationProposedEventArgs
        : EventArgs
    {
        public ManualVerificationProposedEventArgs(FileInfo targetFile)
        {
            TargetFile = targetFile;
        }

        public FileInfo TargetFile { get; }
    }
}
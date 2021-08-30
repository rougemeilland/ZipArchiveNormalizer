using System;
using System.IO;

namespace Utility
{
    public class FileMessageReportedEventArgs
        : EventArgs
    {
        public FileMessageReportedEventArgs(FileInfo targetFile, string message)
        {
            TargetFile = targetFile;
            Message = message;
        }

        public FileInfo TargetFile { get; }
        public string Message { get; }
    }
}

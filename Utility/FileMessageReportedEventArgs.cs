using System;
using System.IO;

namespace Utility
{
    public class FileMessageReportedEventArgs
        : EventArgs
    {
        public FileMessageReportedEventArgs(FileInfo? targetFile, string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException($"'{nameof(message)}' を NULL または空にすることはできません。", nameof(message));

            TargetFile = targetFile;
            Message = message;
        }

        public FileInfo? TargetFile { get; }
        public string Message { get; }
    }
}

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Utility
{
    public static class FileExtensions
    {
        private static object _lockObject;
        private static Regex _simpleFileNamePattern;

        static FileExtensions()
        {
            _lockObject = new object();
            _simpleFileNamePattern = new Regex(@"^(?<path>.*?)(\s*\([0-9]+\))+$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        public static FileInfo RenameFile(this FileInfo sourceFile, string newFileName)
        {
            bool alreadyExists;
            return sourceFile.RenameFile(newFileName, out alreadyExists);
        }

        public static FileInfo RenameFile(this FileInfo sourceFile, string newFileName, out bool alreadyExists)
        {
            alreadyExists = false;
            var sourceFileDirectory = sourceFile.Directory;
            var sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(newFileName);
            var fileNameMatch = _simpleFileNamePattern.Match(sourceFileNameWithoutExtension);
            if (fileNameMatch.Success)
                sourceFileNameWithoutExtension = fileNameMatch.Groups["path"].Value;
            var sourceFileExtension = Path.GetExtension(newFileName);
            lock (_lockObject)
            {
                var retryCount = 1;
                while (true)
                {
                    var newFile =
                        new FileInfo(
                            Path.Combine(
                                sourceFileDirectory.FullName,
                                sourceFileNameWithoutExtension + (retryCount <= 1 ? "" : string.Format(" ({0})", retryCount)) + sourceFileExtension));
                    if (string.Equals(newFile.FullName, sourceFile.FullName, StringComparison.InvariantCultureIgnoreCase))
                        return newFile;
                    else if (!newFile.Exists)
                    {
                        // MoveTo メソッドは FileInfo オブジェクトを改変してしまうため、
                        // 複製してから呼び出している
                        new FileInfo(sourceFile.FullName).MoveTo(newFile.FullName);
                        return newFile;
                    }
                    else if (newFile.Length == sourceFile.Length &&
                            newFile.OpenRead().StreamBytesEqual(sourceFile.OpenRead()))
                    {
                        sourceFile.Delete();
                        alreadyExists = true;
                        return newFile;
                    }
                    else
                        ++retryCount;
                }
            }
        }

        public static UInt32 CalculateCrc32(this FileInfo sourceFile)
        {
            return sourceFile.OpenRead().GetByteSequence().CalculateCrc32();
        }
    }
}
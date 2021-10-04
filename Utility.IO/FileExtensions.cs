using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Utility.IO
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

        public static FileInfo GetFile(this DirectoryInfo directory, string fileName)
        {
            return new FileInfo(Path.Combine(directory.FullName, fileName));
        }

        public static DirectoryInfo GetSubDirectory(this DirectoryInfo directory, string directoryName)
        {
            return new DirectoryInfo(Path.Combine(directory.FullName, directoryName));
        }

        public static byte[] ReadAllBytes(this FileInfo file)
        {
            return File.ReadAllBytes(file.FullName);
        }

        public static string[] ReadAllLines(this FileInfo file)
        {
            return File.ReadAllLines(file.FullName);
        }

        public static IEnumerable<string> ReadLines(this FileInfo file)
        {
            return File.ReadLines(file.FullName);
        }

        public static void WriteAllBytes(this FileInfo file, IEnumerable<byte> data)
        {
            using (var stream = file.OpenWrite())
            {
                stream.Write(data);
            }
        }

        public static void WriteAllBytes(this FileInfo file, byte[] data)
        {
            File.WriteAllBytes(file.FullName, data);
        }

        public static void WriteAllBytes(this FileInfo file, IReadOnlyArray<byte> data)
        {
            File.WriteAllBytes(file.FullName, data.GetRawArray());
        }

        public static void WriteAllText(this FileInfo file, string text)
        {
            File.WriteAllText(file.FullName, text);
        }

        public static void WriteAllText(this FileInfo file, string text, Encoding encoding)
        {
            File.WriteAllText(file.FullName, text, encoding);
        }

        public static void WriteAllText(this FileInfo file, string[] lines)
        {
            File.WriteAllLines(file.FullName, lines);
        }

        public static void WriteAllText(this FileInfo file, IEnumerable<string> lines)
        {
            File.WriteAllLines(file.FullName, lines);
        }

        public static void WriteAllText(this FileInfo file, string[] lines, Encoding encoding)
        {
            File.WriteAllLines(file.FullName, lines, encoding);
        }

        public static void WriteAllText(this FileInfo file, IEnumerable<string> lines, Encoding encoding)
        {
            File.WriteAllLines(file.FullName, lines, encoding);
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
                    if (string.Equals(newFile.FullName, sourceFile.FullName, StringComparison.OrdinalIgnoreCase))
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
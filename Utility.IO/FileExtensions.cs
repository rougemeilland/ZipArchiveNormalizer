using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Utility.IO
{
    public static class FileExtensions
    {
        private static readonly object _lockObject;
        private static readonly Regex _simpleFileNamePattern;

        static FileExtensions()
        {
            _lockObject = new object();
            _simpleFileNamePattern = new Regex(@"^(?<path>.*?)(\s*\([0-9]+\))+$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        public static FileInfo GetFile(this DirectoryInfo directory, string fileName)
        {
            if (directory is null)
                throw new ArgumentNullException(nameof(directory));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException($"'{nameof(fileName)}' を NULL または空にすることはできません。", nameof(fileName));

            return new FileInfo(Path.Combine(directory.FullName, fileName));
        }

        public static DirectoryInfo GetSubDirectory(this DirectoryInfo directory, string directoryName)
        {
            if (directory is null)
                throw new ArgumentNullException(nameof(directory));
            if (string.IsNullOrEmpty(directoryName))
                throw new ArgumentException($"'{nameof(directoryName)}' を NULL または空にすることはできません。", nameof(directoryName));

            return new DirectoryInfo(Path.Combine(directory.FullName, directoryName));
        }

        public static byte[] ReadAllBytes(this FileInfo file)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));

            return File.ReadAllBytes(file.FullName);
        }

        public static string[] ReadAllLines(this FileInfo file)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));

            return File.ReadAllLines(file.FullName);
        }

        public static IEnumerable<string> ReadLines(this FileInfo file)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));

            return File.ReadLines(file.FullName);
        }

        public static void WriteAllBytes(this FileInfo file, IEnumerable<byte> data)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            using var stream = file.OpenWrite();
            stream.WriteByteSequence(data);
        }

        public static void WriteAllBytes(this FileInfo file, byte[] data)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            File.WriteAllBytes(file.FullName, data);
        }

        public static void WriteAllBytes(this FileInfo file, ReadOnlyMemory<byte> data)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));

            using var stream = file.OpenWrite();
            stream.WriteBytes(data.Span);
        }

        public static void WriteAllBytes(this FileInfo file, ReadOnlySpan<byte> data)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));

            using var stream = file.OpenWrite();
            stream.WriteBytes(data);
        }

        public static void WriteAllText(this FileInfo file, string text)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException($"'{nameof(text)}' を NULL または空にすることはできません。", nameof(text));

            File.WriteAllText(file.FullName, text);
        }

        public static void WriteAllText(this FileInfo file, string text, Encoding encoding)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException($"'{nameof(text)}' を NULL または空にすることはできません。", nameof(text));
            if (encoding is null)
                throw new ArgumentNullException(nameof(encoding));

            File.WriteAllText(file.FullName, text, encoding);
        }

        public static void WriteAllText(this FileInfo file, string[] lines)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));

            File.WriteAllLines(file.FullName, lines);
        }

        public static void WriteAllText(this FileInfo file, IEnumerable<string> lines)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));

            File.WriteAllLines(file.FullName, lines);
        }

        public static void WriteAllText(this FileInfo file, string[] lines, Encoding encoding)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));
            if (encoding is null)
                throw new ArgumentNullException(nameof(encoding));

            File.WriteAllLines(file.FullName, lines, encoding);
        }

        public static void WriteAllText(this FileInfo file, IEnumerable<string> lines, Encoding encoding)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));
            if (encoding is null)
                throw new ArgumentNullException(nameof(encoding));

            File.WriteAllLines(file.FullName, lines, encoding);
        }

        public static (FileInfo File, bool AlreadyExists) RenameFile(this FileInfo sourceFile, string newFileName)
        {
            if (sourceFile is null)
                throw new ArgumentNullException(nameof(sourceFile));
            if (string.IsNullOrEmpty(newFileName))
                throw new ArgumentException($"'{nameof(newFileName)}' を NULL または空にすることはできません。", nameof(newFileName));

            var sourceFileDirectory = sourceFile.Directory;
            if (sourceFileDirectory is null)
                throw new ArgumentException($"{nameof(sourceFile)} is the relative path.", nameof(sourceFile));
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
                        return (newFile, false);
                    else if (!newFile.Exists)
                    {
                        // MoveTo メソッドは FileInfo オブジェクトを改変してしまうため、
                        // 複製してから呼び出している
                        new FileInfo(sourceFile.FullName).MoveTo(newFile.FullName);
                        return (newFile, false);
                    }
                    else if (newFile.Length == sourceFile.Length &&
                            newFile.OpenRead().StreamBytesEqual(sourceFile.OpenRead()))
                    {
                        sourceFile.Delete();
                        return (newFile, true);
                    }
                    else
                        ++retryCount;
                }
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc24(this FileInfo sourceFile, IProgress<UInt64>? progress = null)
        {
            if (sourceFile is null)
                throw new ArgumentNullException(nameof(sourceFile));

            return sourceFile.OpenRead().CalculateCrc24(progress);
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc32(this FileInfo sourceFile, IProgress<UInt64>? progress = null)
        {
            if (sourceFile is null)
                throw new ArgumentNullException(nameof(sourceFile));

            return sourceFile.OpenRead().CalculateCrc32(progress);
        }
    }
}

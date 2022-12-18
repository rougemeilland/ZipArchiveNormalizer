using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Utility;
using Utility.IO;
using ZipUtility;

namespace ZipArchiveNormalizer
{
    static class FileExtensions
    {
        private static readonly Regex _imageCollectionArchiveFileNamePattern;
        private static readonly Regex _aozoraBunkoArchiveFileNamePattern;
        private static readonly Regex _contentArchiveFileNamePattern;
        private static readonly Regex _mimeTypeEntryFileNamePattern;

        static FileExtensions()
        {
            _imageCollectionArchiveFileNamePattern = new Regex(Settings.Default.ImageCollectionArchiveFileNamePatternText, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _aozoraBunkoArchiveFileNamePattern = new Regex(Settings.Default.AozoraBunkoArchiveFileNamePatternText, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _contentArchiveFileNamePattern = new Regex(Settings.Default.ContentArchiveFileNamePatternText, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _mimeTypeEntryFileNamePattern = new Regex(Settings.Default.MimeTypeEntryNamePatternText, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        public static ArchiveType GetArchiveType(this FileInfo file)
        {
            using var zipFile = file.OpenAsZipFile();
            return file.GetArchiveType(() => zipFile.GetEntries(), item => item.FullName);
        }

        public static ArchiveType GetArchiveType<ELEMENT_T>(this FileInfo file, Func<IEnumerable<ELEMENT_T>> entriesGetter, Func<ELEMENT_T, string> entryNameSelecter)
        {
            var matchedTypes = new List<ArchiveType>();
            if (_imageCollectionArchiveFileNamePattern.IsMatch(file.Name))
                matchedTypes.Add(ArchiveType.ImageCollection);
            if (_aozoraBunkoArchiveFileNamePattern.IsMatch(file.Name))
                matchedTypes.Add(ArchiveType.AozoraBunko);
            if (_contentArchiveFileNamePattern.IsMatch(file.Name))
                matchedTypes.Add(ArchiveType.Content);
            if (matchedTypes.None())
                return ArchiveType.Unknown;
            if (matchedTypes.IsSingle())
                return matchedTypes.Single();
            if (matchedTypes.Contains(ArchiveType.Content))
            {
                if (entriesGetter().Any(entry => _mimeTypeEntryFileNamePattern.IsMatch(entryNameSelecter(entry))))
                    return ArchiveType.Content;
                else
                    matchedTypes.Remove(ArchiveType.Content);
            }
            if (matchedTypes.Count == 1)
                return matchedTypes.First();
            return ArchiveType.Unknown;
        }

        public static bool IsAozoraBunko(this FileInfo archiveFile)
        {
            return _aozoraBunkoArchiveFileNamePattern.IsMatch(archiveFile.Name);
        }

        public static void ExtractArchiveFileToLocalDirectory(this FileInfo archiveFile, Action<FileInfo> actionOnExtracted)
        {
            var success = false;
            string outputDirectoryPath = GetDestinationDirectoryPath(archiveFile);
            Directory.CreateDirectory(outputDirectoryPath);
            try
            {
                using (var sourceZipFile = archiveFile.OpenAsZipFile())
                {
                    foreach (var sourceEntry in  sourceZipFile.GetEntries().Where(entry => entry.IsFile))
                    {
                        var localFile = new FileInfo(Path.Combine(outputDirectoryPath, sourceEntry.GetRelativeLocalFilePath()));
                        if (localFile.Directory is not null)
                        {
                            localFile.Directory.Create();
                            using (var inputStream = sourceZipFile.GetContentStream(sourceEntry))
                            using (var outputStream = localFile.Create().AsOutputByteStream())
                            {
                                inputStream.CopyTo(outputStream);
                            }
                            localFile.Attributes = FileAttributes.Archive; ;
                            sourceEntry.SeTimeStampToExtractedFile(localFile.FullName);
                            try
                            {
                                actionOnExtracted(new FileInfo(localFile.FullName));
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
                archiveFile.SendToRecycleBin();
                success = true;
            }
            finally
            {
                if (!success && archiveFile.Exists)
                {
                    try
                    {
                        Directory.Delete(outputDirectoryPath, true);
                    }
                    catch (IOException)
                    {
                    }
                }
            }
        }

        public static void SendToRecycleBin(this FileInfo file)
        {
            FileSystem.DeleteFile(file.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }

        private static string GetDestinationDirectoryPath(FileInfo archiveFile)
        {
            var pattern = new Regex(@"^(?<mainpart>[^\\/]*?)(\s*\((?<suffix>[0-9]+)\))?$", RegexOptions.Compiled);
            var outputFileBasePath = archiveFile.DirectoryName ?? ".";
            var zipArchiveFileName = Path.GetFileNameWithoutExtension(archiveFile.FullName);
            var outputDirectoryPath = Path.Combine(outputFileBasePath, zipArchiveFileName);
            while (Directory.Exists(outputDirectoryPath) || File.Exists(outputDirectoryPath + ".zip"))
            {
                var matchDirectoryName = pattern.Match(Path.GetFileName(outputDirectoryPath));
                if (!matchDirectoryName.Success)
                    throw new InternalLogicalErrorException();
                var mainPart = matchDirectoryName.Groups["mainpart"].Value;
                var suffixNumber =
                    matchDirectoryName.Groups["suffix"].Success
                    ? Int32.Parse(matchDirectoryName.Groups["suffix"].Value, NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat) + 1
                    : 2;
                outputDirectoryPath =
                    Path.Combine(
                        outputFileBasePath,
                        string.Format(
                            suffixNumber == 1 ? "{0}" : "{0} ({1})",
                            mainPart,
                            suffixNumber));
            }
            return outputDirectoryPath;
        }
    }
}

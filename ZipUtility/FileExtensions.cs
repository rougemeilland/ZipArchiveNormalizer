using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Utility;

namespace ZipUtility
{
    public static class FileExtensions
    {
        private static readonly Regex _sevenZipMultiVolumeZipFileNamePattern;
        private static readonly Regex _generalMultiVolumeZipFileNamePattern;

        static FileExtensions()
        {
            _sevenZipMultiVolumeZipFileNamePattern = new Regex(@"^(?<body>[^\\/]+\.zip)\.[0-9]{3,}$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _generalMultiVolumeZipFileNamePattern = new Regex(@"^(?<body>[^\\/]+)\.zip$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        public static ZipFileCheckResult CheckZipFile(this FileInfo file, Action? progressAction = null)
        {
            return InternalCheckZipFile(file, null, progressAction);
        }

        public static ZipFileCheckResult CheckZipFile(this FileInfo file, Action<string>? detailAction, Action? progressAction = null)
        {
            return InternalCheckZipFile(file, detailAction, progressAction);
        }

        public static ZipArchiveFile OpenAsZipFile(this FileInfo sourceFile)
        {
            var sourceStream = GetSourceStreamByFileNamePattern(sourceFile);
            while (true)
            {
                var success = false;
                try
                {
                    var zipFile = ZipArchiveFile.Parse(sourceStream);
                    success = true;
                    return zipFile;
                }
                catch (MultiVolumeDetectedException ex)
                {
                    sourceStream.Dispose();
                    var lastDiskNumber = ex.LastDiskNumber;
                    sourceStream = GetSourceStreamByLastDiskNumber(sourceFile, lastDiskNumber);
                }
                finally
                {
                    if (!success)
                        sourceStream.Dispose();
                }
            }
        }

        private static ZipFileCheckResult InternalCheckZipFile(FileInfo file, Action<string>? detailAction, Action? progressAction)
        {
            try
            {
                if (progressAction is not null)
                    progressAction();
                var entryCount = 0UL;
                using (var zipFile = file.OpenAsZipFile())
                {
                    if (progressAction is not null)
                        progressAction();
                    foreach (var entry in zipFile.GetEntries())
                    {
                        zipFile.CheckEntry(entry, progressAction is not null ? new Progress<UInt64>(_ => progressAction()) : null);
                        ++entryCount;
                        if (progressAction is not null)
                            progressAction();
                    }
                }
                if (detailAction is not null)
                    detailAction(string.Format("entry count={0}", entryCount));
                return ZipFileCheckResult.Ok;
            }
            catch (EncryptedZipFileNotSupportedException ex)
            {
                if (detailAction is not null)
                    detailAction(ex.Message);
                return ZipFileCheckResult.Encrypted;
            }
            catch (CompressionMethodNotSupportedException ex)
            {
                if (detailAction is not null)
                    detailAction(ex.Message);
                return ZipFileCheckResult.UnsupportedCompressionMethod;
            }
            catch (NotSupportedSpecificationException ex)
            {
                if (detailAction is not null)
                    detailAction(ex.Message);
                return ZipFileCheckResult.UnsupportedFunction;
            }
            catch (BadZipFileFormatException ex)
            {
                if (detailAction is not null)
                    detailAction(ex.Message);
                return ZipFileCheckResult.Corrupted;
            }
        }

        private static IZipInputStream GetSourceStreamByFileNamePattern(FileInfo sourceFile)
        {
            var match = _sevenZipMultiVolumeZipFileNamePattern.Match(sourceFile.Name);
            if (match.Success)
            {
                var body = match.Groups["body"].Value;
                var files = new List<FileInfo>();
                for (var index = 1UL; index <= UInt32.MaxValue; ++index)
                {
                    var file = new FileInfo(Path.Combine(sourceFile.DirectoryName ?? ".", string.Format("{0}.{1:D3}", body, index)));
                    if (!file.Exists)
                        break;
                    files.Add(file);
                }
                return GetMultiVolumeInputStream(files.ToArray().AsReadOnly());
            }
            else
                return new SingleVolumeZipInputStream( sourceFile);
        }

        private static IZipInputStream GetSourceStreamByLastDiskNumber(FileInfo sourceFile, UInt32 lastDiskNumber)
        {
            var match = _generalMultiVolumeZipFileNamePattern.Match(sourceFile.Name);
            if (!match.Success)
                throw new NotSupportedSpecificationException("Unknown format as multi-volume ZIP file.");
            var body = match.Groups["body"].Value;
            var files = new List<FileInfo>();
            for (var index = 1U; index < lastDiskNumber; ++index)
            {

                var file = new FileInfo(Path.Combine(sourceFile.DirectoryName ?? ".", string.Format("{0}.z{1:D2}", body, index)));
                if (!file.Exists)
                    throw new BadZipFileFormatException("There is a missing disk in a multi-volume ZIP file.");
                files.Add(file);
            }
            files.Add(sourceFile);
            return GetMultiVolumeInputStream(files.ToArray().AsReadOnly());
        }

        private static IZipInputStream GetMultiVolumeInputStream(ReadOnlyMemory<FileInfo> disks)
        {
            throw new NotSupportedSpecificationException($"Not supported \"Multi-Volume Zip File\".; disk count={disks.Length}");
        }
    }
}

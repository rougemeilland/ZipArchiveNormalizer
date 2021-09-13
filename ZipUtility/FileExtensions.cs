using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZipUtility
{
    public static class FileExtensions
    {
        public static ZipFileCheckResult CheckZipFile(this FileInfo file, Action<string> detailAction = null, Action progressAction = null)
        {
            try
            {
                progressAction();
                using (var zipInputStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess))
                {
                    progressAction();
                    foreach (var entry in zipInputStream.EnumerateZipArchiveEntry())
                    {
                        entry.CheckData(zipInputStream, progressAction != null ? count => progressAction() : (Action<int>)null);
                        progressAction();
                    }
                }
                return  ZipFileCheckResult.Ok;
            }
            catch (EncryptedZipFileNotSupportedException ex)
            {
                if (detailAction != null)
                    detailAction(ex.Message);
                return ZipFileCheckResult.Encrypted;
            }
            catch (CompressionMethodNotSupportedException ex)
            {
                if (detailAction != null)
                    detailAction(ex.Message);
                return ZipFileCheckResult.UnsupportedCompressionMethod;
            }
            catch (NotSupportedSpecificationException ex)
            {
                if (detailAction != null)
                    detailAction(ex.Message);
                return ZipFileCheckResult.UnsupportedFunction;
            }
            catch (BadZipFileFormatException ex)
            {
                if (detailAction != null)
                    detailAction(ex.Message);
                return ZipFileCheckResult.Corrupted;
            }
        }

        public static IEnumerable<ZipEntry> EnumerateZipEntry(this FileInfo zipFileInfo)
        {
            using (var zipInputStream = new FileStream(zipFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return zipInputStream.EnumerateZipEntry();
            }
        }

        public static IEnumerable<ZipEntry> EnumerateZipEntry(this Stream zipInputStream)
        {
            using (var zipFile = new ZipFile(zipInputStream, true))
            {
                return zipFile.Cast<ZipEntry>().ToList();
            }
        }

        public static IEnumerable<ZipArchiveEntry> EnumerateZipArchiveEntry(this FileInfo zipFileInfo)
        {
            using (var zipInputStream = new FileStream(zipFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return zipInputStream.EnumerateZipArchiveEntry();
            }
        }

        public static IEnumerable<ZipArchiveEntry> EnumerateZipArchiveEntry(this Stream zipInputStream)
        {
            using (var zipFile = new ZipFile(zipInputStream, true))
            {
                return
                    zipFile.EnumerateZipEntry()
                    .EnumerateZipArchiveEntry(zipInputStream);
            }
        }
    }
}

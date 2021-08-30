using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZipUtility
{
    public static class FileExtensions
    {
        public static bool IsCorrectZipFile(this FileInfo file)
        {
            try
            {
                using (var zipFile = new ZipFile(file.FullName))
                {
                    return zipFile.TestArchive(true);
                }
            }
            catch (Exception)
            {
                return false;
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
            using (var zipFile = new ZipFile(zipInputStream))
            {
                return
                    zipFile.EnumerateZipEntry()
                    .EnumerateZipArchiveEntry(zipInputStream);
            }
        }
    }
}

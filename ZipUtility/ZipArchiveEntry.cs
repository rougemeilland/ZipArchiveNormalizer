using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZipUtility.ZipFileHeader;
using ZipUtility.ZipExtraField;

namespace ZipUtility
{
    public class ZipArchiveEntry
    {
        internal ZipArchiveEntry(ZipEntry zipEntry, ZipEntryLocaFilelHeader localFileyHeader)
        {
            if (zipEntry.Offset != localFileyHeader.Offset)
                throw new Exception();
            Index = localFileyHeader.Index;
            IsFile = zipEntry.IsFile;
            IsDirectory = zipEntry.IsDirectory;
            FullNameForPrimaryKey = zipEntry.Name;
            Offset = zipEntry.Offset;
            Crc = zipEntry.Crc;
            Size = zipEntry.Size;
            PackedSize = zipEntry.CompressedSize;
            IsCompressed = zipEntry.CompressionMethod != CompressionMethod.Stored;
            HostSystem = zipEntry.HostSystem;
            ExternalFileAttributes = zipEntry.ExternalFileAttributes;
            EntryTextEncoding = zipEntry.IsUnicodeText ? ZipArchiveEntryTextEncoding.UTF8 : ZipArchiveEntryTextEncoding.Local;
            ExtraFields = new ExtraFieldStorage(localFileyHeader.ExtraFields);
            LastWriteTimeUtc = localFileyHeader.LastWriteTimeUtc ?? localFileyHeader.DosTime;
            CreationTimeUtc = localFileyHeader.CreationTimeUtc;
            LastAccessTimeUtc = localFileyHeader.LastAccessTimeUtc;
            FullName = localFileyHeader.FullName ?? zipEntry.Name;
            Comment = localFileyHeader.Comment ?? zipEntry.Comment;
            FullNameBytes = localFileyHeader.FullNameBytes.ToArray();
            CommentBytes = localFileyHeader.CommentBytes.ToArray();
        }

        public long Index { get; }
        public bool IsFile { get; }
        public bool IsDirectory { get; }
        public string FullName { get; }
        public IEnumerable<byte> FullNameBytes { get; }
        public string FullNameForPrimaryKey { get; }
        public string Comment { get; }
        public IEnumerable<byte> CommentBytes { get; }
        public long Offset { get; }
        public DateTime LastWriteTimeUtc { get; }
        public DateTime? CreationTimeUtc { get; }
        public DateTime? LastAccessTimeUtc { get; }
        public long Crc { get; }
        public long Size { get; }
        public long PackedSize { get; }
        public bool IsCompressed { get; }
        public int HostSystem { get; }
        public int ExternalFileAttributes { get; }
        public ZipArchiveEntryTextEncoding EntryTextEncoding { get; }
        public ExtraFieldStorage ExtraFields { get; }

        public void SeTimeStampToExtractedFile(string extractedEntryFilePath)
        {
            try
            {
                File.SetLastWriteTimeUtc(extractedEntryFilePath, LastWriteTimeUtc);
            }
            catch (Exception)
            {
                // 対象のファイルが存在するファイルシステムと設定する時刻によって例外が発生することがあるが無視する。
            }
            try
            {
                if (CreationTimeUtc.HasValue)
                    File.SetCreationTimeUtc(extractedEntryFilePath, CreationTimeUtc.Value);
            }
            catch (Exception)
            {
                // 対象のファイルが存在するファイルシステムと設定する時刻によって例外が発生することがあるが無視する。
            }
            try
            {
                if (LastAccessTimeUtc.HasValue)
                    File.SetLastAccessTimeUtc(extractedEntryFilePath, LastAccessTimeUtc.Value);
            }
            catch (Exception)
            {
                // 対象のファイルが存在するファイルシステムと設定する時刻によって例外が発生することがあるが無視する。
            }
        }
    }
}
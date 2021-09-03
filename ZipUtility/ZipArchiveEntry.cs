using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using Utility;
using ZipUtility.ZipExtraField;
using ZipUtility.ZipFileHeader;

namespace ZipUtility
{
    public class ZipArchiveEntry
    {
        internal ZipArchiveEntry(ZipEntry zipEntry, ZipEntryHeader internalHeader)
        {
            Index = internalHeader.CentralDirectoryHeader.Index;
            GeneralPurposeBitFlag = internalHeader.LocalFileHeader.GeneralPurposeBitFlag;
            IsFile = zipEntry.IsFile;
            IsDirectory = zipEntry.IsDirectory;
            IsEncrypted =
                internalHeader.LocalFileHeader.GeneralPurposeBitFlag.HasFlag(ZipEntryGeneralPurposeBitFlag.IsEncrypted | ZipEntryGeneralPurposeBitFlag.IsStrongEncrypted | ZipEntryGeneralPurposeBitFlag.IsMoreStrongEncrypted);
            FullNameForPrimaryKey = internalHeader.LocalFileHeader.OriginalFullName?.Replace(@"\", "/") ?? zipEntry.Name;

            Offset = internalHeader.CentralDirectoryHeader.LocalFileHeaderOffset;
            Crc = internalHeader.LocalFileHeader.Crc;
            Size = internalHeader.LocalFileHeader.Size;
            PackedSize = internalHeader.LocalFileHeader.PackedSize;
            CompressionMethod = internalHeader.LocalFileHeader.CompressionMethod;
            HostSystem = internalHeader.CentralDirectoryHeader.HostSystem;
            ExternalFileAttributes = internalHeader.CentralDirectoryHeader.ExternalFileAttributes;
            EntryTextEncoding =
                internalHeader.LocalFileHeader.GeneralPurposeBitFlag.HasFlag(ZipEntryGeneralPurposeBitFlag.UseUnicodeEncodingForNameAndComment)
                ? ZipEntryTextEncoding.UTF8Encoding
                : ZipEntryTextEncoding.LocalEncoding;
            ExtraFields = new ExtraFieldStorage(internalHeader.LocalFileHeader.ExtraFields);
            LastWriteTimeUtc =
                internalHeader.LocalFileHeader.LastWriteTimeUtc
                ?? internalHeader.CentralDirectoryHeader.LastWriteTimeUtc
                ?? internalHeader.LocalFileHeader.DosDateTime
                ?? internalHeader.CentralDirectoryHeader.DosDateTime;
            LastAccessTimeUtc =
                internalHeader.LocalFileHeader.LastAccessTimeUtc
                ?? internalHeader.CentralDirectoryHeader.LastAccessTimeUtc;
            CreationTimeUtc =
                internalHeader.LocalFileHeader.CreationTimeUtc
                ?? internalHeader.CentralDirectoryHeader.CreationTimeUtc;
            FullNameBytes = internalHeader.LocalFileHeader.FullNameBytes;
            CommentBytes = internalHeader.LocalFileHeader.CommentBytes;
            FullName = internalHeader.LocalFileHeader.FullName?.Replace(@"\", "/") ?? zipEntry.Name;
            Comment = internalHeader.LocalFileHeader.Comment ?? zipEntry.Comment;
#if DEBUG
            Func<DateTime?, DateTime?, bool> dateTimeEqualityComparer =
                (dateTime1, dateTime2) =>
                {
                    if (dateTime1 == null)
                        return dateTime2 == null;
                    else if (dateTime2 == null)
                        return false;
                    else
                        return dateTime1.Value.Equals(dateTime2.Value);
                };
            if (Offset != zipEntry.Offset)
                throw new Exception();
            if (Crc != zipEntry.Crc)
                throw new Exception();
            if (Size != zipEntry.Size)
                throw new Exception();
            if (PackedSize != zipEntry.CompressedSize)
                throw new Exception();
            if (FullNameForPrimaryKey != zipEntry.Name)
                throw new Exception();
            if (CompressionMethod.HasValue && (UInt16)CompressionMethod.Value != (UInt16)zipEntry.CompressionMethod)
                throw new Exception();
            if ((byte)HostSystem != (byte)zipEntry.HostSystem)
                throw new Exception();
            if ((UInt32)ExternalFileAttributes != (UInt32)zipEntry.ExternalFileAttributes)
                throw new Exception();
            if ((EntryTextEncoding == ZipEntryTextEncoding.UTF8Encoding) != zipEntry.IsUnicodeText)
                throw new Exception();
            if (dateTimeEqualityComparer(internalHeader.LocalFileHeader.DosDateTime, internalHeader.CentralDirectoryHeader.DosDateTime) == false)
                throw new Exception();
            if (internalHeader.CentralDirectoryHeader.GeneralPurposeBitFlag != internalHeader.LocalFileHeader.GeneralPurposeBitFlag)
                throw new Exception();
            if (!dateTimeEqualityComparer(internalHeader.CentralDirectoryHeader.DosDateTime, internalHeader.LocalFileHeader.DosDateTime))
                throw new Exception();
            if (internalHeader.CentralDirectoryHeader.CompressionMethod != internalHeader.LocalFileHeader.CompressionMethod)
                throw new Exception();
            if (!internalHeader.CentralDirectoryHeader.FullNameBytes.SequenceEqual(internalHeader.LocalFileHeader.FullNameBytes))
                throw new Exception();
            if (internalHeader.CentralDirectoryHeader.FullName != internalHeader.LocalFileHeader.FullName)
                throw new Exception();
#endif
        }

        public long Index { get; }
        public ZipEntryGeneralPurposeBitFlag GeneralPurposeBitFlag { get; }
        public bool IsFile { get; }
        public bool IsDirectory { get; }
        public bool IsEncrypted { get; }
        public string FullNameForPrimaryKey { get; }
        public long Offset { get; }
        public long Crc { get; }
        public long Size { get; }
        public long PackedSize { get; }
        public ZipEntryCompressionMethod? CompressionMethod { get; }
        public ZipEntryHostSystem HostSystem { get; }
        public UInt32 ExternalFileAttributes { get; }
        public ZipEntryTextEncoding EntryTextEncoding { get; }
        public ExtraFieldStorage ExtraFields { get; }
        public DateTime? LastWriteTimeUtc { get; }
        public DateTime? LastAccessTimeUtc { get; }
        public DateTime? CreationTimeUtc { get; }
        public IReadOnlyArray<byte> FullNameBytes { get; }
        public IReadOnlyArray<byte> CommentBytes { get; }
        public string FullName { get; }
        public string Comment { get; }

        public void SeTimeStampToExtractedFile(string extractedEntryFilePath)
        {
            try
            {
                if (LastWriteTimeUtc.HasValue)
                    File.SetLastWriteTimeUtc(extractedEntryFilePath, LastWriteTimeUtc.Value);
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
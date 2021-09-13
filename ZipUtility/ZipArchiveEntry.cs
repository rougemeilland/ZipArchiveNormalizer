using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Linq;
using Utility;
using Utility.IO;
using ZipUtility.ZipExtraField;
using ZipUtility.ZipFileHeader;

namespace ZipUtility
{
    public class ZipArchiveEntry
    {
        private ZipEntryCompressionMethod _compressionMethod;
        private long _dataOffset;
        private bool? _dataIsOk;

        internal ZipArchiveEntry(ZipEntry zipEntry, ZipEntryHeader internalHeader)
        {
            Index = internalHeader.CentralDirectoryHeader.Index;
            IsDirectory = internalHeader.CentralDirectoryHeader.IsDirectiry;
            FullNameForPrimaryKey = internalHeader.LocalFileHeader.OriginalFullName;

            Offset = internalHeader.CentralDirectoryHeader.LocalFileHeaderOffset;
            Crc = internalHeader.CentralDirectoryHeader.Crc;
            Size = internalHeader.CentralDirectoryHeader.Size;
            PackedSize = internalHeader.CentralDirectoryHeader.PackedSize;
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
            FullName = internalHeader.LocalFileHeader.FullName;
            FullNameCanBeExpressedInUnicode = internalHeader.LocalFileHeader.FullNameCanBeExpressedInUnicode;
            FullNameCanBeExpressedInStandardEncoding = internalHeader.LocalFileHeader.FullNameCanBeExpressedInStandardEncoding;
            Comment = internalHeader.CentralDirectoryHeader.Comment;
            CommentCanBeExpressedInUnicode = internalHeader.CentralDirectoryHeader.CommentCanBeExpressedInUnicode;
            CommentCanBeExpressedInStandardEncoding = internalHeader.CentralDirectoryHeader.CommentCanBeExpressedInStandardEncoding;
            _compressionMethod = internalHeader.LocalFileHeader.CompressionMethod.GetCompressionMethod(internalHeader.LocalFileHeader.GeneralPurposeBitFlag);
            _dataOffset = internalHeader.LocalFileHeader.DataOffset;
            _dataIsOk = internalHeader.LocalFileHeader.IsCheckedCrc ? (bool?)true : null;
            if (HostSystem.IsAnyOf(ZipEntryHostSystem.FAT, ZipEntryHostSystem.VFAT, ZipEntryHostSystem.Windows_NTFS, ZipEntryHostSystem.OS2_HPFS))
            {
                FullNameForPrimaryKey = FullNameForPrimaryKey.Replace(@"\", "/");
                FullName = FullName.Replace(@"\", "/");
            }
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
            if ((UInt16)CompressionMethod != (UInt16)zipEntry.CompressionMethod)
                throw new Exception();
            if ((byte)HostSystem != (byte)zipEntry.HostSystem)
                throw new Exception();
            if ((UInt32)ExternalFileAttributes != (UInt32)zipEntry.ExternalFileAttributes)
                throw new Exception();
            if ((EntryTextEncoding == ZipEntryTextEncoding.UTF8Encoding) != zipEntry.IsUnicodeText)
                throw new Exception();
            if (internalHeader.LocalFileHeader.DosDateTime.HasValue && dateTimeEqualityComparer(internalHeader.LocalFileHeader.DosDateTime, internalHeader.CentralDirectoryHeader.DosDateTime) == false)
                throw new Exception();
            if (internalHeader.CentralDirectoryHeader.GeneralPurposeBitFlag != internalHeader.LocalFileHeader.GeneralPurposeBitFlag)
                throw new Exception();
            if (internalHeader.CentralDirectoryHeader.CompressionMethod != internalHeader.LocalFileHeader.CompressionMethod)
                throw new Exception();
            if (internalHeader.CentralDirectoryHeader.Crc != internalHeader.LocalFileHeader.Crc)
                throw new Exception();
            if (internalHeader.CentralDirectoryHeader.Size != internalHeader.LocalFileHeader.Size)
                throw new Exception();
            if (internalHeader.CentralDirectoryHeader.PackedSize != internalHeader.LocalFileHeader.PackedSize)
                throw new Exception();
            if (!internalHeader.CentralDirectoryHeader.FullNameBytes.SequenceEqual(internalHeader.LocalFileHeader.FullNameBytes))
                throw new Exception();
            if (!string.Equals(internalHeader.CentralDirectoryHeader.FullName, internalHeader.LocalFileHeader.FullName, StringComparison.Ordinal))
                throw new Exception();
            if (internalHeader.CentralDirectoryHeader.FullNameCanBeExpressedInUnicode != internalHeader.LocalFileHeader.FullNameCanBeExpressedInUnicode)
                throw new Exception();
            if (internalHeader.CentralDirectoryHeader.FullNameCanBeExpressedInStandardEncoding != internalHeader.LocalFileHeader.FullNameCanBeExpressedInStandardEncoding)
                throw new Exception();
#endif
        }

        public long Index { get; }
        public bool IsFile => !IsDirectory;
        public bool IsDirectory { get; }

        /// <summary>
        /// このエントリのエントリ名。
        /// <see cref="FullName"/>との違いは、
        /// 汎用フラグbit11のみによりエンコーディングの判断を行っているアプリケーションとの連携のために
        /// <see cref="FullNameForPrimaryKey"/>では拡張フィールドその他によるエンコーディングの判別を行っていないこと。
        /// </summary>
        public string FullNameForPrimaryKey { get; }
        public long Offset { get; }
        public long Crc { get; }
        public long Size { get; }
        public long PackedSize { get; }
        public ZipEntryCompressionMethodId CompressionMethod { get => _compressionMethod.CompressionMethodId; }
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
        public bool FullNameCanBeExpressedInUnicode { get; }
        public bool FullNameCanBeExpressedInStandardEncoding { get; }
        public string Comment { get; }
        public bool CommentCanBeExpressedInUnicode { get; }
        public bool CommentCanBeExpressedInStandardEncoding { get; }

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

        internal void CheckData(Stream zipInputStream, Action<int> progressAction)
        {
            if (IsFile == false)
                return;
            if (_dataIsOk == true)
                return;
            if (_dataIsOk == false)
                throw new BadZipFileFormatException(string.Format("Bad entry data: index={0}, name=\"{1}\"", Index, FullName));
            try
            {
                var actualCrc = _compressionMethod.GetInputStream(zipInputStream, _dataOffset, PackedSize, Size, true).GetByteSequence(progressAction: progressAction).CalculateCrc32();
                if (actualCrc != Crc)
                    throw
                        new BadZipFileFormatException(
                            string.Format(
                                "Bad entry data: index={0}, name=\"{1}\", desired crc=0x{2:x8}, actual crc=0x{3:x8}",
                                Index,
                                FullName,
                                Crc,
                                actualCrc));
                _dataIsOk = true;
            }
            finally
            {
                if (_dataIsOk == null)
                    _dataIsOk = false;
            }
        }
    }
}
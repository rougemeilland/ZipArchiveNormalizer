﻿using System;
using System.IO;
using Utility;
using Utility.IO;
using ZipUtility.ZipExtraField;
using ZipUtility.ZipFileHeader;

namespace ZipUtility
{
    public readonly struct ZipArchiveEntry
    {
        private readonly ZipEntryCompressionMethod _compressionMethod;

        private readonly bool? _dataIsOk;

        internal ZipArchiveEntry(ZipEntryHeader internalHeader, UInt64 localFileHeaderOrder, Int64 zipFileInstanceId)
        {
            Index = internalHeader.CentralDirectoryHeader.Index;
            Order = localFileHeaderOrder;
            IsDirectory = internalHeader.CentralDirectoryHeader.IsDirectiry;
            FullNameForPrimaryKey = internalHeader.LocalFileHeader.OriginalFullName;

            LocalFileHeaderPosition = internalHeader.CentralDirectoryHeader.LocalFileHeaderPosition;
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
            DataPosition = internalHeader.LocalFileHeader.DataPosition;
            _dataIsOk = internalHeader.LocalFileHeader.IsCheckedCrc ? true : null;
            if (HostSystem.IsAnyOf(ZipEntryHostSystem.FAT, ZipEntryHostSystem.VFAT, ZipEntryHostSystem.Windows_NTFS, ZipEntryHostSystem.OS2_HPFS))
            {
                FullNameForPrimaryKey = FullNameForPrimaryKey.Replace(@"\", "/");
                FullName = FullName.Replace(@"\", "/");
            }
            ZipFileInstanceId = zipFileInstanceId;
        }

        /// <summary>
        /// Order of appearance in central directory headers (0, 1, 2, ...)
        /// </summary>
        public UInt64 Index { get; }

        /// <summary>
        /// Order of appearance in local file headers (0, 1, 2, ...)
        /// </summary>
        /// <remarks>
        /// <para>
        /// In most cases, sorting by the <see cref="Index"/> property and sorting by the <see cref="Order"/> property will be in the same order.
        /// Use the <see cref="Index"/> property except for special purposes.
        /// </para>
        /// <para>
        /// Sorting the entries by this property is rarely common.
        /// However, for example, sorting by the <see cref="Order"/> property may be effective in the following cases.
        /// </para>
        /// <para>
        /// 1) When decompressing multiple files included in ZIP continuously.
        /// </para>
        /// <para>
        /// 2) If a particular file must be at the beginning of a ZIP file and you want to check if it meets that requirement (such as a ".epub" file)
        /// </para>
        /// </remarks>
        public UInt64 Order { get; }

        public bool IsFile => !IsDirectory;
        public bool IsDirectory { get; }

        /// <summary>
        /// このエントリのエントリ名。
        /// <see cref="FullName"/> との違いは、
        /// 汎用フラグbit11のみによりエンコーディングの判断を行っているアプリケーションとの連携のために
        /// <see cref="FullNameForPrimaryKey"/> では拡張フィールドその他によるエンコーディングの判別を行っていないこと。
        /// </summary>
        public string FullNameForPrimaryKey { get; }
        public Int64 Crc { get; }
        public UInt64 Size { get; }
        public UInt64 PackedSize { get; }
        public ZipEntryCompressionMethodId CompressionMethod { get => _compressionMethod.CompressionMethodId; }
        public ZipEntryHostSystem HostSystem { get; }
        public UInt32 ExternalFileAttributes { get; }
        public ZipEntryTextEncoding EntryTextEncoding { get; }
        public ExtraFieldStorage ExtraFields { get; }
        public DateTime? LastWriteTimeUtc { get; }
        public DateTime? LastAccessTimeUtc { get; }
        public DateTime? CreationTimeUtc { get; }
        public ReadOnlyMemory<byte> FullNameBytes { get; }
        public ReadOnlyMemory<byte> CommentBytes { get; }
        public string FullName { get; }
        public bool FullNameCanBeExpressedInUnicode { get; }
        public bool FullNameCanBeExpressedInStandardEncoding { get; }
        public string Comment { get; }
        public bool CommentCanBeExpressedInUnicode { get; }
        public bool CommentCanBeExpressedInStandardEncoding { get; }

        public void SeTimeStampToExtractedFile(string extractedEntryFilePath)
        {
            if (string.IsNullOrEmpty(extractedEntryFilePath))
                throw new ArgumentException($"'{nameof(extractedEntryFilePath)}' を NULL または空にすることはできません。", nameof(extractedEntryFilePath));

            try
            {
                if (LastWriteTimeUtc is not null)
                    File.SetLastWriteTimeUtc(extractedEntryFilePath, LastWriteTimeUtc.Value);
            }
            catch (Exception)
            {
                // 対象のファイルが存在するファイルシステムと設定する時刻によって例外が発生することがあるが無視する。
            }
            try
            {
                if (CreationTimeUtc is not null)
                    File.SetCreationTimeUtc(extractedEntryFilePath, CreationTimeUtc.Value);
            }
            catch (Exception)
            {
                // 対象のファイルが存在するファイルシステムと設定する時刻によって例外が発生することがあるが無視する。
            }
            try
            {
                if (LastAccessTimeUtc is not null)
                    File.SetLastAccessTimeUtc(extractedEntryFilePath, LastAccessTimeUtc.Value);
            }
            catch (Exception)
            {
                // 対象のファイルが存在するファイルシステムと設定する時刻によって例外が発生することがあるが無視する。
            }
        }

        internal Int64 ZipFileInstanceId { get; }
        internal ZipStreamPosition LocalFileHeaderPosition { get; }
        internal ZipStreamPosition DataPosition { get; }

        internal IInputByteStream<UInt64> GetContentStream(IZipInputStream zipFileStream, IProgress<UInt64>? progress)
        {
            return
                _compressionMethod.GetDecodingStream(
                    zipFileStream.AsPartial(DataPosition, PackedSize),
                    Size,
                    PackedSize,
                    progress);
        }

        internal void CheckData(IZipInputStream zipInputStream, IProgress<UInt64>? progress)
        {
            if (!IsFile)
                return;
            if (_dataIsOk == true)
                return;
            if (_dataIsOk == false)
                throw new BadZipFileFormatException(string.Format("Bad entry data: index={0}, name=\"{1}\"", Index, FullName));
            var actualCrc = _compressionMethod.CalculateCrc32(zipInputStream, DataPosition, Size, PackedSize, progress).Crc;
            if (actualCrc != Crc)
                throw
                    new BadZipFileFormatException(
                        string.Format(
                            "Bad entry data: index={0}, name=\"{1}\", desired crc=0x{2:x8}, actual crc=0x{3:x8}",
                            Index,
                            FullName,
                            Crc,
                            actualCrc));
        }
    }
}

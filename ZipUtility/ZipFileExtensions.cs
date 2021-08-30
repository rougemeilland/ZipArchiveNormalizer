﻿using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ZipUtility.ZipExtraField;
using ZipUtility.ZipFileHeader;

namespace ZipUtility
{
    public static class ZipFileExtensions
    {
        private static Regex _localFilePathReplacePattern;

        static ZipFileExtensions()
        {
            _localFilePathReplacePattern = new Regex(@"(?<rep>[:\*\?""<>\|])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        public static IEnumerable<ZipEntry> EnumerateZipEntry(this ZipFile zipFile)
        {
            return zipFile.Cast<ZipEntry>().ToList();
        }

        public static IEnumerable<ZipArchiveEntry> EnumerateZipArchiveEntry(this ZipFile zipFile, Stream zipFileInputStream)
        {
            return
                zipFile
                .EnumerateZipEntry()
                .EnumerateZipArchiveEntry(zipFileInputStream);
        }

        public static IEnumerable<ZipArchiveEntry> EnumerateZipArchiveEntry(this IEnumerable<ZipEntry> zipEntries, Stream ziInputStream)
        {
            var zipEntriesArray =
                zipEntries
                .ToArray();
            var localHeaders =
                ZipFileInfo.Parse(ziInputStream).EnumerateEntry(ziInputStream)
                .ToArray();
            if (zipEntriesArray.Length != localHeaders.Length)
                throw new Exception();
            return
                Enumerable.Range(0, zipEntriesArray.Length)
                .Select(index => new ZipArchiveEntry(zipEntriesArray[index], localHeaders[index]))
                .ToList();
        }

        public static Stream GetInputStream(this ZipFile zipFile, ZipArchiveEntry entry)
        {
            return zipFile.GetInputStream(entry.Index);
        }

        public static Stream GetInputStream(this ZipFile zipFile, string entryFullName)
        {
            return zipFile.GetInputStream(new ZipEntry(entryFullName));
        }

        public static ZipEntry CreateDesdinationEntry(this ZipArchiveEntry entry, string renamedEntryFullPath = null)
        {
            var newEntry = new ZipEntry(renamedEntryFullPath ?? entry.FullName);

            // 基本属性の設定
            newEntry.Comment = entry.Comment;
            newEntry.HostSystem = entry.HostSystem;
            newEntry.ExternalFileAttributes = entry.ExternalFileAttributes;
            newEntry.Size = entry.Size; // これをセットしないと 出力先エントリに Zip64 拡張属性が勝手についてしまう

            // mimetype ファイルの場合は圧縮しない
            if (entry.FullName == "mimetype")
                newEntry.CompressionMethod = CompressionMethod.Stored;

            // NameまたはCommentのどちらかがマルチバイトを含む場合は、文字コード系をUTF8に変更する。
            newEntry.IsUnicodeText =
                !newEntry.Name.IsConvertableToMinimumCharacterSet() ||
                !newEntry.Comment.IsConvertableToMinimumCharacterSet();

            // 更新日付の設定
            newEntry.SetLastModificationTime(entry.LastWriteTimeUtc);

            // extra data の設定の開始 (基本的に元の extra field を引き継ぐ)
            var newExtraData = new ExtraFieldStorage(entry.ExtraFields);

            // Extended Timestamp extra field が存在しない、あるいは
            // Extended Timestamp extra field に最終更新日時が存在しない、あるいは
            // 最終アクセス日時が設定されているが Extended Timestamp extra field には最終アクセス日時が存在しない、あるいは
            // 作成日時が設定されているが Extended Timestamp extra field には作成日時存在しない場合は
            // Extended Timestamp extra field を追加する
            var extendedTimestampExtraField = newExtraData.GetData<ExtendedTimestampExtraField>();
            if (extendedTimestampExtraField == null ||
                extendedTimestampExtraField.LastWriteTimeUtc == null ||
                (entry.LastAccessTimeUtc.HasValue && extendedTimestampExtraField.LastAccessTimeUtc == null) ||
                (entry.CreationTimeUtc.HasValue && extendedTimestampExtraField.CreationTimeUtc == null))
            {
                newExtraData.Delete(ExtendedTimestampExtraField.ExtraFieldId);
                newExtraData.AddEntry(new ExtendedTimestampExtraField
                {
                    LastWriteTimeUtc = entry.LastWriteTimeUtc,
                    LastAccessTimeUtc = entry.LastAccessTimeUtc,
                    CreationTimeUtc = entry.CreationTimeUtc,
                });
            }

            // 最終アクセス日時と作成日時がともに設定されていて、かつ NTFS extra field が存在しない場合
            // NTFS extra field を追加する
            if (entry.LastAccessTimeUtc.HasValue &&
                entry.CreationTimeUtc.HasValue &&
                !newExtraData.Contains(NtfsExtraField.ExtraFieldId))
            {
                newExtraData.AddEntry(new NtfsExtraField
                {
                    LastWriteTimeUtc = entry.LastWriteTimeUtc,
                    LastAccessTimeUtc = entry.LastAccessTimeUtc.Value,
                    CreationTimeUtc = entry.CreationTimeUtc.Value,
                });
            }

            // Code Page Extra Field (仮称) を削除する
            newExtraData.Delete(CodePageExtraField.ExtraFieldId);

            // Unicode Path Extra Field を削除する
            newExtraData.Delete(UnicodePathExtraField.ExtraFieldId);

            // Unicode Comment Extra Field を削除する
            newExtraData.Delete(UnicodeCommentExtraField.ExtraFieldId);

            // 編集した extra field を保存先に格納する
            newEntry.ExtraData = newExtraData.ToByteSequence().ToArray();

            return newEntry;
        }

        public static DateTime GetLastModificationTime(this ZipEntry entry)
        {
            // entry.DateTime の Kind は Unspecified だが、 Local とみなして取得する
            return
                new DateTime(
                    entry.DateTime.Year,
                    entry.DateTime.Month,
                    entry.DateTime.Day,
                    entry.DateTime.Hour,
                    entry.DateTime.Minute,
                    entry.DateTime.Second,
                    DateTimeKind.Local)
                .ToUniversalTime();
        }

        public static void SetLastModificationTime(this ZipEntry entry, DateTime datetime)
        {
#if DEBUG
            if (datetime.ToLocalTime() != datetime.ToLocalTime().ToLocalTime())
                throw new Exception();
#endif
            // entry.DateTime 内では Kind は参照されずに DosTime の計算に使用されているので、代入前にあらかじめ Local Time に変更しておく。
            entry.DateTime = datetime.ToLocalTime();
        }

        public static string GetRelativeLocalFilePath(this ZipEntry entry)
        {
            return entry.Name.GetRelativeLocalFilePath();
        }

        public static string GetRelativeLocalFilePath(this ZipArchiveEntry entry)
        {
            return entry.FullName.GetRelativeLocalFilePath();
        }

        private static string GetRelativeLocalFilePath(this string entryPath)
        {
            return
                string.Join(
                    @"\",
                    entryPath.Split('/', '\\')
                    .Select(element =>
                        _localFilePathReplacePattern.Replace(
                            element,
                            m =>
                            {
                                switch (m.Groups["rep"].Value)
                                {
                                    case @":":
                                        return "：";
                                    case @"*":
                                        return "＊";
                                    case @"?":
                                        return "？";
                                    case @"""":
                                        return "”";
                                    case @"<":
                                        return "＜";
                                    case @">":
                                        return "＞";
                                    case @"|":
                                        return "｜";
                                    default:
                                        return m.Value;
                                }

                            })));
        }
    }
}
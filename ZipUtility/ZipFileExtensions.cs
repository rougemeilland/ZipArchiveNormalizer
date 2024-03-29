﻿using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using ZipUtility.ZipExtraField;

namespace ZipUtility
{
    public static class ZipFileExtensions
    {
        private static readonly Regex _localFilePathReplacePattern;

        static ZipFileExtensions()
        {
            _localFilePathReplacePattern = new Regex(@"(?<rep>[:\*\?""<>\|])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }


        public static ZipEntry CreateDesdinationEntry(this ZipArchiveEntry entry, string? renamedEntryFullPath = null)
        {
            // 基本属性の設定
            var newEntry = new ZipEntry(renamedEntryFullPath ?? entry.FullName)
            {
                Comment = entry.Comment,
                HostSystem = (Int32)entry.HostSystem,
                ExternalFileAttributes = (Int32)entry.ExternalFileAttributes,
                Size = (Int64)entry.Size
            };

            // mimetype ファイルの場合は圧縮しない
            if (entry.FullName == "mimetype" || entry.IsDirectory || entry.Size == 0)
                newEntry.CompressionMethod = CompressionMethod.Stored;


#if DEBUG
            if (!entry.FullNameCanBeExpressedInUnicode || entry.CommentCanBeExpressedInUnicode)
                throw new Exception();
#endif
            // NameまたはCommentのどちらかがマルチバイトを含む場合は、文字コード系をUTF8に変更する。
            newEntry.IsUnicodeText =
                !entry.FullNameCanBeExpressedInStandardEncoding ||
                !entry.CommentCanBeExpressedInStandardEncoding;

            // 更新日付の設定
            if (entry.LastWriteTimeUtc is not null)
                newEntry.SetLastModificationTime(entry.LastWriteTimeUtc.Value);

            // extra data の設定の開始 (基本的に元の extra field を引き継ぐ)
            var newExtraData = new ExtraFieldStorage(entry.ExtraFields);

            // Extended Timestamp extra field に最終更新日時が存在しない、あるいは
            // 最終アクセス日時が設定されているが Extended Timestamp extra field には最終アクセス日時が存在しない、あるいは
            // 作成日時が設定されているが Extended Timestamp extra field には作成日時存在しない場合は
            // Extended Timestamp extra field を追加する
            var extendedTimestampExtraField = newExtraData.GetData<ExtendedTimestampExtraField>();
            if ((entry.LastWriteTimeUtc is not null && extendedTimestampExtraField?.LastWriteTimeUtc is null) ||
                (entry.LastAccessTimeUtc is not null && extendedTimestampExtraField?.LastAccessTimeUtc is null) ||
                (entry.CreationTimeUtc is not null && extendedTimestampExtraField?.CreationTimeUtc is null))
            {
                newExtraData.Delete(ExtendedTimestampExtraField.ExtraFieldId);
                newExtraData.AddEntry(new ExtendedTimestampExtraField
                {
                    LastWriteTimeUtc = entry.LastWriteTimeUtc,
                    LastAccessTimeUtc = entry.LastAccessTimeUtc,
                    CreationTimeUtc = entry.CreationTimeUtc,
                });
            }

            // 最終更新日時がと最終アクセス日時、作成日時がすべて設定されていて、かつ NTFS extra field が存在しない場合
            // NTFS extra field を追加する
            if (entry.LastWriteTimeUtc is not null &&
                entry.LastAccessTimeUtc is not null &&
                entry.CreationTimeUtc is not null &&
                !newExtraData.Contains(NtfsExtraField.ExtraFieldId))
            {
                newExtraData.AddEntry(new NtfsExtraField
                {
                    LastWriteTimeUtc = entry.LastWriteTimeUtc,
                    LastAccessTimeUtc = entry.LastAccessTimeUtc,
                    CreationTimeUtc = entry.CreationTimeUtc,
                });
            }

            // Xceed unicode extra field を削除する
            newExtraData.Delete(XceedUnicodeExtraField.ExtraFieldId);

            // Code Page Extra Field (仮称) を削除する
            newExtraData.Delete(CodePageExtraField.ExtraFieldId);

            // Unicode Path Extra Field を削除する
            newExtraData.Delete(UnicodePathExtraField.ExtraFieldId);

            // Unicode Comment Extra Field を削除する
            newExtraData.Delete(UnicodeCommentExtraField.ExtraFieldId);

            // 編集した extra field を保存先に格納する
            newEntry.ExtraData = newExtraData.ToByteArray().ToArray();

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
                            m => m.Groups["rep"].Value switch
                            {
                                @":" => "：",
                                @"*" => "＊",
                                @"?" => "？",
                                @"""" => "”",
                                @"<" => "＜",
                                @">" => "＞",
                                @"|" => "｜",
                                _ => m.Value,
                            })));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Utility;
using ZipUtility.ZipExtraField;

namespace ZipUtility.ZipFileHeader
{
    class ZipEntryLocaFilelHeader
    {
        private static byte[] _localHeaderSignature;
        private static Encoding _utf8EncodingWithouBOM;

        static ZipEntryLocaFilelHeader()
        {
            _localHeaderSignature = new byte[] { 0x50, 0x4b, 0x03, 0x04 };
            _utf8EncodingWithouBOM = new UTF8Encoding(false);
        }

        private ZipEntryLocaFilelHeader(ZipEntryCentralDirectoryHeader centralDirectoryHeader, ZipFileEntryGeneralPurposeBitFlag generalPurposeBitFlag, IReadOnlyArray<byte> fullNameBytes, IReadOnlyArray<byte> commentBytes, IReadOnlyArray<byte> extraDataSource, ExtraFieldStorage extraFieldsOnCentralDirectoryHeader)
        {
            Index = centralDirectoryHeader.Index;
            Offset = centralDirectoryHeader.LocalFileHeaderOffset;
            GeneralPurposeBitFlag = generalPurposeBitFlag;
            DosTime = centralDirectoryHeader.DosTime;
            FullNameBytes = fullNameBytes;
            CommentBytes = commentBytes;
            ExtraFields = new ExtraFieldStorage(ZipEntryHeaderType.LocalFileHeader, extraFieldsOnCentralDirectoryHeader, extraDataSource);
 
            // 日付に関する extra field を検索して日付を設定する (上から順に優先される)
            var timeStampExtraFields = new[]
            {
                ExtraFields.GetData<NtfsExtraField>() as ITimestampExtraField,
                ExtraFields.GetData<ExtendedTimestampExtraField>(),
                ExtraFields.GetData<UnixExtraFieldType1>(),
                ExtraFields.GetData<UnixExtraFieldType0>(),
            }
            .Where(timeStampExtraField => timeStampExtraField != null);
            LastWriteTimeUtc = null;
            LastAccessTimeUtc = null;
            CreationTimeUtc = null;
            foreach (var timeStampExtraField in timeStampExtraFields)
            {
                if (LastWriteTimeUtc == null && timeStampExtraField.LastWriteTimeUtc.HasValue)
                    LastWriteTimeUtc = timeStampExtraField.LastWriteTimeUtc.Value;
                if (LastAccessTimeUtc == null && timeStampExtraField.LastAccessTimeUtc.HasValue)
                    LastAccessTimeUtc = timeStampExtraField.LastAccessTimeUtc.Value;
                if (CreationTimeUtc == null && timeStampExtraField.CreationTimeUtc.HasValue)
                    CreationTimeUtc = timeStampExtraField.CreationTimeUtc.Value;
            }

            // エントリ名とコメントを設定する
            if ((GeneralPurposeBitFlag & ZipFileEntryGeneralPurposeBitFlag.UseUnicodeEncodingForNameAndComment) != ZipFileEntryGeneralPurposeBitFlag.None)
            {
                // ローカルヘッダのフラグでパス名とコメントにUNICODEを使用するように求められている場合
                // エントリ名とコメントのエンコーディングにUTF8を採用する
                FullName = _utf8EncodingWithouBOM.GetString(fullNameBytes.ToArray());
                Comment = _utf8EncodingWithouBOM.GetString(commentBytes.ToArray());
            }
            else
            {
                // ローカルヘッダのフラグでパス名とコメントにUNICODEを使用するように求められていない場合
                var codePageExtraField = ExtraFields.GetData<CodePageExtraField>();
                if (codePageExtraField != null)
                {
                    // 0xe57a extra field (名称不明) にて、使用するコードページが指定されていた場合
                    // 指定されたコードページのエンコーディングを採用する
                    var encoding = Encoding.GetEncoding(codePageExtraField.CodePage);
                    FullName = encoding.GetString(fullNameBytes.ToArray());
                    Comment = encoding.GetString(commentBytes.ToArray());
                }
                else
                {
                    // extra field にて、使用するコードページが指定されていない場合

                    var unicodePathExtraField = ExtraFields.GetData<UnicodePathExtraField>();
                    if (unicodePathExtraField != null && fullNameBytes.IsMatchCrc(unicodePathExtraField.Crc))
                    {
                        // Unicode Path Extra Field が付加されていて、かつCRCが一致している場合
                        // Unicode Path Extra Field 上の文字列をエントリ名として採用する
                        FullName = unicodePathExtraField.FullName;
                    }
                    else
                    {
                        // Unicode Path Extra Field が付加されていないか、またはCRCが一致しない場合
                        // 特にエンコーディングが定められていないので、コンピュータ上の既定のエンコーディングを採用する。
                        FullName = Encoding.Default.GetString(fullNameBytes.ToArray());
                    }

                    var unicodeCommentExtraField = ExtraFields.GetData<UnicodeCommentExtraField>();
                    if (unicodeCommentExtraField != null && commentBytes.IsMatchCrc(unicodeCommentExtraField.Crc))
                    {
                        // Unicode Comment Extra Field が付加されていて、かつCRCが一致している場合
                        // Unicode Comment Extra Field 上の文字列をコメントとして採用する
                        Comment = unicodeCommentExtraField.Comment;
                    }
                    else
                    {
                        // Unicode Path Extra Field が付加されていないか、またはCRCが一致しない場合
                        // 特にエンコーディングが定められていないので、コンピュータ上の既定のエンコーディングを採用する。
                        Comment = Encoding.Default.GetString(commentBytes.ToArray());
                    }
                }
            }
        }

        public long Index { get; }
        public long Offset { get; }
        public ZipFileEntryGeneralPurposeBitFlag GeneralPurposeBitFlag { get; }
        public DateTime DosTime { get; }
        public IReadOnlyArray<byte> FullNameBytes { get; }
        public IReadOnlyArray<byte> CommentBytes { get; }
        public ExtraFieldStorage ExtraFields { get; }
        public DateTime? LastWriteTimeUtc { get; }
        public DateTime? LastAccessTimeUtc { get; }
        public DateTime? CreationTimeUtc { get; }
        public string FullName { get; }
        public string Comment { get; }

        public static ZipEntryLocaFilelHeader Parse(Stream zipFileBaseStream, ZipEntryCentralDirectoryHeader centralDirectoryHeader)
        {
            zipFileBaseStream.Seek(centralDirectoryHeader.LocalFileHeaderOffset, SeekOrigin.Begin);
            var minimumLengthOfHeader = 30;
            var minimumHeaderBytes = zipFileBaseStream.ReadBytes(minimumLengthOfHeader);
            var signature = minimumHeaderBytes.GetSequence(0, _localHeaderSignature.Length);
            if (!signature.SequenceEqual(_localHeaderSignature))
                throw new BadZipFormatException("Not found in local header in expected position");
            var generalPurposeBitFlag = (ZipFileEntryGeneralPurposeBitFlag)minimumHeaderBytes.ToUInt16(6);
            //var compressionMethod = minimumHeaderBytes.ToUInt16(8);
            //var dosTime = minimumHeaderBytes.ToUInt16(10);
            //var dosDate = minimumHeaderBytes.ToUInt16(12);
            //var packedSize = minimumHeaderBytes.ToUInt32(18);
            //var size = minimumHeaderBytes.ToUInt32(22);
            var fileNameLength = minimumHeaderBytes.ToUInt16(26);
            var extraFieldLength = minimumHeaderBytes.ToUInt16(28);
            var fullNameBytes = zipFileBaseStream.ReadBytes(fileNameLength);
            var extraData = zipFileBaseStream.ReadBytes(extraFieldLength);

            // ファイルサイズ/圧縮後ファイルサイズ/CRC を真面目に取得しようとすると、(ファイルサイズがわからないまま)ファイルを全部読み取り、
            // その後にある Data Descriptor を読まねばならないことがある。
            // 圧縮元のデータがファイルシステム上のファイルではなくストリームだとそういう圧縮がされることがあるらしい。
            // このプログラムの目的としては、ファイルサイズやCRCを独自に取得することは必須ではないので、それらの情報の取得は断念することにした。

            // ローカルディレクトリでは日時が設定されないことがあるので使用しない
            // var dosDateTime = new[] { dosDate, dosTime }.FromDosDateTimeToDateTime(DateTimeKind.Local).ToUniversalTime();
            return
                new ZipEntryLocaFilelHeader(
                    centralDirectoryHeader,
                    generalPurposeBitFlag,
                    fullNameBytes,
                    centralDirectoryHeader.CommentBytes,
                    extraData,
                    centralDirectoryHeader.ExtraFields);
        }
    }
}
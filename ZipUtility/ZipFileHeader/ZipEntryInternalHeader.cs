using System;
using System.Linq;
using System.Text;
using Utility;
using ZipUtility.ZipExtraField;

namespace ZipUtility.ZipFileHeader
{
    abstract class ZipEntryInternalHeader<ZIP64_EXTRA_FIELD_T>
        : IZip64ExtendedInformationExtraFieldValueSource
        where ZIP64_EXTRA_FIELD_T : Zip64ExtendedInformationExtraField
    {
        private static Encoding _utf8EncodingWithoutBOM;

        static ZipEntryInternalHeader()
        {
            _utf8EncodingWithoutBOM = new UTF8Encoding(false);
        }

        protected ZipEntryInternalHeader(ZipEntryGeneralPurposeBitFlag generalPurposeBitFlag, ZipEntryCompressionMethod? compressionMethod, DateTime? dosDateTime, IReadOnlyArray<byte> fullNameBytes, IReadOnlyArray<byte> commentBytes, ExtraFieldStorage extraFields)
        {
            GeneralPurposeBitFlag = generalPurposeBitFlag;
            CompressionMethod = compressionMethod;
            DosDateTime = dosDateTime;
            FullNameBytes = fullNameBytes;
            CommentBytes = commentBytes;
            ExtraFields = extraFields;
            LastWriteTimeUtc = null;
            LastAccessTimeUtc = null;
            CreationTimeUtc = null;
            FullName = null;
            Comment = null;
            OriginalFullName = null;
            Zip64ExtraField = null;

            var ntfsExtraField = ExtraFields.GetData<NtfsExtraField>();
            if (ntfsExtraField != null &&
                ntfsExtraField.LastWriteTimeUtc == null &&
                ntfsExtraField.LastAccessTimeUtc == null &&
                ntfsExtraField.CreationTimeUtc == null)
            {
                // NTFS extra field が存在するが 日時情報が全く含まれていない場合は、extra field を削除する。
                ExtraFields.Delete(NtfsExtraField.ExtraFieldId);
            }

            var extendedTimestampExtraField = ExtraFields.GetData<ExtendedTimestampExtraField>();
            if (extendedTimestampExtraField != null &&
                extendedTimestampExtraField.LastWriteTimeUtc == null &&
                extendedTimestampExtraField.LastAccessTimeUtc == null &&
                extendedTimestampExtraField.CreationTimeUtc == null)
            {
                // Extended Time Stamp extra field が存在するが 日時情報が全く含まれていない場合は、extra field を削除する。
                ExtraFields.Delete(ExtendedTimestampExtraField.ExtraFieldId);
            }

            // 日付に関する extra field を検索して日付を設定する (上から順に優先される)
            var timeStampExtraFields = new[]
            {
                ExtraFields.GetData<NtfsExtraField>() as ITimestampExtraField,
                ExtraFields.GetData<ExtendedTimestampExtraField>(),
                ExtraFields.GetData<UnixExtraFieldType1>(),
                ExtraFields.GetData<UnixExtraFieldType0>(),
            }
            .Where(timeStampExtraField => timeStampExtraField != null);
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
            if ((GeneralPurposeBitFlag & ZipEntryGeneralPurposeBitFlag.UseUnicodeEncodingForNameAndComment) != ZipEntryGeneralPurposeBitFlag.None)
            {
                // ローカルヘッダのフラグでパス名とコメントにUNICODEを使用するように求められている場合
                // エントリ名とコメントのエンコーディングにUTF8を採用する
                FullName = _utf8EncodingWithoutBOM.GetString(fullNameBytes.ToArray());
                OriginalFullName = FullName;
                if (commentBytes != null)
                    Comment = _utf8EncodingWithoutBOM.GetString(commentBytes.ToArray());
            }
            else
            {
                // ローカルヘッダのフラグでパス名とコメントにUNICODEを使用するように求められていない場合

                OriginalFullName = Encoding.Default.GetString(fullNameBytes.ToArray());

                var codePageExtraField = ExtraFields.GetData<CodePageExtraField>();
                if (codePageExtraField != null)
                {
                    // 0xe57a extra field (名称不明) にて、使用するコードページが指定されていた場合
                    // 指定されたコードページのエンコーディングを採用する
                    var encoding = Encoding.GetEncoding(codePageExtraField.CodePage);
                    FullName = encoding.GetString(fullNameBytes.ToArray());
                    if (commentBytes != null)
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

                    if (commentBytes != null)
                    {
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
        }

        public ZipEntryGeneralPurposeBitFlag GeneralPurposeBitFlag { get; }
        public ZipEntryCompressionMethod? CompressionMethod { get; }
        public DateTime? DosDateTime { get; }
        public IReadOnlyArray<byte> FullNameBytes { get; }
        public IReadOnlyArray<byte> CommentBytes { get; }
        public ExtraFieldStorage ExtraFields { get; }
        public DateTime? LastWriteTimeUtc { get; }
        public DateTime? LastAccessTimeUtc { get; }
        public DateTime? CreationTimeUtc { get; }
        public string FullName { get; }
        public string Comment { get; }
        public string OriginalFullName { get; }
        protected ZIP64_EXTRA_FIELD_T Zip64ExtraField { get; private set; }
        protected virtual UInt32? SizeInHeader { get => null; set => throw new NotSupportedException(); }
        protected virtual UInt32? PackedSizeInHeader { get => null; set => throw new NotSupportedException(); }
        protected virtual UInt32? RelativeHeaderOffsetInHeader { get => null; set => throw new NotSupportedException(); }
        protected virtual UInt16? DiskStartNumberInHeader { get => null; set => throw new NotSupportedException(); }

        protected void ApplyZip64ExtraField(ZIP64_EXTRA_FIELD_T zip64ExtraField)
        {
            Zip64ExtraField = zip64ExtraField;
            if (Zip64ExtraField != null)
                Zip64ExtraField.ZipHeaderSource = this;
        }

        UInt32? IZip64ExtendedInformationExtraFieldValueSource.Size { get => SizeInHeader; set => SizeInHeader = value; }
        UInt32? IZip64ExtendedInformationExtraFieldValueSource.PackedSize { get => PackedSizeInHeader; set => PackedSizeInHeader = value; }
        UInt32? IZip64ExtendedInformationExtraFieldValueSource.RelativeHeaderOffset { get => RelativeHeaderOffsetInHeader; set => RelativeHeaderOffsetInHeader = value; }
        UInt16? IZip64ExtendedInformationExtraFieldValueSource.DiskStartNumber { get => DiskStartNumberInHeader; set => DiskStartNumberInHeader = value; }
    }
}

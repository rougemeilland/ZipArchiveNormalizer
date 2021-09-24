using System;
using System.Linq;
using System.Text;
using Utility;
using ZipUtility.ZipExtraField;

namespace ZipUtility.ZipFileHeader
{
    abstract class ZipEntryInternalHeader<ZIP64_EXTRA_FIELD_T>
        : IZip64ExtendedInformationExtraFieldValueSource
        where ZIP64_EXTRA_FIELD_T : Zip64ExtendedInformationExtraField, new()
    {
        private static Encoding _utf8EncodingWithoutBOM;

        static ZipEntryInternalHeader()
        {
            _utf8EncodingWithoutBOM = new UTF8Encoding(false);
        }

        protected ZipEntryInternalHeader(ZipEntryGeneralPurposeBitFlag generalPurposeBitFlag, ZipEntryCompressionMethodId compressionMethod, DateTime? dosDateTime, IReadOnlyArray<byte> fullNameBytes, IReadOnlyArray<byte> commentBytes, ExtraFieldStorage extraFields)
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
                // ヘッダのフラグでパス名とコメントにUTF8を使用するように求められている場合
                // エントリ名とコメントのエンコーディングにUTF8を採用する
                FullName = _utf8EncodingWithoutBOM.GetString(fullNameBytes);
                OriginalFullName = FullName;
                Comment = _utf8EncodingWithoutBOM.GetString(commentBytes);
                FullNameCanBeExpressedInUnicode = true;
                CommentCanBeExpressedInUnicode = true;
            }
            else
            {
                // ヘッダのフラグでパス名とコメントにUTF8を使用するように求められていない場合
                OriginalFullName = Encoding.Default.GetString(fullNameBytes);

                var codePageExtraField = ExtraFields.GetData<CodePageExtraField>();
                if (codePageExtraField != null)
                {
                    // 0xe57a extra field (名称不明) にて、使用するコードページが指定されていた場合
                    // 指定されたコードページのエンコーディングを採用する
                    var encoding = Encoding.GetEncoding(codePageExtraField.CodePage);
                    FullName = encoding.GetString(fullNameBytes);
                    Comment = encoding.GetString(commentBytes);
                    var encodingForTest =
                        Encoding.GetEncoding(codePageExtraField.CodePage);
                    FullNameCanBeExpressedInUnicode =
                        encodingForTest.GetBytes(encodingForTest.GetString(fullNameBytes)).AsReadOnly()
                        .SequenceEqual(fullNameBytes);
                    CommentCanBeExpressedInUnicode =
                        encodingForTest.GetBytes(encodingForTest.GetString(commentBytes)).AsReadOnly()
                        .SequenceEqual(commentBytes);
                }
                else
                {
                    // extra field にて、使用するコードページが指定されていない場合
                    var xceedUnicodeExtraField = extraFields.GetData<XceedUnicodeExtraField>();
                    if (xceedUnicodeExtraField != null)
                    {
                        // Xceed unicode extra fieldが付加されている場合
                        FullName = xceedUnicodeExtraField.FullName;
                        Comment = xceedUnicodeExtraField.Comment;
                        FullNameCanBeExpressedInUnicode = true;
                        CommentCanBeExpressedInUnicode = true;
                    }
                    else
                    {
                        // Xceed unicode extra fieldが付加されていない場合

                        // 与えられたエントリ名とコメントのバイト列をUNICODEと一対一で対応させることができるEncodingを探す。
                        // 探す順序は以下の通り。
                        // 1) IBM437
                        // 2) コンピュータのデフォルトのエンコーディング
                        // 3) 非UNICODE系のその他のエンコーディング
                        // 4) UNICODE系のエンコーディング
                        // もしそのようなエンコーディングが一つもなければ、
                        // 「エントリ名またはコメント名にUNICODEでは扱えない文字が含まれている」
                        // ということになる。
                        var validEncoding =
                            Encoding.GetEncodings()
                                .Select(encoding => encoding.GetEncoding())
                                .OrderBy(encoding =>
                                {
                                    if (encoding.WebName == "IBM437")
                                        return -2;
                                    else if (encoding.WebName == Encoding.Default.WebName)
                                        return -1;
                                    else if (encoding.WebName.IsAnyOf("utf-16", "unicodeFFFE", "utf-32", "utf-32BE", "utf-7", "utf-8"))
                                        return encoding.CodePage + 0x10000;
                                    else
                                        return encoding.CodePage;
                                })
                                .Where(encoding =>
                                {
                                    try
                                    {
                                        return
                                            encoding.GetBytes(encoding.GetString(fullNameBytes)).AsReadOnly().SequenceEqual(fullNameBytes) &&
                                            encoding.GetBytes(encoding.GetString(commentBytes)).AsReadOnly().SequenceEqual(commentBytes);
                                    }
                                    catch (ArgumentException)
                                    {
                                        return false;
                                    }
                                })
                                .FirstOrDefault();
                        var unicodePathExtraField = ExtraFields.GetData<UnicodePathExtraField>();
                        if (unicodePathExtraField != null && fullNameBytes.IsMatchCrc(unicodePathExtraField.Crc))
                        {
                            // Unicode Path Extra Field が付加されていて、かつCRCが一致している場合
                            // Unicode Path Extra Field 上の文字列をエントリ名として採用する
                            FullName = unicodePathExtraField.FullName;
                            FullNameCanBeExpressedInUnicode = true;
                        }
                        else
                        {
                            // Unicode Path Extra Field が付加されていないか、またはCRCが一致しない場合
                            if (validEncoding != null)
                            {
                                // 特にエンコーディングが定められていないが、与えられたバイト列を解釈することができるエンコーディングが存在するので、
                                // そのエンコーディングで変換する。
                                FullName = validEncoding.GetString(fullNameBytes);
                                FullNameCanBeExpressedInUnicode = true;
                            }
                            else
                            {
                                // 特にエンコーディングが定められておらず、しかもUNICODEにも変換できない文字が含まれている場合
                                // .NETの文字列として扱おうとすると文字化けが発生するので、文字列として変換は行わない
                                FullName = "#" + fullNameBytes.ToFriendlyString();
                                FullNameCanBeExpressedInUnicode = false;
                            }
                        }

                        var unicodeCommentExtraField = ExtraFields.GetData<UnicodeCommentExtraField>();
                        if (unicodeCommentExtraField != null && commentBytes.IsMatchCrc(unicodeCommentExtraField.Crc))
                        {
                            // Unicode Comment Extra Field が付加されていて、かつCRCが一致している場合
                            // Unicode Comment Extra Field 上の文字列をコメントとして採用する
                            Comment = unicodeCommentExtraField.Comment;
                            CommentCanBeExpressedInUnicode = true;
                        }
                        else
                        {
                            // Unicode Path Extra Field が付加されていないか、またはCRCが一致しない場合
                            if (validEncoding != null)
                            {
                                // 特にエンコーディングが定められていないが、与えられたバイト列を解釈することができるエンコーディングが存在するので、
                                // そのエンコーディングで変換する。
                                Comment = validEncoding.GetString(commentBytes);
                                CommentCanBeExpressedInUnicode = true;
                            }
                            else
                            {
                                // 特にエンコーディングが定められておらず、しかもUNICODEにも変換できない文字が含まれている場合
                                // .NETの文字列として扱おうとすると文字化けが発生するので、文字列として変換は行わない
                                Comment = "#" + fullNameBytes.ToFriendlyString();
                                CommentCanBeExpressedInUnicode = false;
                            }
                        }
                    }
                }
            }
            FullNameCanBeExpressedInStandardEncoding = FullName.IsConvertableToMinimumCharacterSet();
            CommentCanBeExpressedInStandardEncoding = Comment.IsConvertableToMinimumCharacterSet();
        }

        public ZipEntryGeneralPurposeBitFlag GeneralPurposeBitFlag { get; }
        public ZipEntryCompressionMethodId CompressionMethod { get; }
        public DateTime? DosDateTime { get; }
        public IReadOnlyArray<byte> FullNameBytes { get; }
        public IReadOnlyArray<byte> CommentBytes { get; }
        public ExtraFieldStorage ExtraFields { get; }
        public DateTime? LastWriteTimeUtc { get; }
        public DateTime? LastAccessTimeUtc { get; }
        public DateTime? CreationTimeUtc { get; }

        /// <summary>
        /// エントリ名のエンコーディングの判断を
        /// </summary>
        public string OriginalFullName { get; }
        public string FullName { get; }
        public bool FullNameCanBeExpressedInUnicode { get; }
        public bool FullNameCanBeExpressedInStandardEncoding { get; }
        public string Comment { get; }
        public bool CommentCanBeExpressedInUnicode { get; }
        public bool CommentCanBeExpressedInStandardEncoding { get; }
        protected ZIP64_EXTRA_FIELD_T Zip64ExtraField { get; private set; }
        protected virtual UInt32 SizeInHeader { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        protected virtual UInt32 PackedSizeInHeader { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        protected virtual UInt32 RelativeHeaderOffsetInHeader { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        protected virtual UInt16 DiskStartNumberInHeader { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        protected void ApplyZip64ExtraField(ZIP64_EXTRA_FIELD_T zip64ExtraField)
        {
            Zip64ExtraField = zip64ExtraField;
            if (Zip64ExtraField == null)
                Zip64ExtraField = new ZIP64_EXTRA_FIELD_T();
            Zip64ExtraField.ZipHeaderSource = this;
        }

        UInt32 IZip64ExtendedInformationExtraFieldValueSource.Size { get => SizeInHeader; set => SizeInHeader = value; }
        UInt32 IZip64ExtendedInformationExtraFieldValueSource.PackedSize { get => PackedSizeInHeader; set => PackedSizeInHeader = value; }
        UInt32 IZip64ExtendedInformationExtraFieldValueSource.RelativeHeaderOffset { get => RelativeHeaderOffsetInHeader; set => RelativeHeaderOffsetInHeader = value; }
        UInt16 IZip64ExtendedInformationExtraFieldValueSource.DiskStartNumber { get => DiskStartNumberInHeader; set => DiskStartNumberInHeader = value; }
    }
}

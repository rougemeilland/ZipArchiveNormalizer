using System;
using System.Collections.Generic;
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
        private static readonly Encoding _utf8EncodingWithoutBOM;
        private static IEnumerable<Encoding> _allEncodings;

        static ZipEntryInternalHeader()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _utf8EncodingWithoutBOM = new UTF8Encoding(false);

            // サポートされているエンコーディングのリストを作成する。
            // 順序は以下の通り。
            // 1) IBM437
            // 2) コンピュータのデフォルトのエンコーディング
            // 3) 非UNICODE系のその他のエンコーディング
            // 4) UNICODE系のエンコーディング
            _allEncodings =
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
                .ToArray();
        }

        protected ZipEntryInternalHeader(ZipEntryGeneralPurposeBitFlag generalPurposeBitFlag, ZipEntryCompressionMethodId compressionMethod, DateTime? dosDateTime, ReadOnlyMemory<byte> fullNameBytes, ReadOnlyMemory<byte> commentBytes, ExtraFieldStorage extraFields, ZIP64_EXTRA_FIELD_T? zip64ExtraField)
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
            FullName = "";
            Comment = "";
            OriginalFullName = "";
            Zip64ExtraField = zip64ExtraField is not null ? zip64ExtraField : new ZIP64_EXTRA_FIELD_T();
            Zip64ExtraField.SetZipHeaderSource(this);

            var ntfsExtraField = ExtraFields.GetData<NtfsExtraField>();
            if (ntfsExtraField is not null &&
                ntfsExtraField.LastWriteTimeUtc is null &&
                ntfsExtraField.LastAccessTimeUtc is null &&
                ntfsExtraField.CreationTimeUtc is null)
            {
                // NTFS extra field が存在するが 日時情報が全く含まれていない場合は、extra field を削除する。
                ExtraFields.Delete(NtfsExtraField.ExtraFieldId);
            }

            var extendedTimestampExtraField = ExtraFields.GetData<ExtendedTimestampExtraField>();
            if (extendedTimestampExtraField is not null &&
                extendedTimestampExtraField.LastWriteTimeUtc is null &&
                extendedTimestampExtraField.LastAccessTimeUtc is null &&
                extendedTimestampExtraField.CreationTimeUtc is null)
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
            .Where(timeStampExtraField => timeStampExtraField is not null);
            foreach (var timeStampExtraField in timeStampExtraFields.WhereNotNull())
            {
                if (LastWriteTimeUtc is null && timeStampExtraField.LastWriteTimeUtc is not null)
                    LastWriteTimeUtc = timeStampExtraField.LastWriteTimeUtc;
                if (LastAccessTimeUtc is null && timeStampExtraField.LastAccessTimeUtc is not null)
                    LastAccessTimeUtc = timeStampExtraField.LastAccessTimeUtc;
                if (CreationTimeUtc is null && timeStampExtraField.CreationTimeUtc is not null)
                    CreationTimeUtc = timeStampExtraField.CreationTimeUtc;
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
                if (codePageExtraField is not null)
                {
                    // 0xe57a extra field (名称不明) にて、使用するコードページが指定されていた場合
                    // 指定されたコードページのエンコーディングを採用する
                    var encoding = Encoding.GetEncoding(codePageExtraField.CodePage);
                    FullName = encoding.GetString(fullNameBytes);
                    Comment = encoding.GetString(commentBytes);
                    var encodingForTest =
                        Encoding.GetEncoding(codePageExtraField.CodePage);
                    FullNameCanBeExpressedInUnicode =
                        encodingForTest.GetBytes(encodingForTest.GetString(fullNameBytes)).AsReadOnlySpan()
                        .SequenceEqual(fullNameBytes.Span);
                    CommentCanBeExpressedInUnicode =
                        encodingForTest.GetBytes(encodingForTest.GetString(commentBytes)).AsReadOnlySpan()
                        .SequenceEqual(commentBytes.Span);
                }
                else
                {
                    // extra field にて、使用するコードページが指定されていない場合
                    var xceedUnicodeExtraField = extraFields.GetData<XceedUnicodeExtraField>();
                    if (xceedUnicodeExtraField is not null &&
                        xceedUnicodeExtraField.FullName is not null &&
                        xceedUnicodeExtraField.Comment is not null)
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
                        // もしそのようなエンコーディングが一つもなければ、
                        // 「エントリ名またはコメント名にUNICODEでは扱えない文字が含まれている」
                        // ということになる。
                        var validEncoding =
                            _allEncodings
                                .Where(encoding =>
                                {
                                    try
                                    {
                                        return
                                            encoding.GetBytes(encoding.GetString(fullNameBytes)).AsReadOnlySpan().SequenceEqual(fullNameBytes.Span) &&
                                            encoding.GetBytes(encoding.GetString(commentBytes)).AsReadOnlySpan().SequenceEqual(commentBytes.Span);
                                    }
                                    catch (ArgumentException)
                                    {
                                        return false;
                                    }
                                })
                                .FirstOrDefault();
                        var unicodePathExtraField = ExtraFields.GetData<UnicodePathExtraField>();
                        if (unicodePathExtraField is not null && fullNameBytes.IsMatchCrc32(unicodePathExtraField.Crc))
                        {
                            // Unicode Path Extra Field が付加されていて、かつCRCが一致している場合
                            // Unicode Path Extra Field 上の文字列をエントリ名として採用する
                            FullName = unicodePathExtraField.FullName;
                            FullNameCanBeExpressedInUnicode = true;
                        }
                        else
                        {
                            // Unicode Path Extra Field が付加されていないか、またはCRCが一致しない場合
                            if (validEncoding is not null)
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
                        if (unicodeCommentExtraField is not null && commentBytes.IsMatchCrc32(unicodeCommentExtraField.Crc))
                        {
                            // Unicode Comment Extra Field が付加されていて、かつCRCが一致している場合
                            // Unicode Comment Extra Field 上の文字列をコメントとして採用する
                            Comment = unicodeCommentExtraField.Comment;
                            CommentCanBeExpressedInUnicode = true;
                        }
                        else
                        {
                            // Unicode Path Extra Field が付加されていないか、またはCRCが一致しない場合
                            if (validEncoding is not null)
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
        public ReadOnlyMemory<byte> FullNameBytes { get; }
        public ReadOnlyMemory<byte> CommentBytes { get; }
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

        UInt32 IZip64ExtendedInformationExtraFieldValueSource.Size { get => SizeInHeader; set => SizeInHeader = value; }
        UInt32 IZip64ExtendedInformationExtraFieldValueSource.PackedSize { get => PackedSizeInHeader; set => PackedSizeInHeader = value; }
        UInt32 IZip64ExtendedInformationExtraFieldValueSource.RelativeHeaderOffset { get => RelativeHeaderOffsetInHeader; set => RelativeHeaderOffsetInHeader = value; }
        UInt16 IZip64ExtendedInformationExtraFieldValueSource.DiskStartNumber { get => DiskStartNumberInHeader; set => DiskStartNumberInHeader = value; }
    }
}

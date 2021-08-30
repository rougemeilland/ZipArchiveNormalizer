namespace ZipUtility.ZipExtraField
{
    /// <summary>
    /// https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT の
    /// セクション "4.6.8 -Info-ZIP Unicode Comment Extra Field (0x6375)" を参照
    /// </summary>
    public class UnicodeCommentExtraField
        : UnicodeStringExtraField
    {
        public UnicodeCommentExtraField()
            : base(ExtraFieldId)
        {
        }

        public const ushort ExtraFieldId = 0x6375;
        public string Comment { get => UnicodeString; set => UnicodeString = value; }
        protected override byte SupportedVersion => 1;
    }
}
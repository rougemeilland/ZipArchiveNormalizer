namespace ZipUtility.ZipExtraField
{
    /// <summary>
    /// https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT の
    /// セクション "4.6.9 -Info-ZIP Unicode Path Extra Field (0x7075)" を参照
    /// </summary>
    public class UnicodePathExtraField
        : UnicodeStringExtraField
    {
        public UnicodePathExtraField()
            : base(ExtraFieldId)
        {
        }

        public const ushort ExtraFieldId = 0x7075;
        public string FullName { get => UnicodeString; set => UnicodeString = value; }
        protected override byte SupportedVersion => 1;
    }
}

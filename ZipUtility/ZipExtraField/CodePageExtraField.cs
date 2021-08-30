using System;
using System.Linq;
using ZipUtility.Helper;

namespace ZipUtility.ZipExtraField
{
    // see https://gnqg.hatenablog.com/entry/2016/09/11/155033
    public class CodePageExtraField
        : ExtraField
    {
        public CodePageExtraField()
            : base(ExtraFieldId)
        {
            CodePage = -1;
        }


        public const UInt16 ExtraFieldId = 0xe57a;

        public override byte[] GetData(ZipEntryHeaderType headerType)
        {
            var writer = new ByteArrayOutputStream();
            writer.WriteInt32LE(CodePage);
            return writer.ToByteSequence().ToArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, byte[] data, int index, int count)
        {
            var reader = new ByteArrayInputStream(data, index, count);
            CodePage = reader.ReadInt32LE();
        }

        public Int32 CodePage { get; set; }
    }
}
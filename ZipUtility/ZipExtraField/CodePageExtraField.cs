using System;
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
            if (CodePage < 0)
                return null;
            var writer = new ByteArrayOutputStream();
            writer.WriteInt32LE(CodePage);
            return writer.ToByteSequence().ToArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, byte[] data, int index, int count)
        {
            CodePage = -1;
            var reader = new ByteArrayInputStream(data, index, count);
            var succes = false;
            try
            {
                CodePage = reader.ReadInt32LE();
                if (reader.ReadToEnd().Length > 0)
                    throw GetBadFormatException(headerType, data, index, count);
                succes = true;

            }
            catch (UnexpectedEndOfStreamException)
            {
                throw GetBadFormatException(headerType, data, index, count);
            }
            finally
            {
                if (!succes)
                {
                    CodePage = -1;
                }
            }
        }

        public Int32 CodePage { get; set; }
    }
}
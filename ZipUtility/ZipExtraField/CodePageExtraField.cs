using System;
using Utility.IO;

namespace ZipUtility.ZipExtraField
{
    // see https://gnqg.hatenablog.com/entry/2016/09/11/155033
    public class CodePageExtraField
        : ExtraField
    {
        private Int32? _codePage;

        public CodePageExtraField()
            : base(ExtraFieldId)
        {
            _codePage = null;
        }


        public const UInt16 ExtraFieldId = 0xe57a;

        public override ReadOnlyMemory<byte>? GetData(ZipEntryHeaderType headerType)
        {
            if (_codePage is null)
                return null;
            var writer = new ByteArrayRenderer();
            writer.WriteInt32LE(CodePage);
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data)
        {
            _codePage = null;
            var reader = new ByteArrayParser(data);
            var succes = false;
            try
            {
                CodePage = reader.ReadInt32LE();
                if (reader.ReadAllBytes().Length > 0)
                    throw GetBadFormatException(headerType, data);
                succes = true;

            }
            catch (UnexpectedEndOfStreamException)
            {
                throw GetBadFormatException(headerType, data);
            }
            finally
            {
                if (!succes)
                {
                    _codePage = null;
                }
            }
        }

        public Int32 CodePage
        {
            get => _codePage ?? throw new InvalidOperationException();
            set => _codePage = value;
        }
    }
}

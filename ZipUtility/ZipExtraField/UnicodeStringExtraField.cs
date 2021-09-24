using System;
using System.Text;
using Utility;
using ZipUtility.Helper;

namespace ZipUtility.ZipExtraField
{
    public abstract class UnicodeStringExtraField
        : ExtraField
    {
        private static Encoding _utf8Encoding;

        static UnicodeStringExtraField()
        {
            // BOMなしのUTF8エンコーディング
            _utf8Encoding = new UTF8Encoding(false);
        }

        protected UnicodeStringExtraField(UInt16 extraFieldId)
            : base(extraFieldId)
        {
            Crc = 0;
            UnicodeString = null;
        }

        public override IReadOnlyArray<byte> GetData(ZipEntryHeaderType headerType)
        {
            if (UnicodeString == null)
                return null;
            var writer = new ByteArrayOutputStream();
            writer.WriteByte(SupportedVersion);
            writer.WriteUInt32LE(Crc);
            writer.WriteBytes(_utf8Encoding.GetBytes(UnicodeString));
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, IReadOnlyArray<byte> data, int index, int count)
        {
            UnicodeString = null;
            Crc = 0;
            var reader = new ByteArrayInputStream(data, index, count);
            var success = false;
            try
            {
                var version = reader.ReadByte();
                if (version != SupportedVersion)
                    return;
                Crc = reader.ReadUInt32LE();
                UnicodeString = _utf8Encoding.GetString(reader.ReadToEnd());
                if (string.IsNullOrEmpty(UnicodeString))
                    return;
                if (reader.ReadToEnd().Length > 0)
                    throw GetBadFormatException(headerType, data, index, count);
                success = true;
            }
            catch (UnexpectedEndOfStreamException)
            {
                throw GetBadFormatException(headerType, data, index, count);
            }
            finally
            {
                if (!success)
                {
                    UnicodeString = null;
                    Crc = 0;
                }
            }
        }

        public UInt32 Crc { get; set; }
        public int CodePage => _utf8Encoding.CodePage;
        protected abstract byte SupportedVersion { get; }
        protected string UnicodeString { get; set; }
    }
}
using System;
using System.Text;
using Utility.IO;

namespace ZipUtility.ZipExtraField
{
    public abstract class UnicodeStringExtraField
        : ExtraField
    {
        private static readonly Encoding _utf8Encoding;

        private UInt32? _crc;
        private string? _unicodeString;

        static UnicodeStringExtraField()
        {
            // BOMなしのUTF8エンコーディング
            _utf8Encoding = new UTF8Encoding(false);
        }

        protected UnicodeStringExtraField(UInt16 extraFieldId)
            : base(extraFieldId)
        {
            _crc = null;
            _unicodeString = null;
        }

        public override ReadOnlyMemory<byte>? GetData(ZipEntryHeaderType headerType)
        {
            if (_crc is null || _unicodeString is null)
                return null;
            var writer = new ByteArrayRenderer();
            writer.WriteByte(SupportedVersion);
            writer.WriteUInt32LE(Crc);
            writer.WriteBytes(_utf8Encoding.GetBytes(UnicodeString));
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data)
        {
            _unicodeString = null;
            _crc = null;
            var reader = new ByteArrayParser(data);
            var success = false;
            try
            {
                var version = reader.ReadByte();
                if (version != SupportedVersion)
                    return;
                Crc = reader.ReadUInt32LE();
                UnicodeString = _utf8Encoding.GetString(reader.ReadAllBytes());
                if (string.IsNullOrEmpty(UnicodeString))
                    return;
                if (reader.ReadAllBytes().Length > 0)
                    throw GetBadFormatException(headerType, data);
                success = true;
            }
            catch (UnexpectedEndOfStreamException)
            {
                throw GetBadFormatException(headerType, data);
            }
            finally
            {
                if (!success)
                {
                    _unicodeString = null;
                    _crc = null;
                }
            }
        }

        public UInt32 Crc
        {
            get => _crc ?? throw new InvalidOperationException();
            set => _crc = value;
        }

        protected abstract byte SupportedVersion { get; }

        protected string UnicodeString
        {
            get => _unicodeString ?? throw new InvalidOperationException();
            set => _unicodeString = value;
        }
    }
}

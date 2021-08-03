using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Text;
using ZipCabinetNormalizer.Helper;

namespace ZipCabinetNormalizer.ZipExtraField
{
    class UnicodeCommentExtraField
        : ITaggedData
    {
        private const byte _supportedVersion = 1;
        private static Encoding _utf8Encoding;

        static UnicodeCommentExtraField()
        {
            _utf8Encoding = new UTF8Encoding(false);
        }

        public UnicodeCommentExtraField()
        {
            CRC32 = 0;
            Comment = null;
        }

        public short TagID => 0x6375;

        public byte[] GetData()
        {
            if (Comment == null)
                return null;
            var writer = new ByteArrayOutputStream();
            writer.WriteByte(_supportedVersion);
            writer.WriteUInt32LE(CRC32);
            writer.WriteBytes(_utf8Encoding.GetBytes(Comment));
            return writer.ToByteArray();
        }

        public void SetData(byte[] data, int index, int count)
        {
            var success = false;
            try
            {
                var reader = new ByteArrayInputStream(data, index, count);
                var version = reader.ReadByte();
                if (version != _supportedVersion)
                    return;
                CRC32 = reader.ReadUInt32LE();
                Comment = _utf8Encoding.GetString(reader.ReadToEnd());
                if (string.IsNullOrEmpty(Comment))
                    return;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    Comment = null;
                    CRC32 = 0;
                }
            }
        }

        public UInt32 CRC32 { get; set; }

        public string Comment { get; set; }
    }
}

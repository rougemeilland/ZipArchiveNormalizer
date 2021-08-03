using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Linq;
using System.Text;
using ZipCabinetNormalizer.Helper;

namespace ZipCabinetNormalizer.ZipExtraField
{
    class UnicodePathExtraField
        : ITaggedData
    {
        private const byte _supportedVersion = 1;
        private static Encoding _utf8Encoding;

        static UnicodePathExtraField()
        {
            _utf8Encoding = new UTF8Encoding(false);
        }

        public UnicodePathExtraField()
        {
            CRC32 = 0;
            FullName = null;
        }

        public short TagID => 0x7075;

        public byte[] GetData()
        {
            if (FullName == null)
                return null;
            var writer = new ByteArrayOutputStream();
            writer.WriteByte(_supportedVersion);
            writer.WriteUInt32LE(CRC32);
            writer.WriteBytes(_utf8Encoding.GetBytes(FullName));
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
                FullName = _utf8Encoding.GetString(reader.ReadToEnd());
                if (string.IsNullOrEmpty(FullName))
                    return;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    FullName = null;
                    CRC32 = 0;
                }
            }
        }

        public UInt32 CRC32 { get; set; }

        public string FullName { get; set; }
    }
}

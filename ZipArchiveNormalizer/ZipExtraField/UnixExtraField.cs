using ICSharpCode.SharpZipLib.Zip;
using System;
using ZipArchiveNormalizer.Helper;

namespace ZipArchiveNormalizer.ZipExtraField
{
    class UnixExtraField
        : ITaggedData
    {
        private static DateTime _baseTime;


        static UnixExtraField()
        {
            _baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public UnixExtraField()
        {
            LastAccessedTime = DateTime.UtcNow;
            LastModificationTime = DateTime.UtcNow;
            UserId = 0;
            GroupId = 0;
            AdditionalData = new byte[0];
        }

        public short TagID => 13;

        public byte[] GetData()
        {
            var writer = new ByteArrayOutputStream();
            writer.WriteUInt32LE((UInt32)(LastAccessedTime.ToUniversalTime() - _baseTime).TotalSeconds);
            writer.WriteUInt32LE((UInt32)(LastModificationTime.ToUniversalTime() - _baseTime).TotalSeconds);
            writer.WriteUInt16LE(UserId);
            writer.WriteUInt16LE(GroupId);
            writer.WriteBytes(AdditionalData);
            return writer.ToByteArray();
        }

        public void SetData(byte[] data, int index, int count)
        {
            var reader = new ByteArrayInputStream(data, index, count);
            LastAccessedTime = _baseTime + TimeSpan.FromSeconds(reader.ReadUInt32LE());
            LastModificationTime = _baseTime + TimeSpan.FromSeconds(reader.ReadUInt32LE());
            UserId = reader.ReadUInt16LE();
            GroupId = reader.ReadUInt16LE();
            AdditionalData = reader.ReadToEnd();
        }

        public DateTime LastAccessedTime { get; private set; }
        public DateTime LastModificationTime { get; private set; }
        public UInt16 UserId { get; private set; }
        public UInt16 GroupId { get; private set; }
        public byte[] AdditionalData { get; private set; }
    }
}

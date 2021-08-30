using System;
using System.Linq;
using ZipUtility.Helper;

namespace ZipUtility.ZipExtraField
{
    public class UnixExtraFieldType0
        : UnixTimestampExtraField
    {
        public UnixExtraFieldType0()
            : base(ExtraFieldId)
        {
            UserId = 0;
            GroupId = 0;
            AdditionalData = new byte[0];
        }

        public const ushort ExtraFieldId = 13;

        public override byte[] GetData(ZipEntryHeaderType headerType)
        {
            if (LastAccessTimeUtc == null || LastWriteTimeUtc == null)
                return null;
            var writer = new ByteArrayOutputStream();
            writer.WriteInt32LE(ToUnixTimeStamp(LastAccessTimeUtc.Value));
            writer.WriteInt32LE(ToUnixTimeStamp(LastWriteTimeUtc.Value));
            writer.WriteUInt16LE(UserId);
            writer.WriteUInt16LE(GroupId);
            writer.WriteBytes(AdditionalData);
            return writer.ToByteSequence().ToArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, byte[] data, int index, int count)
        {
            var reader = new ByteArrayInputStream(data, index, count);
            LastAccessTimeUtc = FromUnixTimeStamp(reader.ReadInt32LE());
            LastWriteTimeUtc = FromUnixTimeStamp(reader.ReadInt32LE());
            UserId = reader.ReadUInt16LE();
            GroupId = reader.ReadUInt16LE();
            AdditionalData = reader.ReadToEnd();
        }

        public override DateTime? CreationTimeUtc { get => null; set => throw new NotSupportedException(); }

        public UInt16 UserId { get; set; }
        public UInt16 GroupId { get; set; }
        public byte[] AdditionalData { get; set; }
    }
}
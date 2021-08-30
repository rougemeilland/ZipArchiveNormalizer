using System;
using System.Linq;
using ZipUtility.Helper;

namespace ZipUtility.ZipExtraField
{
    public class UnixExtraFieldType1
        : UnixTimestampExtraField
    {
        public UnixExtraFieldType1()
            : base(ExtraFieldId)
        {
        }

        public const ushort ExtraFieldId = 0x5855;

        public override byte[] GetData(ZipEntryHeaderType headerType)
        {
            if (LastAccessTimeUtc == null || LastWriteTimeUtc == null)
                return null;

            var writer = new ByteArrayOutputStream();
            writer.WriteInt32LE(ToUnixTimeStamp(LastAccessTimeUtc.Value));
            writer.WriteInt32LE(ToUnixTimeStamp(LastWriteTimeUtc.Value));
            if (headerType == ZipEntryHeaderType.LocalFileHeader)
            {
                writer.WriteUInt16LE(UserId);
                writer.WriteUInt16LE(GroupId);
            }
            return writer.ToByteSequence().ToArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, byte[] data, int index, int count)
        {
            var reader = new ByteArrayInputStream(data, index, count);
            LastAccessTimeUtc = FromUnixTimeStamp(reader.ReadInt32LE());
            LastWriteTimeUtc = FromUnixTimeStamp(reader.ReadInt32LE());
            if (headerType == ZipEntryHeaderType.LocalFileHeader)
            {
                UserId = reader.ReadUInt16LE();
                GroupId = reader.ReadUInt16LE();
            }
        }

        public override DateTime? CreationTimeUtc { get => null; set => throw new NotSupportedException(); }
        public UInt16 UserId { get; set; }
        public UInt16 GroupId { get; set; }
    }
}
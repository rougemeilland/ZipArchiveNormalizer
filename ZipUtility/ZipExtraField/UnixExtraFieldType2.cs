using System;
using System.Linq;
using ZipUtility.Helper;

namespace ZipUtility.ZipExtraField
{
    public abstract class UnixExtraFieldType2
        : ExtraField
    {
        public UnixExtraFieldType2()
            : base(ExtraFieldId)
        {
            UserId = 0;
            GroupId = 0;
        }

        public const ushort ExtraFieldId = 0x7855;

        public override byte[] GetData(ZipEntryHeaderType headerType)
        {
            var writer = new ByteArrayOutputStream();
            if (headerType == ZipEntryHeaderType.LocalFileHeader)
            {
                writer.WriteUInt16LE(UserId);
                writer.WriteUInt16LE(GroupId);
            }
            return writer.ToByteSequence().ToArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, byte[] data, int index, int count)
        {
            UserId = 0;
            GroupId = 0;
            var reader = new ByteArrayInputStream(data, index, count);
            if (headerType == ZipEntryHeaderType.LocalFileHeader)
            {
                UserId = reader.ReadUInt16LE();
                GroupId = reader.ReadUInt16LE();
            }
        }

        public UInt16 UserId { get; set; }
        public UInt16 GroupId { get; set; }
    }
}
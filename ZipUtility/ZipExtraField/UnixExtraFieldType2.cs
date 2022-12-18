using System;
using Utility.IO;

namespace ZipUtility.ZipExtraField
{
    public abstract class UnixExtraFieldType2
        : ExtraField
    {
        public UnixExtraFieldType2()
            : base(ExtraFieldId)
        {
            UserId = UInt16.MaxValue;
            GroupId = UInt16.MaxValue;
        }

        public const ushort ExtraFieldId = 0x7855;

        public override ReadOnlyMemory<byte>? GetData(ZipEntryHeaderType headerType)
        {
            var writer = new ByteArrayRenderer();
            if (headerType == ZipEntryHeaderType.LocalFileHeader)
            {
                writer.WriteUInt16LE(UserId);
                writer.WriteUInt16LE(GroupId);
            }
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data)
        {
            UserId = UInt16.MaxValue;
            GroupId = UInt16.MaxValue;
            var reader = new ByteArrayParser(data);
            var success = false;
            try
            {
                if (headerType == ZipEntryHeaderType.LocalFileHeader)
                {
                    UserId = reader.ReadUInt16LE();
                    GroupId = reader.ReadUInt16LE();
                }
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
                    UserId = UInt16.MaxValue;
                    GroupId = UInt16.MaxValue;
                }
            }
        }

        public UInt16 UserId { get; set; }
        public UInt16 GroupId { get; set; }
    }
}

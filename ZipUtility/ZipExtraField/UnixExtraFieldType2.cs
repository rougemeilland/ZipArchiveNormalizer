using System;
using Utility;
using Utility.IO;
using ZipUtility.Helper;

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

        public override IReadOnlyArray<byte> GetData(ZipEntryHeaderType headerType)
        {
            var writer = new ByteArrayOutputStream();
            if (headerType == ZipEntryHeaderType.LocalFileHeader)
            {
                writer.WriteUInt16LE(UserId);
                writer.WriteUInt16LE(GroupId);
            }
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, IReadOnlyArray<byte> data, int index, int count)
        {
            UserId = UInt16.MaxValue;
            GroupId = UInt16.MaxValue;
            var reader = new ByteArrayInputStream(data, index, count);
            var success = false;
            try
            {
                if (headerType == ZipEntryHeaderType.LocalFileHeader)
                {
                    UserId = reader.ReadUInt16LE();
                    GroupId = reader.ReadUInt16LE();
                }
                if (reader.ReadAllBytes().Length > 0)
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
                    UserId = UInt16.MaxValue;
                    GroupId = UInt16.MaxValue;
                }
            }
        }

        public UInt16 UserId { get; set; }
        public UInt16 GroupId { get; set; }
    }
}
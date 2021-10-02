using System;
using Utility;
using Utility.IO;
using ZipUtility.Helper;

namespace ZipUtility.ZipExtraField
{
    public class UnixExtraFieldType1
        : UnixTimestampExtraField
    {
        public UnixExtraFieldType1()
            : base(ExtraFieldId)
        {
            LastAccessTimeUtc = null;
            LastWriteTimeUtc = null;
            UserId = UInt16.MaxValue;
            GroupId = UInt16.MaxValue;
        }

        public const ushort ExtraFieldId = 0x5855;

        public override IReadOnlyArray<byte> GetData(ZipEntryHeaderType headerType)
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
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, IReadOnlyArray<byte> data, int index, int count)
        {
            LastAccessTimeUtc = null;
            LastWriteTimeUtc = null;
            UserId = UInt16.MaxValue;
            GroupId = UInt16.MaxValue;
            var reader = new ByteArrayInputStream(data, index, count);
            var success = false;
            try
            {
                LastAccessTimeUtc = FromUnixTimeStamp(reader.ReadInt32LE());
                LastWriteTimeUtc = FromUnixTimeStamp(reader.ReadInt32LE());
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
                    LastAccessTimeUtc = null;
                    LastWriteTimeUtc = null;
                    UserId = UInt16.MaxValue;
                    GroupId = UInt16.MaxValue;
                }
            }
        }

        public override DateTime? CreationTimeUtc { get => null; set => throw new NotSupportedException(); }
        public UInt16 UserId { get; set; }
        public UInt16 GroupId { get; set; }
    }
}
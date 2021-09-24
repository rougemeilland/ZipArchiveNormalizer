using System;
using Utility;
using ZipUtility.Helper;

namespace ZipUtility.ZipExtraField
{
    public class UnixExtraFieldType0
        : UnixTimestampExtraField
    {
        public UnixExtraFieldType0()
            : base(ExtraFieldId)
        {
            UserId = UInt16.MaxValue;
            GroupId = UInt16.MaxValue;
            AdditionalData = new byte[0];
        }

        public const ushort ExtraFieldId = 13;

        public override IReadOnlyArray<byte> GetData(ZipEntryHeaderType headerType)
        {
            if (LastAccessTimeUtc == null || LastWriteTimeUtc == null)
                return null;
            var writer = new ByteArrayOutputStream();
            writer.WriteInt32LE(ToUnixTimeStamp(LastAccessTimeUtc.Value));
            writer.WriteInt32LE(ToUnixTimeStamp(LastWriteTimeUtc.Value));
            writer.WriteUInt16LE(UserId);
            writer.WriteUInt16LE(GroupId);
            writer.WriteBytes(AdditionalData);
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, IReadOnlyArray<byte> data, int index, int count)
        {
            LastAccessTimeUtc = null;
            LastWriteTimeUtc = null;
            UserId = UInt16.MaxValue;
            GroupId = UInt16.MaxValue;
            AdditionalData = null;
            var reader = new ByteArrayInputStream(data, index, count);
            var success = false;
            try
            {
                LastAccessTimeUtc = FromUnixTimeStamp(reader.ReadInt32LE());
                LastWriteTimeUtc = FromUnixTimeStamp(reader.ReadInt32LE());
                UserId = reader.ReadUInt16LE();
                GroupId = reader.ReadUInt16LE();
                AdditionalData = reader.ReadToEnd();
                if (reader.ReadToEnd().Length > 0)
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
                    AdditionalData = null;
                }
            }
        }

        public override DateTime? CreationTimeUtc { get => null; set => throw new NotSupportedException(); }

        public UInt16 UserId { get; set; }
        public UInt16 GroupId { get; set; }
        public byte[] AdditionalData { get; set; }
    }
}
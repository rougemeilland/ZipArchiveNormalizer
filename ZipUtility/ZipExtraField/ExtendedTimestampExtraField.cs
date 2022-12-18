using System;
using Utility.IO;

namespace ZipUtility.ZipExtraField
{
    public class ExtendedTimestampExtraField
        : UnixTimestampExtraField
    {
        public ExtendedTimestampExtraField()
            : base(ExtraFieldId)
        {
        }

        public const ushort ExtraFieldId = 0x5455;

        public override ReadOnlyMemory<byte>? GetData(ZipEntryHeaderType headerType)
        {
            var flag = 0x00;
            var writer = new ByteArrayRenderer();
            if (LastWriteTimeUtc is not null)
                flag |= 0x01;
            if (LastAccessTimeUtc is not null)
                flag |= 0x02;
            if (CreationTimeUtc is not null)
                flag |= 0x04;
            if (flag == 0)
                return null;
            writer.WriteByte((byte)flag);
            if (LastWriteTimeUtc is not null)
                writer.WriteInt32LE(ToUnixTimeStamp(LastWriteTimeUtc.Value));
            if (headerType == ZipEntryHeaderType.LocalFileHeader)
            {
                if (LastAccessTimeUtc is not null)
                    writer.WriteInt32LE(ToUnixTimeStamp(LastAccessTimeUtc.Value));
                if (CreationTimeUtc is not null)
                    writer.WriteInt32LE(ToUnixTimeStamp(CreationTimeUtc.Value));
            }
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data)
        {
            LastWriteTimeUtc = null;
            LastAccessTimeUtc = null;
            CreationTimeUtc = null;
            var reader = new ByteArrayParser(data);
            var success = false;
            try
            {
                var flag = reader.ReadByte();
                if ((flag & 0x01) != 0)
                    LastWriteTimeUtc = FromUnixTimeStamp(reader.ReadInt32LE());
                if (headerType == ZipEntryHeaderType.LocalFileHeader)
                {
                    if ((flag & 0x02) != 0)
                        LastAccessTimeUtc = FromUnixTimeStamp(reader.ReadInt32LE());
                    if ((flag & 0x04) != 0)
                        CreationTimeUtc = FromUnixTimeStamp(reader.ReadInt32LE());
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
                    LastWriteTimeUtc = null;
                    LastAccessTimeUtc = null;
                    CreationTimeUtc = null;
                }
            }
        }
    }
}

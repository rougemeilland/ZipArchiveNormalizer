using Utility;
using Utility.IO;
using ZipUtility.Helper;

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

        public override IReadOnlyArray<byte> GetData(ZipEntryHeaderType headerType)
        {
            var flag = 0x00;
            var writer = new ByteArrayOutputStream();
            if (LastWriteTimeUtc.HasValue)
                flag |= 0x01;
            if (LastAccessTimeUtc.HasValue)
                flag |= 0x02;
            if (CreationTimeUtc.HasValue)
                flag |= 0x04;
            if (flag == 0)
                return null;
            writer.WriteByte((byte)flag);
            if (LastWriteTimeUtc.HasValue)
                writer.WriteInt32LE(ToUnixTimeStamp(LastWriteTimeUtc.Value));
            if (headerType == ZipEntryHeaderType.LocalFileHeader)
            {
                if (LastAccessTimeUtc.HasValue)
                    writer.WriteInt32LE(ToUnixTimeStamp(LastAccessTimeUtc.Value));
                if (CreationTimeUtc.HasValue)
                    writer.WriteInt32LE(ToUnixTimeStamp(CreationTimeUtc.Value));
            }
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, IReadOnlyArray<byte> data, int index, int count)
        {
            LastWriteTimeUtc = null;
            LastAccessTimeUtc = null;
            CreationTimeUtc = null;
            var reader = new ByteArrayInputStream(data, index, count);
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
                    LastWriteTimeUtc = null;
                    LastAccessTimeUtc = null;
                    CreationTimeUtc = null;
                }
            }
        }
    }
}
using System;
using Utility.IO;

namespace ZipUtility.ZipExtraField
{
    public class UnixExtraFieldType0
        : UnixTimestampExtraField
    {
        private UInt16? _userId;
        private UInt16? _groupId;
        private ReadOnlyMemory<byte>? _additionalData;

        public UnixExtraFieldType0()
            : base(ExtraFieldId)
        {
            _userId = null;
            _groupId = null;
            _additionalData = null;
        }

        public const ushort ExtraFieldId = 13;

        public override ReadOnlyMemory<byte>? GetData(ZipEntryHeaderType headerType)
        {
            if (!LastAccessTimeUtc.HasValue || !LastWriteTimeUtc.HasValue || !_userId.HasValue || !_groupId.HasValue || !_additionalData.HasValue)
                return null;
            var writer = new ByteArrayRenderer();
            writer.WriteInt32LE(ToUnixTimeStamp(LastAccessTimeUtc.Value));
            writer.WriteInt32LE(ToUnixTimeStamp(LastWriteTimeUtc.Value));
            writer.WriteUInt16LE(UserId);
            writer.WriteUInt16LE(GroupId);
            writer.WriteBytes(AdditionalData);
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data)
        {
            LastAccessTimeUtc = null;
            LastWriteTimeUtc = null;
            _userId = null;
            _groupId = null;
            _additionalData = null;
            var reader = new ByteArrayParser(data);
            var success = false;
            try
            {
                LastAccessTimeUtc = FromUnixTimeStamp(reader.ReadInt32LE());
                LastWriteTimeUtc = FromUnixTimeStamp(reader.ReadInt32LE());
                UserId = reader.ReadUInt16LE();
                GroupId = reader.ReadUInt16LE();
                AdditionalData = reader.ReadAllBytes();
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
                    LastAccessTimeUtc = null;
                    LastWriteTimeUtc = null;
                    _userId = null;
                    _groupId = null;
                    _additionalData = null;
                }
            }
        }

        public override DateTime? CreationTimeUtc { get => null; set => throw new NotSupportedException(); }

        public UInt16 UserId
        {
            get => _userId ?? throw new InvalidOperationException();
            set => _userId = value;
        }

        public UInt16 GroupId
        {
            get => _groupId ?? throw new InvalidOperationException();
            set => _groupId = value;
        }

        public ReadOnlyMemory<byte> AdditionalData
        {
            get => _additionalData ?? throw new InvalidOperationException();
            set => _additionalData = value;
        }
    }
}

using System;
using Utility;
using Utility.IO;
using ZipUtility.Helper;

namespace ZipUtility.ZipExtraField
{
    public class NewUnixExtraField
        : ExtraField
    {
        private const byte _supportedVersion = 1;
        private IReadOnlyArray<byte> _uid;
        private IReadOnlyArray<byte> _gid;

        public NewUnixExtraField()
            : base(ExtraFieldId)
        {
            Version = 0;
            _uid = null;
            _gid = null;
        }

        public const UInt16 ExtraFieldId = 0x7875;

        public override IReadOnlyArray<byte> GetData(ZipEntryHeaderType headerType)
        {
            if (!IsOk)
                return null;
            var writer = new ByteArrayOutputStream();
            writer.WriteByte(Version);
            writer.WriteByte((byte)UID.Length);
            writer.WriteBytes(UID);
            writer.WriteByte((byte)GID.Length);
            writer.WriteBytes(GID);
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, IReadOnlyArray<byte> data, int index, int count)
        {
            _uid = null;
            _gid = null;
            var reader = new ByteArrayInputStream(data, index, count);
            var success = false;
            try
            {
                Version = reader.ReadByte();
                if (Version != _supportedVersion)
                    return;
                var uidSize = reader.ReadByte();
                UID = reader.ReadBytes(uidSize);
                var gidSize = reader.ReadByte();
                GID = reader.ReadBytes(gidSize);
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
                    Version = 0;
                    _uid = null;
                    _gid = null;
                }
            }
        }

        public bool IsOk =>
            Version == _supportedVersion &&
            _uid != null && _uid.Length <= byte.MaxValue &&
            _gid != null && _gid.Length <= byte.MaxValue;

        public byte Version { get; set; }
        public IReadOnlyArray<byte> UID { get => _uid; set => _uid = value == null && value.Length <= byte.MaxValue ? value : throw new ArgumentException("Null or too long values cannot be set in the UID."); }
        public IReadOnlyArray<byte> GID { get => _gid; set => _gid = value == null && value.Length <= byte.MaxValue ? value : throw new ArgumentException("Null or too long values cannot be set in the GID."); }
    }
}
using System;
using System.Linq;
using ZipUtility.Helper;

namespace ZipUtility.ZipExtraField
{
    public class NewUnixExtraField
        : ExtraField
    {
        private const byte _supportedVersion = 1;
        private byte[] _uid;
        private byte[] _gid;

        public NewUnixExtraField()
            : base(ExtraFieldId)
        {
            Version = 0;
            _uid = null;
            _gid = null;
        }


        public const UInt16 ExtraFieldId = 0x7875;

        public override byte[] GetData(ZipEntryHeaderType headerType)
        {
            if (!IsOk)
                return null;
            var writer = new ByteArrayOutputStream();
            writer.WriteByte(Version);
            writer.WriteByte((byte)UID.Length);
            writer.WriteBytes(UID);
            writer.WriteByte((byte)GID.Length);
            writer.WriteBytes(GID);
            return writer.ToByteSequence().ToArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, byte[] data, int index, int count)
        {
            var success = false;
            try
            {
                var reader = new ByteArrayInputStream(data, index, count);
                Version = reader.ReadByte();
                if (Version != _supportedVersion)
                    return;
                var uidSize = reader.ReadByte();
                UID = reader.ReadBytes(uidSize);
                var gidSize = reader.ReadByte();
                GID = reader.ReadBytes(gidSize);
                success = true;
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
            Version == 1 &&
            _uid != null && _uid.Length <= byte.MaxValue &&
            _gid != null && _gid.Length <= byte.MaxValue;

        public byte Version { get; set; }
        public byte[] UID { get => _uid; set => _uid = value == null && value.Length <= byte.MaxValue ? value : throw new ArgumentException("Null or too long values cannot be set in the UID."); }
        public byte[] GID { get => _gid; set => _gid = value == null && value.Length <= byte.MaxValue ? value : throw new ArgumentException("Null or too long values cannot be set in the GID."); }
    }
}
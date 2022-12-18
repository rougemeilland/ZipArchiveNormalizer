using System;
using Utility.IO;

namespace ZipUtility.ZipExtraField
{
    public class NewUnixExtraField
        : ExtraField
    {
        private const byte _supportedVersion = 1;
        private byte? _version;
        private ReadOnlyMemory<byte>? _uid;
        private ReadOnlyMemory<byte>? _gid;

        public NewUnixExtraField()
            : base(ExtraFieldId)
        {
            _version = null;
            _uid = null;
            _gid = null;
        }

        public const UInt16 ExtraFieldId = 0x7875;

        public override ReadOnlyMemory<byte>? GetData(ZipEntryHeaderType headerType)
        {
            if (!IsOk)
                return null;
            var writer = new ByteArrayRenderer();
            writer.WriteByte(Version);
            writer.WriteByte((byte)UID.Length);
            writer.WriteBytes(UID);
            writer.WriteByte((byte)GID.Length);
            writer.WriteBytes(GID);
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data)
        {
            _version = null;
            _uid = null;
            _gid = null;
            var reader = new ByteArrayParser(data);
            var success = false;
            try
            {
                var version = reader.ReadByte();
                if (version != _supportedVersion)
                    return;
                _version = version;
                var uidSize = reader.ReadByte();
                _uid = reader.ReadBytes(uidSize);
                var gidSize = reader.ReadByte();
                _gid = reader.ReadBytes(gidSize);
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
                    _version = null;
                    _uid = null;
                    _gid = null;
                }
            }
        }

        public bool IsOk =>
            Version == _supportedVersion &&
            _uid.HasValue && _uid.Value.Length <= byte.MaxValue &&
            _gid.HasValue && _gid.Value.Length <= byte.MaxValue;

        public byte Version
        {
            get => _version ?? throw new InvalidOperationException();
            set
            {
                _version = value;
            }
        }

        public ReadOnlyMemory<byte> UID
        {
            get => _uid ?? throw new InvalidOperationException();
            set
            {
                if (value.Length > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value), "Too Int64 array");
                _uid = value;
            }
        }

        public ReadOnlyMemory<byte> GID
        {
            get => _gid ?? throw new InvalidOperationException();
            set
            {
                if (value.Length > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value), "Too Int64 array");
                _gid = value;
            }
        }
    }
}

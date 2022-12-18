using System;
using Utility.IO;

namespace ZipUtility.ZipExtraField
{
    public class WindowsSecurityDescriptorExtraField
        : ExtraField
    {
        private const byte _supportedVersion = 0;
        private UInt32? _uncompressedSDSize;
        private byte? _version;
        private ZipEntryCompressionMethodId? _compressionType;
        private UInt32? _crc;
        private ReadOnlyMemory<byte>? _compressedSD;

        public WindowsSecurityDescriptorExtraField()
            : base(ExtraFieldId)
        {
            _uncompressedSDSize = null;
            _version = null;
            _compressionType = null;
            _crc = null;
            _compressedSD = null;
        }


        public const UInt16 ExtraFieldId = 0x4453;

        public override ReadOnlyMemory<byte>? GetData(ZipEntryHeaderType headerType)
        {
            switch (headerType)
            {
                case ZipEntryHeaderType.LocalFileHeader:
                    if (_uncompressedSDSize is null |
                        _version is null ||
                        _compressionType is null ||
                        _crc is null ||
                        _compressedSD is null)
                        return null;
                    break;
                case ZipEntryHeaderType.CentralDirectoryHeader:
                    if (_uncompressedSDSize is null)
                        return null;
                    break;
                default:
                    return null;
            }
            var writer = new ByteArrayRenderer();
            writer.WriteUInt32LE(UncompressedSDSize);
            if (headerType == ZipEntryHeaderType.LocalFileHeader)
            {
                writer.WriteByte(_supportedVersion);
                writer.WriteUInt16LE((UInt16)CompressionType);
                writer.WriteUInt32LE(Crc);
                writer.WriteBytes(CompressedSD);
            }
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data)
        {
            _uncompressedSDSize = null;
            _version = null;
            _compressionType = null;
            _crc = null;
            _compressedSD = null;
            Array.AsReadOnly(new byte[10]);
            var reader = new ByteArrayParser(data);
            var success = false;
            try
            {
                UncompressedSDSize = reader.ReadUInt32LE();
                if (headerType == ZipEntryHeaderType.LocalFileHeader)
                {
                    var version = reader.ReadByte();
                    if (version != _supportedVersion)
                        return;
                    CompressionType = (ZipEntryCompressionMethodId)reader.ReadUInt16LE();
                    Crc = reader.ReadUInt32LE();
                    _compressedSD = reader.ReadAllBytes();
                }
                else
                {
                    _version = null;
                    _compressionType = null;
                    _crc = null;
                    _compressedSD = null;
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
                    _uncompressedSDSize = null;
                    _version = null;
                    _compressionType = null;
                    _crc = null;
                    _compressedSD = null;
                }
            }
        }

        public UInt32 UncompressedSDSize
        {
            get => _uncompressedSDSize ?? throw new InvalidOperationException();
            set => _uncompressedSDSize = value;
        }

        public byte Version
        {
            get => _version ?? throw new InvalidOperationException();
            set => _version = value;
        }

        public ZipEntryCompressionMethodId CompressionType
        {
            get => _compressionType ?? throw new InvalidOperationException();
            set => _compressionType = value;
        }

        public UInt32 Crc
        {
            get => _crc ?? throw new InvalidOperationException();
            set => _crc = value;
        }

        public ReadOnlyMemory<byte> CompressedSD
        {
            get => _compressedSD ?? throw new InvalidOperationException();
            set => _compressedSD = value;
        }
    }
}

using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Linq;
using ZipUtility.Helper;

namespace ZipUtility.ZipExtraField
{
    public class WindowsSecurityDescriptorExtraField
        : ExtraField
    {
        private const byte _supportedVersion = 0;
        private UInt32? _uncompressedSDSize;
        private byte? _version;
        private CompressionMethod? _compressionType;
        private UInt32? _crc;

        public WindowsSecurityDescriptorExtraField()
            : base(ExtraFieldId)
        {
            _uncompressedSDSize = null;
            _version = null;
            _compressionType = null;
            _crc = null;
            CompressedSD = null;
        }


        public const UInt16 ExtraFieldId = 0x4453;

        public override byte[] GetData(ZipEntryHeaderType headerType)
        {
            switch (headerType)
            {
                case ZipEntryHeaderType.LocalFileHeader:
                    if (_uncompressedSDSize == null |
                        _version == null ||
                        _compressionType == null ||
                        _crc == null ||
                        CompressedSD == null)
                        return null;
                    break;
                case ZipEntryHeaderType.CentralDirectoryHeader:
                    if (_uncompressedSDSize == null)
                        return null;
                    break;
                default:
                    return null;
            }
            var writer = new ByteArrayOutputStream();
            writer.WriteUInt32LE(UncompressedSDSize);
            if (headerType == ZipEntryHeaderType.LocalFileHeader)
            {
                writer.WriteByte(_supportedVersion);
                writer.WriteUInt16LE((UInt16)CompressionType);
                writer.WriteUInt32LE(Crc);
                writer.WriteBytes(CompressedSD);
            }
            return writer.ToByteSequence().ToArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, byte[] data, int index, int count)
        {
            var success = false;
            try
            {
                var reader = new ByteArrayInputStream(data, index, count);
                UncompressedSDSize = reader.ReadUInt32LE();
                if (headerType == ZipEntryHeaderType.LocalFileHeader)
                {
                    var version = reader.ReadByte();
                    if (version != _supportedVersion)
                        return;
                    CompressionType = (CompressionMethod)reader.ReadUInt16LE();
                    Crc = reader.ReadUInt32LE();
                    CompressedSD = reader.ReadToEnd();
                }
                else
                {
                    _version = null;
                    _compressionType = null;
                    _crc = null;
                    CompressedSD = null;
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    _uncompressedSDSize = null;
                    _version = null;
                    _compressionType = null;
                    _crc = null;
                    CompressedSD = null;
                }
            }
        }

        public UInt32 UncompressedSDSize { get => _uncompressedSDSize.Value; set => _uncompressedSDSize = value; }
        public byte Version { get => _version.Value; set => _version = value; }
        public CompressionMethod CompressionType { get => _compressionType.Value; set => _compressionType = value; }
        public UInt32 Crc { get => _crc.Value; set => _crc = value; }
        public byte[] CompressedSD { get; set; }
    }
}
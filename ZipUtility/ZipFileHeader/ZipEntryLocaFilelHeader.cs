using System;
using System.IO;
using System.Linq;
using Utility;
using ZipUtility.ZipExtraField;

namespace ZipUtility.ZipFileHeader
{
    class ZipEntryLocaFilelHeader
        : ZipEntryInternalHeader<Zip64ExtendedInformationExtraFieldForLocalHeader>
    {
        private static byte[] _localHeaderSignature;
        private UInt32 _packedSizeValueInLocalFileHeader;
        private UInt32 _sizeValueInLocalFileHeader;

        static ZipEntryLocaFilelHeader()
        {
            _localHeaderSignature = new byte[] { 0x50, 0x4b, 0x03, 0x04 };
        }

        private ZipEntryLocaFilelHeader(ZipEntryGeneralPurposeBitFlag generalPurposeBitFlag, ZipEntryCompressionMethod? compressionMethod, DateTime? dosDateTime, UInt32 crc, UInt32 packedSizeValueInLocalFileHeader, UInt32 sizeValueInLocalFileHeader, IReadOnlyArray<byte> fullNameBytes, IReadOnlyArray<byte> commentBytes, IReadOnlyArray<byte> extraDataSource, ExtraFieldStorage extraFieldsOnCentralDirectoryHeader)
            : base(generalPurposeBitFlag, compressionMethod, dosDateTime, fullNameBytes, commentBytes, new ExtraFieldStorage(ZipEntryHeaderType.LocalFileHeader, extraDataSource))
        {
            Crc = crc;
            _packedSizeValueInLocalFileHeader = packedSizeValueInLocalFileHeader;
            _sizeValueInLocalFileHeader = sizeValueInLocalFileHeader;
            ApplyZip64ExtraField(ExtraFields.GetData<Zip64ExtendedInformationExtraFieldForLocalHeader>());
        }

        public UInt32 Crc { get; }
        public long PackedSize => Zip64ExtraField?.PackedSize ?? _packedSizeValueInLocalFileHeader;
        public long Size => Zip64ExtraField?.Size ?? _sizeValueInLocalFileHeader;
        public static ZipEntryLocaFilelHeader Parse(Stream zipFileBaseStream, ZipEntryCentralDirectoryHeader centralDirectoryHeader)
        {
            zipFileBaseStream.Seek(centralDirectoryHeader.LocalFileHeaderOffset, SeekOrigin.Begin);
            var minimumLengthOfHeader = 30;
            var minimumHeaderBytes = zipFileBaseStream.ReadBytes(minimumLengthOfHeader);
            var signature = minimumHeaderBytes.GetSequence(0, _localHeaderSignature.Length);
            if (!signature.SequenceEqual(_localHeaderSignature))
                throw new BadZipFormatException("Not found in local header in expected position");
            var generalPurposeBitFlag = (ZipEntryGeneralPurposeBitFlag)minimumHeaderBytes.ToUInt16(6);
            var compressionMethod = minimumHeaderBytes.ToUInt16(8); // この値はgeneralPurposeBitFlagのbit13がセットされているとゼロクリアされ意味を持たなくなる。
            var dosTime = minimumHeaderBytes.ToUInt16(10); // この値はgeneralPurposeBitFlagのbit13がセットされているとゼロクリアされ意味を持たなくなる。
            var dosDate = minimumHeaderBytes.ToUInt16(12); // この値はgeneralPurposeBitFlagのbit13がセットされているとゼロクリアされ意味を持たなくなる。
            var crc = minimumHeaderBytes.ToUInt32(14); // この値はgeneralPurposeBitFlagのbit13がセットされているとゼロクリアされ意味を持たなくなる。
            var packedSize = minimumHeaderBytes.ToUInt32(18); // この値は、ZIP64の場合、generalPurposeBitFlagのbit13がセットされていてもゼロクリアされず0xffffffffのまま。
            var size = minimumHeaderBytes.ToUInt32(22); // この値は、ZIP64の場合、generalPurposeBitFlagのbit13がセットされていてもゼロクリアされず0xffffffffのまま。
            var fileNameLength = minimumHeaderBytes.ToUInt16(26);
            var extraFieldLength = minimumHeaderBytes.ToUInt16(28);
            var fullNameBytes = zipFileBaseStream.ReadBytes(fileNameLength);
            var extraData = zipFileBaseStream.ReadBytes(extraFieldLength);

            // データディスクリプタが存在する場合は取得する
            if (generalPurposeBitFlag.HasFlag(ZipEntryGeneralPurposeBitFlag.HasDataDescriptor))
            {
                var dataDescriptor = ZipEntryDataDescriptor.Parse(zipFileBaseStream, centralDirectoryHeader.Index);
                crc = dataDescriptor.Crc;
                packedSize = dataDescriptor.PackedSize;
                size = dataDescriptor.Size;
            }

            var dosDateTime =
                (dosDate == 0 && dosTime == 0) || generalPurposeBitFlag.HasFlag(ZipEntryGeneralPurposeBitFlag.IsMoreStrongEncrypted)
                    ? (DateTime?)null
                    : new[] { dosDate, dosTime }.FromDosDateTimeToDateTime(DateTimeKind.Local).ToUniversalTime();

            return
                new ZipEntryLocaFilelHeader(
                    generalPurposeBitFlag,
                    generalPurposeBitFlag.HasFlag(ZipEntryGeneralPurposeBitFlag.IsMoreStrongEncrypted)
                        ? (ZipEntryCompressionMethod?)null
                        : (ZipEntryCompressionMethod)compressionMethod,
                    dosDateTime,
                    crc,
                    packedSize,
                    size,
                    fullNameBytes,
                    centralDirectoryHeader.CommentBytes,
                    extraData,
                    centralDirectoryHeader.ExtraFields);
        }

        protected override UInt32? PackedSizeInHeader { get => _packedSizeValueInLocalFileHeader; set => _packedSizeValueInLocalFileHeader = value ?? throw new InvalidOperationException(@"Do not set ""IZip64ExtendedInformationExtraFieldValueSource.PackedSize"" to null."); }
        protected override UInt32? SizeInHeader { get => _sizeValueInLocalFileHeader; set => _sizeValueInLocalFileHeader = value ?? throw new InvalidOperationException(@"Do not set ""IZip64ExtendedInformationExtraFieldValueSource.Size"" to null."); }
    }
}
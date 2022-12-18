using System;
using System.Linq;
using Utility;
using Utility.IO;
using ZipUtility.ZipExtraField;

namespace ZipUtility.ZipFileHeader
{
    class ZipEntryLocaFilelHeader
        : ZipEntryInternalHeader<Zip64ExtendedInformationExtraFieldForLocalHeader>
    {
        private static readonly ReadOnlyMemory<byte> _localHeaderSignature;

        private readonly ZipEntryDataDescriptor? _dataDescriptor;
        private readonly UInt32 _crcValueInLocalFileHeader;

        static ZipEntryLocaFilelHeader()
        {
            _localHeaderSignature = new byte[] { 0x50, 0x4b, 0x03, 0x04 }.AsReadOnly();
        }

        private ZipEntryLocaFilelHeader(ZipEntryGeneralPurposeBitFlag generalPurposeBitFlag, ZipEntryCompressionMethodId compressionMethod, DateTime? dosDateTime, UInt32 crc, UInt32 packedSizeValueInLocalFileHeader, UInt32 sizeValueInLocalFileHeader, ReadOnlyMemory<byte> fullNameBytes, ReadOnlyMemory<byte> commentBytes, ExtraFieldStorage extraFields, ZipStreamPosition dataPosition, ZipEntryDataDescriptor? dataDescriptor)
            : base(generalPurposeBitFlag, compressionMethod, dosDateTime, fullNameBytes, commentBytes, extraFields, extraFields.GetData<Zip64ExtendedInformationExtraFieldForLocalHeader>())
        {
            _dataDescriptor = dataDescriptor;
            _crcValueInLocalFileHeader = crc;
            PackedSizeInHeader = packedSizeValueInLocalFileHeader;
            SizeInHeader = sizeValueInLocalFileHeader;
            DataPosition = dataPosition;
        }

        public bool IsCheckedCrc { get => _dataDescriptor is not null; }
        public UInt32 Crc { get => _dataDescriptor?.Crc ?? _crcValueInLocalFileHeader; }
        public UInt64 PackedSize { get => _dataDescriptor?.PackedSize ?? Zip64ExtraField.PackedSize; set => Zip64ExtraField.PackedSize = value; }
        public UInt64 Size { get => _dataDescriptor?.Size ?? Zip64ExtraField.Size; set => Zip64ExtraField.Size = value; }
        public ZipStreamPosition DataPosition { get; }

        public static ZipEntryLocaFilelHeader Parse(IZipInputStream zipInputStream, ZipEntryCentralDirectoryHeader centralDirectoryHeader)
        {
            zipInputStream.Seek(centralDirectoryHeader.LocalFileHeaderPosition);
            var minimumLengthOfHeader = 30U;
            var minimumHeaderBytes = zipInputStream.ReadBytes(minimumLengthOfHeader);
            var signature = minimumHeaderBytes[.._localHeaderSignature.Length];
            if (!signature.Span.SequenceEqual(_localHeaderSignature.Span))
                throw new BadZipFileFormatException("Not found in local header in expected position");
            var generalPurposeBitFlag = (ZipEntryGeneralPurposeBitFlag)minimumHeaderBytes[6..].ToUInt16LE();
            if (generalPurposeBitFlag.HasEncryptionFlag())
                throw new EncryptedZipFileNotSupportedException((generalPurposeBitFlag & (ZipEntryGeneralPurposeBitFlag.Encrypted | ZipEntryGeneralPurposeBitFlag.EncryptedCentralDirectory | ZipEntryGeneralPurposeBitFlag.StrongEncrypted)).ToString());
            if (generalPurposeBitFlag.HasFlag(ZipEntryGeneralPurposeBitFlag.CompressedPatchedData))
                throw new NotSupportedSpecificationException("Not supported \"Compressed Patched Data\".");
            var compressionMethod = (ZipEntryCompressionMethodId)minimumHeaderBytes[8..].ToUInt16LE();
            var dosTime = minimumHeaderBytes[10..].ToUInt16LE();
            var dosDate = minimumHeaderBytes[12..].ToUInt16LE();
            var crc = minimumHeaderBytes[14..].ToUInt32LE();
            var packedSize = minimumHeaderBytes[18..].ToUInt32LE();
            var size = minimumHeaderBytes[22..].ToUInt32LE();
            var fileNameLength = minimumHeaderBytes[26..].ToUInt16LE();
            var extraFieldLength = minimumHeaderBytes[28..].ToUInt16LE();
            var fullNameBytes = zipInputStream.ReadBytes(fileNameLength);
            var extraData = zipInputStream.ReadBytes(extraFieldLength);
            var dataPosition = centralDirectoryHeader.LocalFileHeaderPosition + minimumLengthOfHeader + fileNameLength + extraFieldLength;

            var dosDateTime =
                (dosDate == 0 && dosTime == 0)
                    ? (DateTime?)null
                    : (dosDate, dosTime).FromDosDateTimeToDateTime(DateTimeKind.Local).ToUniversalTime();

            var dataDescriptor =
                generalPurposeBitFlag.HasFlag(ZipEntryGeneralPurposeBitFlag.HasDataDescriptor)
                    ? ZipEntryDataDescriptor.Parse(
                        zipInputStream,
                        dataPosition,
                        centralDirectoryHeader.PackedSize,
                        centralDirectoryHeader.Size,
                        size == UInt32.MaxValue || packedSize == UInt32.MaxValue,
                        compressionMethod,
                        generalPurposeBitFlag)
                    : null;

            var extraFields = new ExtraFieldStorage(ZipEntryHeaderType.LocalFileHeader, extraData);

            return
                new ZipEntryLocaFilelHeader(
                    generalPurposeBitFlag,
                    compressionMethod,
                    dosDateTime,
                    crc,
                    packedSize,
                    size,
                    fullNameBytes,
                    Array.Empty<byte>().AsReadOnly(),
                    extraFields,
                    dataPosition,
                    dataDescriptor);
        }

        protected override UInt32 PackedSizeInHeader { get; set; }
        protected override UInt32 SizeInHeader { get; set; }
    }
}

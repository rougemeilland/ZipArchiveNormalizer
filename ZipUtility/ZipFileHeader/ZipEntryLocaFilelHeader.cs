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
        private static byte[] _localHeaderSignature;
        private ZipEntryDataDescriptor _dataDescriptor;
        private UInt32 _crcValueInLocalFileHeader;

        static ZipEntryLocaFilelHeader()
        {
            _localHeaderSignature = new byte[] { 0x50, 0x4b, 0x03, 0x04 };
        }

        private ZipEntryLocaFilelHeader(ZipEntryGeneralPurposeBitFlag generalPurposeBitFlag, ZipEntryCompressionMethodId compressionMethod, DateTime? dosDateTime, UInt32 crc, UInt32 packedSizeValueInLocalFileHeader, UInt32 sizeValueInLocalFileHeader, IReadOnlyArray<byte> fullNameBytes, IReadOnlyArray<byte> commentBytes, IReadOnlyArray<byte> extraFieldsSource, ZipStreamPosition dataPosition, ZipEntryDataDescriptor dataDescriptor)
            : base(generalPurposeBitFlag, compressionMethod, dosDateTime, fullNameBytes, commentBytes, new ExtraFieldStorage(ZipEntryHeaderType.LocalFileHeader, extraFieldsSource))
        {
            _dataDescriptor = dataDescriptor;
            _crcValueInLocalFileHeader = crc;
            PackedSizeInHeader = packedSizeValueInLocalFileHeader;
            SizeInHeader = sizeValueInLocalFileHeader;
            DataPosition = dataPosition;

            // SizeInHeader, PackedSizeInHeader の設定後に実行する
            ApplyZip64ExtraField(ExtraFields.GetData<Zip64ExtendedInformationExtraFieldForLocalHeader>());
        }

        public bool IsCheckedCrc { get => _dataDescriptor != null; }
        public UInt32 Crc { get => _dataDescriptor?.Crc ?? _crcValueInLocalFileHeader; }
        public UInt64 PackedSize { get => _dataDescriptor?.PackedSize ?? Zip64ExtraField.PackedSize; set => Zip64ExtraField.PackedSize = value; }
        public UInt64 Size { get => _dataDescriptor?.Size ?? Zip64ExtraField.Size; set => Zip64ExtraField.Size = value; }
        public ZipStreamPosition DataPosition { get; }

        public static ZipEntryLocaFilelHeader Parse(IZipInputStream zipInputStream, ZipEntryCentralDirectoryHeader centralDirectoryHeader)
        {
            zipInputStream.Seek(centralDirectoryHeader.LocalFileHeaderPosition);
            var minimumLengthOfHeader = 30U;
            var minimumHeaderBytes = zipInputStream.ReadBytes(minimumLengthOfHeader);
            var signature = minimumHeaderBytes.GetSequence(0, _localHeaderSignature.Length);
            if (!signature.SequenceEqual(_localHeaderSignature))
                throw new BadZipFileFormatException("Not found in local header in expected position");
            var generalPurposeBitFlag = (ZipEntryGeneralPurposeBitFlag)minimumHeaderBytes.ToUInt16LE(6);
            if (generalPurposeBitFlag.HasEncryptionFlag())
                throw new EncryptedZipFileNotSupportedException((generalPurposeBitFlag & (ZipEntryGeneralPurposeBitFlag.Encrypted | ZipEntryGeneralPurposeBitFlag.EncryptedCentralDirectory | ZipEntryGeneralPurposeBitFlag.StrongEncrypted)).ToString());
            if (generalPurposeBitFlag.HasFlag(ZipEntryGeneralPurposeBitFlag.CompressedPatchedData))
                throw new NotSupportedSpecificationException("Not supported \"Compressed Patched Data\".");
            var compressionMethod = (ZipEntryCompressionMethodId)minimumHeaderBytes.ToUInt16LE(8);
            var dosTime = minimumHeaderBytes.ToUInt16LE(10);
            var dosDate = minimumHeaderBytes.ToUInt16LE(12);
            var crc = minimumHeaderBytes.ToUInt32LE(14);
            var packedSize = minimumHeaderBytes.ToUInt32LE(18);
            var size = minimumHeaderBytes.ToUInt32LE(22);
            var fileNameLength = minimumHeaderBytes.ToUInt16LE(26);
            var extraFieldLength = minimumHeaderBytes.ToUInt16LE(28);
            var fullNameBytes = zipInputStream.ReadBytes(fileNameLength);
            var extraData = zipInputStream.ReadBytes(extraFieldLength);
            var dataPosition = centralDirectoryHeader.LocalFileHeaderPosition + minimumLengthOfHeader + fileNameLength + extraFieldLength;

            var dosDateTime =
                (dosDate == 0 && dosTime == 0)
                    ? (DateTime?)null
                    : new[] { dosDate, dosTime }.FromDosDateTimeToDateTime(DateTimeKind.Local).ToUniversalTime();

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

            return
                new ZipEntryLocaFilelHeader(
                    generalPurposeBitFlag,
                    compressionMethod,
                    dosDateTime,
                    crc,
                    packedSize,
                    size,
                    fullNameBytes,
                    new byte[0].AsReadOnly(),
                    extraData,
                    dataPosition,
                    dataDescriptor);
        }

        protected override UInt32 PackedSizeInHeader { get; set; }
        protected override UInt32 SizeInHeader { get; set; }
    }
}
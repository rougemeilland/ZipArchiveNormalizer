using System;
using System.Collections.Generic;
using System.Linq;
using Utility;
using Utility.IO;
using ZipUtility.ZipExtraField;

namespace ZipUtility.ZipFileHeader
{
    class ZipEntryCentralDirectoryHeader
        : ZipEntryInternalHeader<Zip64ExtendedInformationExtraFieldForCentraHeader>
    {
        private static readonly ReadOnlyMemory<byte> _centralHeaderSignature;

        private ZipStreamPosition _localFileHeaderPosition;

        static ZipEntryCentralDirectoryHeader()
        {
            _centralHeaderSignature = new byte[] { 0x50, 0x4b, 0x01, 0x02 }.AsReadOnly();
        }

        private ZipEntryCentralDirectoryHeader(IZipInputStream zipInputStream, UInt64 index, ZipEntryHostSystem hostSystem, ZipEntryGeneralPurposeBitFlag generalPurposeBitFlag, ZipEntryCompressionMethodId compressionMethod, DateTime? dosDateTime, UInt32 crc, UInt32 packedSizeValueInCentralDirectory, UInt32 sizeValueInCentralDirectory, UInt16 diskStartNumberValueInCentralDirectory, UInt32 externalFileAttributes, UInt32 localFileHeaderOffsetValueInCentralDirectory, ReadOnlyMemory<byte> fullNameBytes, ReadOnlyMemory<byte> commentBytes, ExtraFieldStorage extraFields)
            : base(generalPurposeBitFlag, compressionMethod, dosDateTime, fullNameBytes, commentBytes, extraFields, extraFields.GetData<Zip64ExtendedInformationExtraFieldForCentraHeader>())
        {
            Index = index;
            HostSystem = hostSystem;
            Crc = crc;
            PackedSizeInHeader = packedSizeValueInCentralDirectory;
            SizeInHeader = sizeValueInCentralDirectory;
            DiskStartNumberInHeader = diskStartNumberValueInCentralDirectory;
            ExternalFileAttributes = externalFileAttributes;
            RelativeHeaderOffsetInHeader = localFileHeaderOffsetValueInCentralDirectory;

            _localFileHeaderPosition = zipInputStream.GetPosition(Zip64ExtraField.DiskStartNumber, Zip64ExtraField.RelativeHeaderOffset);
            IsDirectiry = CheckIfEntryNameIsDirectoryName();
        }

        public UInt64 Index { get; }
        public ZipEntryHostSystem HostSystem { get; }
        public UInt32 Crc { get; }
        public UInt64 PackedSize { get => Zip64ExtraField.PackedSize; set => Zip64ExtraField.PackedSize = value; }
        public UInt64 Size { get => Zip64ExtraField.Size; set => Zip64ExtraField.Size = value; }
        public UInt32 ExternalFileAttributes { get; }

        public ZipStreamPosition LocalFileHeaderPosition
        {
            get => _localFileHeaderPosition;

            set
            {
                var rawPosition = (IZipStreamPositionValue)value;
                _localFileHeaderPosition = value;
                Zip64ExtraField.DiskStartNumber = rawPosition.DiskNumber;
                Zip64ExtraField.RelativeHeaderOffset = rawPosition.OffsetOnTheDisk;
            }
        }

        public bool IsDirectiry { get; }
        public bool IsFile => !IsDirectiry;

        public static IEnumerable<ZipEntryCentralDirectoryHeader> Enumerate(IZipInputStream zipInputStream, ZipStreamPosition centralDirectoryPosition, UInt64 centralHeadersCount)
        {
            zipInputStream.Seek(centralDirectoryPosition);
            var centralHeaders = new List<ZipEntryCentralDirectoryHeader>();
            for (var index = 0UL; index < centralHeadersCount; index++)
                centralHeaders.Add(Parse(zipInputStream, index));
            return centralHeaders;
        }

        protected override UInt32 PackedSizeInHeader { get; set; }
        protected override UInt32 SizeInHeader { get; set; }
        protected override UInt32 RelativeHeaderOffsetInHeader { get; set; }
        protected override UInt16 DiskStartNumberInHeader { get; set; }

        private static ZipEntryCentralDirectoryHeader Parse(IZipInputStream zipInputStream, UInt64 index)
        {
            var minimumLengthOfHeader = 46;
            var minimumHeaderBytes = zipInputStream.ReadBytes(minimumLengthOfHeader);
            var signature = minimumHeaderBytes[.._centralHeaderSignature.Length];
            if (!signature.Span.SequenceEqual(_centralHeaderSignature.Span))
                throw new BadZipFileFormatException("Not found central header in expected position");
            var versionMadeBy = minimumHeaderBytes[4..].ToUInt16LE();
            var hostSystem = (ZipEntryHostSystem)(versionMadeBy >> 8);
            var generalPurposeBitFlag = (ZipEntryGeneralPurposeBitFlag)minimumHeaderBytes[8..].ToUInt16LE();
            if (generalPurposeBitFlag.HasEncryptionFlag())
                throw new EncryptedZipFileNotSupportedException((generalPurposeBitFlag & (ZipEntryGeneralPurposeBitFlag.Encrypted | ZipEntryGeneralPurposeBitFlag.EncryptedCentralDirectory | ZipEntryGeneralPurposeBitFlag.StrongEncrypted)).ToString());
            if (generalPurposeBitFlag.HasFlag(ZipEntryGeneralPurposeBitFlag.CompressedPatchedData))
                throw new NotSupportedSpecificationException("Not supported \"Compressed Patched Data\".");
            var compressionMethod = (ZipEntryCompressionMethodId)minimumHeaderBytes[10..].ToUInt16LE();
            var dosTime = minimumHeaderBytes[12..].ToUInt16LE();
            var dosDate = minimumHeaderBytes[14..].ToUInt16LE();
            var crc = minimumHeaderBytes[16..].ToUInt32LE();
            var packedSize = minimumHeaderBytes[20..].ToUInt32LE();
            var size = minimumHeaderBytes[24..].ToUInt32LE();
            var fileNameLength = minimumHeaderBytes[28..].ToUInt16LE();
            var extraFieldLength = minimumHeaderBytes[30..].ToUInt16LE();
            var commentLength = minimumHeaderBytes[32..].ToUInt16LE();
            var diskStartNumber = minimumHeaderBytes[34..].ToUInt16LE();
            var externalFileAttribute = minimumHeaderBytes[38..].ToUInt32LE();
            var relativeLocalFileHeaderOffset = minimumHeaderBytes[42..].ToUInt32LE();

            var fullNameBytes = zipInputStream.ReadBytes(fileNameLength);
            var extraDataSource = zipInputStream.ReadBytes(extraFieldLength);
            var commentBytes = zipInputStream.ReadBytes(commentLength);

            var dosDateTime =
                (dosDate == 0 && dosTime == 0)
                    ? (DateTime?)null
                    : (dosDate, dosTime).FromDosDateTimeToDateTime(DateTimeKind.Local).ToUniversalTime();

            var extraFields = new ExtraFieldStorage(ZipEntryHeaderType.CentralDirectoryHeader, extraDataSource);

            return
                new ZipEntryCentralDirectoryHeader(
                    zipInputStream,
                    index,
                    hostSystem,
                    generalPurposeBitFlag,
                    compressionMethod,
                    dosDateTime,
                    crc,
                    packedSize,
                    size,
                    diskStartNumber,
                    externalFileAttribute,
                    relativeLocalFileHeaderOffset,
                    fullNameBytes,
                    commentBytes,
                    extraFields);
        }

        private bool CheckIfEntryNameIsDirectoryName()
        {
            if (FullName.EndsWith("/"))
                return true;
            if (Size == 0 && PackedSize == 0 && !string.IsNullOrEmpty(FullName) && FullName.EndsWith("\\"))
            {
                switch (HostSystem)
                {
                    case ZipEntryHostSystem.FAT:
                    case ZipEntryHostSystem.Windows_NTFS:
                    case ZipEntryHostSystem.OS2_HPFS:
                    case ZipEntryHostSystem.VFAT:
                        return true;
                    default:
                        break;
                }
            }

            return
                HostSystem switch
                {
                    ZipEntryHostSystem.Amiga => (ExternalFileAttributes & 0x0c000000U) switch
                    {
                        0x08000000 => true,
                        _ => false,
                    },
                    ZipEntryHostSystem.FAT or ZipEntryHostSystem.Windows_NTFS or ZipEntryHostSystem.OS2_HPFS or ZipEntryHostSystem.VFAT => (ExternalFileAttributes & 0x0010U) != 0,
                    ZipEntryHostSystem.UNIX => (ExternalFileAttributes & 0xf0000000U) == 0x40000000U,
                    _ => false,
                };
        }
    }
}

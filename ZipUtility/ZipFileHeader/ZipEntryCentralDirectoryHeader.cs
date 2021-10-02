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
        private static byte[] _centralHeaderSignature;
        private ZipStreamPosition _localFileHeaderPosition;

        static ZipEntryCentralDirectoryHeader()
        {
            _centralHeaderSignature = new byte[] { 0x50, 0x4b, 0x01, 0x02 };
        }

        private ZipEntryCentralDirectoryHeader(IZipInputStream zipInputStream, UInt64 index, ZipEntryHostSystem hostSystem, ZipEntryGeneralPurposeBitFlag generalPurposeBitFlag, ZipEntryCompressionMethodId compressionMethod, DateTime? dosDateTime, UInt32 crc, UInt32 packedSizeValueInCentralDirectory, UInt32 sizeValueInCentralDirectory, UInt16 diskStartNumberValueInCentralDirectory, UInt32 externalFileAttributes, UInt32 localFileHeaderOffsetValueInCentralDirectory, IReadOnlyArray<byte> fullNameBytes, IReadOnlyArray<byte> commentBytes, IReadOnlyArray<byte> extraFieldDataSource)
            : base(generalPurposeBitFlag, compressionMethod, dosDateTime, fullNameBytes, commentBytes, new ExtraFieldStorage(ZipEntryHeaderType.CentralDirectoryHeader, extraFieldDataSource))
        {
            Index = index;
            HostSystem = hostSystem;
            Crc = crc;
            PackedSizeInHeader = packedSizeValueInCentralDirectory;
            SizeInHeader = sizeValueInCentralDirectory;
            DiskStartNumberInHeader = diskStartNumberValueInCentralDirectory;
            ExternalFileAttributes = externalFileAttributes;
            RelativeHeaderOffsetInHeader = localFileHeaderOffsetValueInCentralDirectory;
            
            ApplyZip64ExtraField(ExtraFields.GetData<Zip64ExtendedInformationExtraFieldForCentraHeader>());

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
                var rawPosition = value as IZipStreamPositionValue;
                if (rawPosition == null)
                    throw new Exception();
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
            var signature = minimumHeaderBytes.GetSequence(0, _centralHeaderSignature.Length);
            if (!signature.SequenceEqual(_centralHeaderSignature))
                throw new BadZipFileFormatException("Not found central header in expected position");
            var versionMadeBy = minimumHeaderBytes.ToUInt16LE(4);
            var hostSystem = (ZipEntryHostSystem)(versionMadeBy >> 8);
            var generalPurposeBitFlag = (ZipEntryGeneralPurposeBitFlag)minimumHeaderBytes.ToUInt16LE(8);
            if (generalPurposeBitFlag.HasEncryptionFlag())
                throw new EncryptedZipFileNotSupportedException((generalPurposeBitFlag & (ZipEntryGeneralPurposeBitFlag.Encrypted | ZipEntryGeneralPurposeBitFlag.EncryptedCentralDirectory | ZipEntryGeneralPurposeBitFlag.StrongEncrypted)).ToString());
            if (generalPurposeBitFlag.HasFlag(ZipEntryGeneralPurposeBitFlag.CompressedPatchedData))
                throw new NotSupportedSpecificationException("Not supported \"Compressed Patched Data\".");
            var compressionMethod = (ZipEntryCompressionMethodId)minimumHeaderBytes.ToUInt16LE(10);
            var dosTime = minimumHeaderBytes.ToUInt16LE(12);
            var dosDate = minimumHeaderBytes.ToUInt16LE(14);
            var crc = minimumHeaderBytes.ToUInt32LE(16);
            var packedSize = minimumHeaderBytes.ToUInt32LE(20);
            var size = minimumHeaderBytes.ToUInt32LE(24);
            var fileNameLength = minimumHeaderBytes.ToUInt16LE(28);
            var extraFieldLength = minimumHeaderBytes.ToUInt16LE(30);
            var commentLength = minimumHeaderBytes.ToUInt16LE(32);
            var diskStartNumber = minimumHeaderBytes.ToUInt16LE(34);
            var externalFileAttribute = minimumHeaderBytes.ToUInt32LE(38);
            var relativeLocalFileHeaderOffset = minimumHeaderBytes.ToUInt32LE(42);

            var fullNameBytes = zipInputStream.ReadBytes(fileNameLength);
            var extraDataSource = zipInputStream.ReadBytes(extraFieldLength);
            var commentBytes = zipInputStream.ReadBytes(commentLength);

            var dosDateTime =
                (dosDate == 0 && dosTime == 0)
                    ? (DateTime?)null
                    :  new[] { dosDate, dosTime }.FromDosDateTimeToDateTime(DateTimeKind.Local).ToUniversalTime();

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
                    extraDataSource);
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

            switch (HostSystem)
            {
                case ZipEntryHostSystem.Amiga:
                    switch (ExternalFileAttributes & 0x0c000000)
                    {
                        case 0x08000000:
                            return true;
                        case 0x04000000:
                        default:
                            return false;
                    }
                case ZipEntryHostSystem.FAT:
                case ZipEntryHostSystem.Windows_NTFS:
                case ZipEntryHostSystem.OS2_HPFS:
                case ZipEntryHostSystem.VFAT:
                    return ((ExternalFileAttributes & 0x0010) != 0);
                case ZipEntryHostSystem.UNIX:
                    return ((ExternalFileAttributes & 0xf0000000) == 0x40000000);
                case ZipEntryHostSystem.Atari_ST:
                case ZipEntryHostSystem.Macintosh:
                case ZipEntryHostSystem.OpenVMS:
                case ZipEntryHostSystem.VM_CMS:
                case ZipEntryHostSystem.AcornRisc:
                case ZipEntryHostSystem.MVS:
                default:
                    return false;
            }
        }
    }
}
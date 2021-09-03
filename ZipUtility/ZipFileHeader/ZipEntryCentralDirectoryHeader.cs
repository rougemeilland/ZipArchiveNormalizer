using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utility;
using ZipUtility.ZipExtraField;

namespace ZipUtility.ZipFileHeader
{
    class ZipEntryCentralDirectoryHeader
        : ZipEntryInternalHeader<Zip64ExtendedInformationExtraFieldForCentraHeader>
    {
        private static byte[] _centralHeaderSignature;
        private Int64 _zipStartOffset;
        private UInt32 _packedSizeValueInCentralDirectory;
        private UInt32 _sizeValueInCentralDirectory;
        private UInt32 _localFileHeaderOffsetValueInCentralDirectory;
        private UInt16 _diskStartNumberValueInCentralDirectory;

        static ZipEntryCentralDirectoryHeader()
        {
            _centralHeaderSignature = new byte[] { 0x50, 0x4b, 0x01, 0x02 };
        }

        private ZipEntryCentralDirectoryHeader(Int64 index, Int64 zipStartOffset, ZipEntryHostSystem hostSystem, ZipEntryGeneralPurposeBitFlag generalPurposeBitFlag, ZipEntryCompressionMethod? compressionMethod, DateTime? dosDateTime, UInt32 packedSizeValueInCentralDirectory, UInt32 sizeValueInCentralDirectory, UInt16 diskStartNumberValueInCentralDirectory, UInt32 externalFileAttributes, UInt32 localFileHeaderOffsetValueInCentralDirectory, IReadOnlyArray<byte> fullNameBytes, IReadOnlyArray<byte> commentBytes, IReadOnlyArray<byte> extraFieldDataSource)
            : base(generalPurposeBitFlag, compressionMethod, dosDateTime, fullNameBytes, commentBytes, new ExtraFieldStorage(ZipEntryHeaderType.CentralDirectoryHeader, extraFieldDataSource))
        {
            Index = index;
            _zipStartOffset = zipStartOffset;
            HostSystem = hostSystem;
            _packedSizeValueInCentralDirectory = packedSizeValueInCentralDirectory;
            _sizeValueInCentralDirectory = sizeValueInCentralDirectory;
            _diskStartNumberValueInCentralDirectory = diskStartNumberValueInCentralDirectory;
            ExternalFileAttributes = externalFileAttributes;
            _localFileHeaderOffsetValueInCentralDirectory = localFileHeaderOffsetValueInCentralDirectory;
            ApplyZip64ExtraField(ExtraFields.GetData<Zip64ExtendedInformationExtraFieldForCentraHeader>());
        }

        public long Index { get; }
        public ZipEntryHostSystem HostSystem { get; }
        public UInt32 DiskStartNumber => Zip64ExtraField?.DiskStartNumber ?? _diskStartNumberValueInCentralDirectory;
        public UInt32 ExternalFileAttributes { get; }
        public long LocalFileHeaderOffset => _zipStartOffset + (Zip64ExtraField?.RelativeHeaderOffset ?? _localFileHeaderOffsetValueInCentralDirectory);

        public static IEnumerable<ZipEntryCentralDirectoryHeader> Enumerate(Stream zipFileBaseStream, Int64 zipStartOffset, Int64 centralHeadersStartOffset, Int64 centralHeadersCount)
        {
            zipFileBaseStream.Seek(centralHeadersStartOffset, SeekOrigin.Begin);
            var centralHeaders = new List<ZipEntryCentralDirectoryHeader>();
            for (var index = 0L; index < centralHeadersCount; index++)
                centralHeaders.Add(Parse(zipFileBaseStream, index, zipStartOffset));
            return centralHeaders;
        }

        protected override UInt32? PackedSizeInHeader { get => _packedSizeValueInCentralDirectory; set => _packedSizeValueInCentralDirectory = value ?? throw new InvalidOperationException(@"Do not set ""IZip64ExtendedInformationExtraFieldValueSource.PackedSize"" to null."); }
        protected override UInt32? SizeInHeader { get => _sizeValueInCentralDirectory; set => _sizeValueInCentralDirectory = value ?? throw new InvalidOperationException(@"Do not set ""IZip64ExtendedInformationExtraFieldValueSource.Size"" to null."); }
        protected override UInt32? RelativeHeaderOffsetInHeader { get => _localFileHeaderOffsetValueInCentralDirectory; set => _localFileHeaderOffsetValueInCentralDirectory = value ?? throw new InvalidOperationException(@"Do not set ""IZip64ExtendedInformationExtraFieldValueSource.RelativeHeaderOffset"" to null."); }
        protected override UInt16? DiskStartNumberInHeader { get => _diskStartNumberValueInCentralDirectory; set => _diskStartNumberValueInCentralDirectory = value ?? throw new InvalidOperationException(@"Do not set ""IZip64ExtendedInformationExtraFieldValueSource.DiskStartNumber"" to null."); }

        private static ZipEntryCentralDirectoryHeader Parse(Stream zipFileBaseStream, Int64 index, Int64 zipStartOffset)
        {
            var minimumLengthOfHeader = 46;
            var minimumHeaderBytes = zipFileBaseStream.ReadBytes(minimumLengthOfHeader);
            var signature = minimumHeaderBytes.GetSequence(0, _centralHeaderSignature.Length);
            if (!signature.SequenceEqual(_centralHeaderSignature))
                throw new BadZipFormatException("Not found central header in expected position");
            var versionMadeBy = minimumHeaderBytes.ToUInt16(4);
            var hostSystem = (ZipEntryHostSystem)(versionMadeBy >> 8);
            var generalPurposeBitFlag = (ZipEntryGeneralPurposeBitFlag)minimumHeaderBytes.ToUInt16(8);
            var compressionMethod = (ZipEntryCompressionMethod)minimumHeaderBytes.ToUInt16(10); // この値はgeneralPurposeBitFlagのbit13がセットされているとゼロクリアされ意味を持たなくなる。
            var dosTime = minimumHeaderBytes.ToUInt16(12); // この値はgeneralPurposeBitFlagのbit13がセットされているとゼロクリアされ意味を持たなくなる。
            var dosDate = minimumHeaderBytes.ToUInt16(14); // この値はgeneralPurposeBitFlagのbit13がセットされているとゼロクリアされ意味を持たなくなる。
            var packedSize = minimumHeaderBytes.ToUInt32(20); // この値は、ZIP64の場合、generalPurposeBitFlagのbit13がセットされていてもゼロクリアされず0xffffffffのまま。
            var size = minimumHeaderBytes.ToUInt32(24); // この値は、ZIP64の場合、generalPurposeBitFlagのbit13がセットされていてもゼロクリアされず0xffffffffのまま。
            var fileNameLength = minimumHeaderBytes.ToUInt16(28);
            var extraFieldLength = minimumHeaderBytes.ToUInt16(30);
            var commentLength = minimumHeaderBytes.ToUInt16(32);
            var diskStartNumber = minimumHeaderBytes.ToUInt16(34);
            var externalFileAttribute = minimumHeaderBytes.ToUInt32(38);
            var relativeLocalFileHeaderOffset = minimumHeaderBytes.ToUInt32(42);

            var fullNameBytes = zipFileBaseStream.ReadBytes(fileNameLength);
            var extraDataSource = zipFileBaseStream.ReadBytes(extraFieldLength);
            var commentBytes = zipFileBaseStream.ReadBytes(commentLength);

            var dosDateTime =
                (dosDate == 0 && dosTime == 0) || generalPurposeBitFlag.HasFlag(ZipEntryGeneralPurposeBitFlag.IsMoreStrongEncrypted)
                    ? (DateTime?)null
                    :  new[] { dosDate, dosTime }.FromDosDateTimeToDateTime(DateTimeKind.Local).ToUniversalTime();

            return
                new ZipEntryCentralDirectoryHeader(
                    index,
                    zipStartOffset,
                    hostSystem,
                    generalPurposeBitFlag,
                    generalPurposeBitFlag.HasFlag(ZipEntryGeneralPurposeBitFlag.IsMoreStrongEncrypted)
                        ? (ZipEntryCompressionMethod?)null
                        : (ZipEntryCompressionMethod)compressionMethod,
                    dosDateTime,
                    packedSize,
                    size,
                    diskStartNumber,
                    externalFileAttribute,
                    relativeLocalFileHeaderOffset,
                    fullNameBytes,
                    commentBytes,
                    extraDataSource);
        }
    }
}
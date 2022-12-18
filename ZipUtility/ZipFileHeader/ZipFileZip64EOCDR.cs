using System;
using System.Collections.Generic;
using System.Linq;
using Utility;
using Utility.IO;

namespace ZipUtility.ZipFileHeader
{
    class ZipFileZip64EOCDR
    {
        private static readonly ReadOnlyMemory<byte> _zip64EndOfCentralDirectoryRecordSignature;

        static ZipFileZip64EOCDR()
        {
            _zip64EndOfCentralDirectoryRecordSignature = new byte[] { 0x50, 0x4b, 0x06, 0x06 }.AsReadOnly();
        }

        private ZipFileZip64EOCDR(UInt64 offsetOfThisHeader, UInt16 versionMadeBy, UInt16 versionNeededToExtract, UInt32 numberOfThisDisk, UInt32 numberOfTheDiskWithTheStartOfTheCentralDirectory, UInt64 totalNumberOfEntriesInTheCentralDirectoryOnThisDisk, UInt64 totalNumberOfEntriesInTheCentralDirectory, UInt64 sizeOfTheCentralDirectory, UInt64 offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber, IEnumerable<byte> zip64ExtensibleDataSector)
        {
            OffsetOfThisHeader = offsetOfThisHeader;
            VersionMadeBy = versionMadeBy;
            VersionNeededToExtract = versionNeededToExtract;
            NumberOfThisDisk = numberOfThisDisk;
            NumberOfTheDiskWithTheStartOfTheCentralDirectory = numberOfTheDiskWithTheStartOfTheCentralDirectory;
            TotalNumberOfEntriesInTheCentralDirectoryOnThisDisk = totalNumberOfEntriesInTheCentralDirectoryOnThisDisk;
            TotalNumberOfEntriesInTheCentralDirectory = totalNumberOfEntriesInTheCentralDirectory;
            SizeOfTheCentralDirectory = sizeOfTheCentralDirectory;
            OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;
            Zip64ExtensibleDataSector = zip64ExtensibleDataSector.ToArray();
        }

        public UInt64 OffsetOfThisHeader { get; }
        public UInt16 VersionMadeBy { get; }
        public UInt16 VersionNeededToExtract { get; }
        public UInt32 NumberOfThisDisk { get; }
        public UInt32 NumberOfTheDiskWithTheStartOfTheCentralDirectory { get; }
        public UInt64 TotalNumberOfEntriesInTheCentralDirectoryOnThisDisk { get; }
        public UInt64 TotalNumberOfEntriesInTheCentralDirectory { get; }
        public UInt64 SizeOfTheCentralDirectory { get; }
        public UInt64 OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber { get; }
        public IEnumerable<byte> Zip64ExtensibleDataSector { get; }

        public static ZipFileZip64EOCDR Parse(IZipInputStream zipInputStream, ZipFileZip64EOCDL previousHeader)
        {
            var minimumLengthOfHeader = 56U;
            if (previousHeader.OffsetOfThisHeader < minimumLengthOfHeader)
                throw new BadZipFileFormatException("Too short file for ZIP-64");
            zipInputStream.Seek(
                zipInputStream.GetPosition(
                    previousHeader.NumberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory,
                    previousHeader.OffsetOfTheZip64EndOfCentralDirectoryRecord));
            var minimumHeaderBytes = zipInputStream.ReadBytes(minimumLengthOfHeader);
            var signature = minimumHeaderBytes[.._zip64EndOfCentralDirectoryRecordSignature.Length];
            if (!signature.Span.SequenceEqual(_zip64EndOfCentralDirectoryRecordSignature.Span))
                throw new BadZipFileFormatException("Not found 'zip64 end of central directory record' for ZIP-64");
            var sizeOfZip64EndOfCentralDirectoryRecord = minimumHeaderBytes[4..].ToUInt64LE();
            var versionMadeBy = minimumHeaderBytes[12..].ToUInt16LE();
            var versionNeededToExtract = minimumHeaderBytes[14..].ToUInt16LE();
            var numberOfThisDisk = minimumHeaderBytes[16..].ToUInt32LE();
            var numberOfTheDiskWithTheStartOfTheCentralDirectory = minimumHeaderBytes[20..].ToUInt32LE();
            var totalNumberOfEntriesInTheCentralDirectoryOnThisDisk = minimumHeaderBytes[24..].ToUInt64LE();
            var totalNumberOfEntriesInTheCentralDirectory = minimumHeaderBytes[32..].ToUInt64LE();
            var sizeOfTheCentralDirectory = minimumHeaderBytes[40..].ToUInt64LE();
            var offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = minimumHeaderBytes[48..].ToUInt64LE();
            var zip64ExtensibleDataSector = zipInputStream.ReadByteSequence(sizeOfZip64EndOfCentralDirectoryRecord - minimumLengthOfHeader + 12);
            var centralDirectoryEncryptionHeader = ZipFileCentralDirectoryEncryptionHeader.Parse(zip64ExtensibleDataSector);
            if (centralDirectoryEncryptionHeader is not null)
                throw new EncryptedZipFileNotSupportedException(ZipEntryGeneralPurposeBitFlag.EncryptedCentralDirectory.ToString());
            return
                new ZipFileZip64EOCDR(
                    previousHeader.OffsetOfTheZip64EndOfCentralDirectoryRecord,
                    versionMadeBy,
                    versionNeededToExtract,
                    numberOfThisDisk,
                    numberOfTheDiskWithTheStartOfTheCentralDirectory,
                    totalNumberOfEntriesInTheCentralDirectoryOnThisDisk,
                    totalNumberOfEntriesInTheCentralDirectory,
                    sizeOfTheCentralDirectory,
                    offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber,
                    zip64ExtensibleDataSector);
        }
    }
}

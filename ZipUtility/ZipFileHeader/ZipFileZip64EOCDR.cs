using System;
using System.Collections.Generic;
using System.Linq;
using Utility;
using Utility.IO;

namespace ZipUtility.ZipFileHeader
{
    class ZipFileZip64EOCDR
    {
        private static byte[] _zip64EndOfCentralDirectoryRecordSignature;

        static ZipFileZip64EOCDR()
        {
            _zip64EndOfCentralDirectoryRecordSignature = new byte[] { 0x50, 0x4b, 0x06, 0x06 };
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
            var signature = minimumHeaderBytes.GetSequence(0, _zip64EndOfCentralDirectoryRecordSignature.Length);
            if (!signature.SequenceEqual(_zip64EndOfCentralDirectoryRecordSignature))
                throw new BadZipFileFormatException("Not found 'zip64 end of central directory record' for ZIP-64");
            var sizeOfZip64EndOfCentralDirectoryRecord = minimumHeaderBytes.ToUInt64LE(4);
            var versionMadeBy = minimumHeaderBytes.ToUInt16LE(12);
            var versionNeededToExtract = minimumHeaderBytes.ToUInt16LE(14);
            var numberOfThisDisk = minimumHeaderBytes.ToUInt32LE(16);
            var numberOfTheDiskWithTheStartOfTheCentralDirectory = minimumHeaderBytes.ToUInt32LE(20);
            var totalNumberOfEntriesInTheCentralDirectoryOnThisDisk = minimumHeaderBytes.ToUInt64LE(24);
            var totalNumberOfEntriesInTheCentralDirectory = minimumHeaderBytes.ToUInt64LE(32);
            var sizeOfTheCentralDirectory = minimumHeaderBytes.ToUInt64LE(40);
            var offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = minimumHeaderBytes.ToUInt64LE(48);
            var zip64ExtensibleDataSector = zipInputStream.ReadBytes(sizeOfZip64EndOfCentralDirectoryRecord - minimumLengthOfHeader + 12);
            var centralDirectoryEncryptionHeader = ZipFileCentralDirectoryEncryptionHeader.Parse(zip64ExtensibleDataSector);
            if (centralDirectoryEncryptionHeader != null)
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utility;

namespace ZipUtility.ZipFileHeader
{
    class ZipFileZip64EndOfCentralDirectoryRecord
    {
        private static byte[] _zip64EndOfCentralDirectoryRecordSignature;

        static ZipFileZip64EndOfCentralDirectoryRecord()
        {
            _zip64EndOfCentralDirectoryRecordSignature = new byte[] { 0x50, 0x4b, 0x06, 0x06 };
        }

        private ZipFileZip64EndOfCentralDirectoryRecord(Int64 offsetOfThisHeader, UInt16 versionMadeBy, UInt16 versionNeededToExtract, UInt32 numberOfThisDisk, UInt32 numberOfTheDiskWithTheStartOfTheCentralDirectory, Int64 totalNumberOfEntriesInTheCentralDirectoryOnThisDisk, Int64 totalNumberOfEntriesInTheCentralDirectory, Int64 sizeOfTheCentralDirectory, Int64 offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber, IEnumerable<byte> zip64ExtensibleDataSector)
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

        public Int64 OffsetOfThisHeader { get; }
        public UInt16 VersionMadeBy { get; }
        public UInt16 VersionNeededToExtract { get; }
        public UInt32 NumberOfThisDisk { get; }
        public UInt32 NumberOfTheDiskWithTheStartOfTheCentralDirectory { get; }
        public Int64 TotalNumberOfEntriesInTheCentralDirectoryOnThisDisk { get; }
        public Int64 TotalNumberOfEntriesInTheCentralDirectory { get; }
        public Int64 SizeOfTheCentralDirectory { get; }
        public Int64 OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber { get; }
        public IEnumerable<byte> Zip64ExtensibleDataSector { get; }

        public static ZipFileZip64EndOfCentralDirectoryRecord Parse(Stream zipInputStream, Int64 zipStartOffset, ZipFileZip64EndOfCentralDirectoryLocator previousHeader)
        {
            var minimumLengthOfHeader = 56;
            if (previousHeader.OffsetOfThisHeader < zipStartOffset + minimumLengthOfHeader)
                throw new BadZipFormatException("Too short file for ZIP-64");
            zipInputStream.Seek(previousHeader.OffsetOfTheZip64EndOfCentralDirectoryRecord, SeekOrigin.Begin);
            var minimumHeaderBytes = zipInputStream.ReadBytes(minimumLengthOfHeader);
            var signature = minimumHeaderBytes.GetSequence(0, _zip64EndOfCentralDirectoryRecordSignature.Length);
            if (!signature.SequenceEqual(_zip64EndOfCentralDirectoryRecordSignature))
                throw new BadZipFormatException("Not found 'zip64 end of central directory record' for ZIP-64");
            var sizeOfZip64EndOfCentralDirectoryRecord = minimumHeaderBytes.ToInt64(4);
            var versionMadeBy = minimumHeaderBytes.ToUInt16(12);
            var versionNeededToExtract = minimumHeaderBytes.ToUInt16(14);
            var numberOfThisDisk = minimumHeaderBytes.ToUInt32(16);
            var numberOfTheDiskWithTheStartOfTheCentralDirectory = minimumHeaderBytes.ToUInt32(20);
            var totalNumberOfEntriesInTheCentralDirectoryOnThisDisk = minimumHeaderBytes.ToInt64(24);
            var totalNumberOfEntriesInTheCentralDirectory = minimumHeaderBytes.ToInt64(32);
            var sizeOfTheCentralDirectory = minimumHeaderBytes.ToInt64(40);
            var offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = minimumHeaderBytes.ToInt64(48);
            var zip64ExtensibleDataSector = zipInputStream.ReadBytes(sizeOfZip64EndOfCentralDirectoryRecord - minimumLengthOfHeader + 12);
            return
                new ZipFileZip64EndOfCentralDirectoryRecord(
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
using System;
using System.Collections.Generic;
using System.IO;

namespace ZipUtility.ZipFileHeader
{
    class ZipFileInfo
    {
        private Int64 _zipStartOffset;
        private UInt32 _numberOfThisDisk;
        private UInt32 _diskWhereCentralDirectoryStarts;
        private Int64 _numberOfCentralDirectoryRecordsOnThisDisk;
        private Int64 _totalNumberOfCentralDirectoryRecords;
        private Int64 _sizeOfCentralDirectory;
        private Int64 _offsetOfStartOfCentralDirectory;


        public ZipFileInfo(Int64 zipStartOffset, UInt32 numberOfThisDisk, UInt32 diskWhereCentralDirectoryStarts, Int64 numberOfCentralDirectoryRecordsOnThisDisk, Int64 totalNumberOfCentralDirectoryRecords, Int64 sizeOfCentralDirectory, Int64 offsetOfStartOfCentralDirectory)
        {
            _zipStartOffset = zipStartOffset;
            _numberOfThisDisk = numberOfThisDisk;
            _diskWhereCentralDirectoryStarts = diskWhereCentralDirectoryStarts;
            _numberOfCentralDirectoryRecordsOnThisDisk = numberOfCentralDirectoryRecordsOnThisDisk;
            _totalNumberOfCentralDirectoryRecords = totalNumberOfCentralDirectoryRecords;
            _sizeOfCentralDirectory = sizeOfCentralDirectory;
            _offsetOfStartOfCentralDirectory = offsetOfStartOfCentralDirectory;
        }

        public IEnumerable<ZipEntryLocaFilelHeader> EnumerateEntry(Stream zipInputStream)
        {
            var centralHeaders = new List<ZipEntryCentralDirectoryHeader>();
            foreach (var centralHeader in ZipEntryCentralDirectoryHeader.Enumerate(zipInputStream, _zipStartOffset, _offsetOfStartOfCentralDirectory, _numberOfCentralDirectoryRecordsOnThisDisk))
                centralHeaders.Add(centralHeader);
            var localHeaders = new List<ZipEntryLocaFilelHeader>();
            foreach (var centralHeader in centralHeaders)
                localHeaders.Add(ZipEntryLocaFilelHeader.Parse(zipInputStream, centralHeader));
            return localHeaders;
        }

        public static ZipFileInfo Parse(Stream zipInputStream)
        {
            var zipStartOffset = zipInputStream.FindFirstSigunature(4, 0, zipInputStream.Length, (buffer, index) => buffer[index] == 0x50 && buffer[index + 1] ==  0x4b && (buffer[index + 2] == 0x03 && buffer[index + 3] == 0x04 || buffer[index + 2] < 0x10 && buffer[index + 3] < 0x10));
            if (zipStartOffset < 0)
                throw new BadZipFormatException("No local file header.");
            var eocd = ZipFileEOCD.Find(zipInputStream, zipStartOffset);
            var commentBytes = eocd.CommentBytes;
            var zip64EndOfCentralDirectoryLocator = ZipFileZip64EndOfCentralDirectoryLocator.Find(zipInputStream, zipStartOffset, eocd);
            if (eocd.IsRequiresZip64 || zip64EndOfCentralDirectoryLocator != null)
            {
                if (zip64EndOfCentralDirectoryLocator == null)
                    throw new BadZipFormatException("Not found 'zip64 end of central directory locator' in Zip file");
                var zip64EndOfCentralDirectoryRecord = ZipFileZip64EndOfCentralDirectoryRecord.Parse(zipInputStream, zipStartOffset, zip64EndOfCentralDirectoryLocator);
                var unknown1 = zip64EndOfCentralDirectoryRecord.NumberOfThisDisk;
                var unknown2 = zip64EndOfCentralDirectoryRecord.NumberOfTheDiskWithTheStartOfTheCentralDirectory;
                return new ZipFileInfo(
                    zipStartOffset,
                    zip64EndOfCentralDirectoryLocator.TotalNumberOfDisks,
                    zip64EndOfCentralDirectoryLocator.NumberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory,
                    zip64EndOfCentralDirectoryRecord.TotalNumberOfEntriesInTheCentralDirectoryOnThisDisk,
                    zip64EndOfCentralDirectoryRecord.TotalNumberOfEntriesInTheCentralDirectory,
                    zip64EndOfCentralDirectoryRecord.SizeOfTheCentralDirectory,
                    zip64EndOfCentralDirectoryRecord.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber);

            }
            else
            {
                if (eocd.OffsetOfStartOfCentralDirectory < eocd.OffsetOfThisHeader - eocd.SizeOfCentralDirectory)
                    throw new BadZipFormatException("Detected embedded resource?");

                return new ZipFileInfo(
                    zipStartOffset,
                    eocd.NumberOfThisDisk,
                    eocd.DiskWhereCentralDirectoryStarts,
                    eocd.NumberOfCentralDirectoryRecordsOnThisDisk,
                    eocd.TotalNumberOfCentralDirectoryRecords,
                    eocd.SizeOfCentralDirectory,
                    eocd.OffsetOfStartOfCentralDirectory);
            }
        }
    }
}
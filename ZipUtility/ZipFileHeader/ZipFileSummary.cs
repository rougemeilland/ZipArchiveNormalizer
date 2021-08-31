using System;
using System.Collections.Generic;
using System.IO;

namespace ZipUtility.ZipFileHeader
{
    class ZipFileSummary
    {
        private Int64 _zipStartOffset;
        private UInt32 _numberOfThisDisk;
        private UInt32 _diskWhereCentralDirectoryStarts;
        private Int64 _numberOfCentralDirectoryRecordsOnThisDisk;
        private Int64 _totalNumberOfCentralDirectoryRecords;
        private Int64 _sizeOfCentralDirectory;
        private Int64 _offsetOfStartOfCentralDirectory;


        public ZipFileSummary(Int64 zipStartOffset, UInt32 numberOfThisDisk, UInt32 diskWhereCentralDirectoryStarts, Int64 numberOfCentralDirectoryRecordsOnThisDisk, Int64 totalNumberOfCentralDirectoryRecords, Int64 sizeOfCentralDirectory, Int64 offsetOfStartOfCentralDirectory)
        {
            _zipStartOffset = zipStartOffset;
            _numberOfThisDisk = numberOfThisDisk;
            _diskWhereCentralDirectoryStarts = diskWhereCentralDirectoryStarts;
            _numberOfCentralDirectoryRecordsOnThisDisk = numberOfCentralDirectoryRecordsOnThisDisk;
            _totalNumberOfCentralDirectoryRecords = totalNumberOfCentralDirectoryRecords;
            _sizeOfCentralDirectory = sizeOfCentralDirectory;
            _offsetOfStartOfCentralDirectory = offsetOfStartOfCentralDirectory;
        }

        public IEnumerable<ZipEntryHeader> EnumerateEntry(Stream zipInputStream)
        {
            var centralHeaders = new List<ZipEntryCentralDirectoryHeader>();
            foreach (var centralHeader in ZipEntryCentralDirectoryHeader.Enumerate(zipInputStream, _zipStartOffset, _offsetOfStartOfCentralDirectory, _numberOfCentralDirectoryRecordsOnThisDisk))
                centralHeaders.Add(centralHeader);
            var headers = new List<ZipEntryHeader>();
            foreach (var centralHeader in centralHeaders)
                headers.Add(new ZipEntryHeader(centralHeader, ZipEntryLocaFilelHeader.Parse(zipInputStream, centralHeader)));
            return headers;
        }

        public static ZipFileSummary Parse(Stream zipInputStream)
        {
            var zipStartOffset = zipInputStream.FindFirstSigunature(4, 0, zipInputStream.Length, (buffer, index) => buffer[index] == 0x50 && buffer[index + 1] ==  0x4b && (buffer[index + 2] == 0x03 && buffer[index + 3] == 0x04 || buffer[index + 2] < 0x10 && buffer[index + 3] < 0x10));
            if (zipStartOffset < 0)
            {
                // ローカルファイルヘッダが一つもない場合
                // エントリが一つもないZIPファイルかあるいはそもそもZIPファイルではない場合が考えられる
                // この時点では区別がつかないので、とりあえず zipStartOffset = 0 にして続行する
                zipStartOffset = 0;
            }
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
                return new ZipFileSummary(
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

                return new ZipFileSummary(
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
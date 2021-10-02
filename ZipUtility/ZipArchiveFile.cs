using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Utility;
using Utility.IO;
using ZipUtility.ZipFileHeader;

namespace ZipUtility
{
    public class ZipArchiveFile
        : IDisposable
    {
        private static Int64 _serialNumber;
        private bool _isDisposed;
        private IZipInputStream _zipStream;
        private UInt32 _numberOfLastDisk;
        private ZipStreamPosition _centralDirectoryPosition;
        private UInt64 _totalNumberOfCentralDirectoryRecords;
        private UInt64 _sizeOfCentralDirectory;
        private Int64 _instanceId;

        static ZipArchiveFile()
        {
            _serialNumber = 0;
        }


        private ZipArchiveFile(IZipInputStream zipStream, UInt32 numberOfLastDisk, ZipStreamPosition centralDirectoryPosition, UInt64 totalNumberOfCentralDirectoryRecords, UInt64 sizeOfCentralDirectory, IReadOnlyArray<byte> commentBytes)
        {
            _isDisposed = false;
            _zipStream = zipStream;
            _numberOfLastDisk = numberOfLastDisk;
            _centralDirectoryPosition = centralDirectoryPosition;
            _totalNumberOfCentralDirectoryRecords = totalNumberOfCentralDirectoryRecords;
            _sizeOfCentralDirectory = sizeOfCentralDirectory;
            Comment = Encoding.Default.GetString(commentBytes);
            _instanceId = Interlocked.Increment(ref _serialNumber);
        }

        public string Comment { get; }

        internal static ZipArchiveFile Parse(IZipInputStream zipInputStream)
        {
            if (zipInputStream == null)
                throw new ArgumentNullException(nameof(zipInputStream));
            var lastDiskHeader = ZipFileLastDiskHeader.Parse(zipInputStream);
            if (lastDiskHeader.EOCDR.IsRequiresZip64 || lastDiskHeader.Zip64EOCDL != null)
            {
                if (lastDiskHeader.Zip64EOCDL == null)
                    throw new BadZipFileFormatException("Not found 'zip64 end of central directory locator' in Zip file");
                if (!zipInputStream.IsMultiVolumeZipStream && lastDiskHeader.Zip64EOCDL.TotalNumberOfDisks > 1)
                    throw new MultiVolumeDetectedException(lastDiskHeader.Zip64EOCDL.TotalNumberOfDisks - 1U);
#if DEBUG
                if (lastDiskHeader.Zip64EOCDL.TotalNumberOfDisks <= 0)
                    throw new Exception();
                if (lastDiskHeader.Zip64EOCDL.NumberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory >= lastDiskHeader.Zip64EOCDL.TotalNumberOfDisks)
                    throw new Exception();
#endif
                var zip64EOCDR = ZipFileZip64EOCDR.Parse(zipInputStream, lastDiskHeader.Zip64EOCDL);
                var unknown1 = zip64EOCDR.NumberOfThisDisk;
                var unknown2 = zip64EOCDR.NumberOfTheDiskWithTheStartOfTheCentralDirectory;
                var centralDirectoryPosition =
                    zipInputStream.GetPosition(
                        zip64EOCDR.NumberOfTheDiskWithTheStartOfTheCentralDirectory,
                        zip64EOCDR.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber);
                return
                    new ZipArchiveFile(
                        zipInputStream,
                        zip64EOCDR.NumberOfThisDisk,
                        centralDirectoryPosition,
                        zip64EOCDR.TotalNumberOfEntriesInTheCentralDirectory,
                        zip64EOCDR.SizeOfTheCentralDirectory,
                        lastDiskHeader.EOCDR.CommentBytes);

            }
            else
            {
#if false
                if (lastDiskHeader.EOCDR.OffsetOfStartOfCentralDirectory < lastDiskHeader.EOCDR.OffsetOfThisHeader - lastDiskHeader.EOCDR.SizeOfCentralDirectory)
                    throw new BadZipFileFormatException("Detected embedded resource?");
#endif
                if (!zipInputStream.IsMultiVolumeZipStream && lastDiskHeader.EOCDR.NumberOfThisDisk >= 1)
                    throw new MultiVolumeDetectedException(lastDiskHeader.EOCDR.NumberOfThisDisk);
#if DEBUG
                if (lastDiskHeader.EOCDR.DiskWhereCentralDirectoryStarts > lastDiskHeader.EOCDR.NumberOfThisDisk)
                    throw new Exception();
#endif
                var centralDirectoryPosition =
                    zipInputStream.GetPosition(
                        lastDiskHeader.EOCDR.DiskWhereCentralDirectoryStarts,
                        lastDiskHeader.EOCDR.OffsetOfStartOfCentralDirectory);
                return
                    new ZipArchiveFile(
                        zipInputStream,
                        lastDiskHeader.EOCDR.NumberOfThisDisk,
                        centralDirectoryPosition,
                        lastDiskHeader.EOCDR.TotalNumberOfCentralDirectoryRecords,
                        lastDiskHeader.EOCDR.SizeOfCentralDirectory,
                        lastDiskHeader.EOCDR.CommentBytes);
            }
        }

        public ZipArchiveEntryCollection GetEntries()
        {
            var headerArray =
                ZipEntryCentralDirectoryHeader.Enumerate(_zipStream, _centralDirectoryPosition, _totalNumberOfCentralDirectoryRecords)
                    .Select(centralHeader => new ZipEntryHeader(centralHeader, ZipEntryLocaFilelHeader.Parse(_zipStream, centralHeader)))
                    .QuickSort(header => header.CentralDirectoryHeader.LocalFileHeaderPosition);
            var entries = new List<ZipArchiveEntry>();
            var localHeaderOrder = 0UL;
            foreach (var header in headerArray)
            {
                entries.Add(new ZipArchiveEntry(header, localHeaderOrder, _instanceId));
                ++localHeaderOrder;
            }
            return new ZipArchiveEntryCollection(entries);
        }

        public IInputByteStream<UInt64> GetInputStream(ZipArchiveEntry entry)
        {
            if (entry.ZipFileInstanceId != _instanceId)
                throw new ArgumentException();
            return entry.GetInputStream(_zipStream);
        }

        public void CheckEntry(ZipArchiveEntry entry, Action<int> progressAction = null)
        {
            if (entry.ZipFileInstanceId != _instanceId)
                throw new ArgumentException();
            entry.CheckData(_zipStream, progressAction);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_zipStream != null)
                    {
                        _zipStream.Dispose();
                        _zipStream = null;
                    }
                }
                _isDisposed = true;
            }
        }
    }
}
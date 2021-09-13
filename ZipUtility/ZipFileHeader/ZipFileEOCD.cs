using System;
using System.IO;
using System.Linq;
using Utility;
using Utility.IO;

namespace ZipUtility.ZipFileHeader
{
    class ZipFileEOCD
    {
        private static byte[] _eocdSignature;

        static ZipFileEOCD()
        {
            _eocdSignature = new byte[] { 0x50, 0x4b, 0x05, 0x06 };
        }

        private ZipFileEOCD(Int64 offsetOfThisHeader, UInt16 numberOfThisDisk, UInt16 diskWhereCentralDirectoryStarts, UInt16 numberOfCentralDirectoryRecordsOnThisDisk, UInt16 totalNumberOfCentralDirectoryRecords, UInt32 sizeOfCentralDirectory, UInt32 offsetOfStartOfCentralDirectory, IReadOnlyArray<byte> commentBytes)
        {
            OffsetOfThisHeader = offsetOfThisHeader;
            NumberOfThisDisk = numberOfThisDisk;
            DiskWhereCentralDirectoryStarts = diskWhereCentralDirectoryStarts;
            NumberOfCentralDirectoryRecordsOnThisDisk = numberOfCentralDirectoryRecordsOnThisDisk;
            TotalNumberOfCentralDirectoryRecords = totalNumberOfCentralDirectoryRecords;
            SizeOfCentralDirectory = sizeOfCentralDirectory;
            OffsetOfStartOfCentralDirectory = offsetOfStartOfCentralDirectory;
            CommentBytes = commentBytes.ToArray();
            IsRequiresZip64 =
                NumberOfThisDisk == 0xffffU ||
                DiskWhereCentralDirectoryStarts == 0xffffU ||
                NumberOfCentralDirectoryRecordsOnThisDisk == 0xffffU ||
                TotalNumberOfCentralDirectoryRecords == 0xffffU ||
                SizeOfCentralDirectory == 0xffffffffUL ||
                OffsetOfStartOfCentralDirectory == 0xffffffffUL;
        }

        public Int64 OffsetOfThisHeader { get; }
        public UInt16 NumberOfThisDisk { get; }
        public UInt16 DiskWhereCentralDirectoryStarts { get; }
        public UInt16 NumberOfCentralDirectoryRecordsOnThisDisk { get; }
        public UInt16 TotalNumberOfCentralDirectoryRecords { get; }
        public UInt32 SizeOfCentralDirectory { get; }
        public UInt32 OffsetOfStartOfCentralDirectory { get; }
        public byte[] CommentBytes { get; }
        public bool IsRequiresZip64 { get; }

        public static ZipFileEOCD Find(Stream zipInputStream, Int64 zipStartOffset)
        {
            var zipFileLength = zipInputStream.Seek(0, SeekOrigin.End);
            var minimumLengthOfHeader = 22;
            var maximumLengthOfHeader = 22 + 0xffff;
            var offset = zipFileLength - maximumLengthOfHeader;
            if (offset < zipStartOffset)
                offset = zipStartOffset;
            if (zipFileLength < offset + minimumLengthOfHeader)
                throw new BadZipFileFormatException("Too short Zip file");
            var offsetOfThisHeader =
                zipInputStream.FindLastSigunature(
                    _eocdSignature,
                    offset,
                    zipFileLength - offset - minimumLengthOfHeader + _eocdSignature.Length);
            if (offsetOfThisHeader < 0)
                throw new BadZipFileFormatException("EOCD Not found in Zip file");
            zipInputStream.Seek(offsetOfThisHeader, SeekOrigin.Begin);
            var minimumHeader = zipInputStream.ReadBytes(22);
            var signature = minimumHeader.GetSequence(0, _eocdSignature.Length);
            if (!signature.SequenceEqual(_eocdSignature))
                throw new Exception();
            var numberOfThisDisk = minimumHeader.ToUInt16LE(4);
            var diskWhereCentralDirectoryStarts = minimumHeader.ToUInt16LE(6);
            var numberOfCentralDirectoryRecordsOnThisDisk = minimumHeader.ToUInt16LE(8);
            var totalNumberOfCentralDirectoryRecords = minimumHeader.ToUInt16LE(10);
            var sizeOfCentralDirectory = minimumHeader.ToUInt32LE(12);
            var offsetOfStartOfCentralDirectory = minimumHeader.ToUInt32LE(16);
            var commentLength = minimumHeader.ToUInt16LE(20);
            var commentBytes = zipInputStream.ReadBytes(commentLength);
            return
                new ZipFileEOCD(
                    offsetOfThisHeader,
                    numberOfThisDisk,
                    diskWhereCentralDirectoryStarts,
                    numberOfCentralDirectoryRecordsOnThisDisk,
                    totalNumberOfCentralDirectoryRecords,
                    sizeOfCentralDirectory,
                    offsetOfStartOfCentralDirectory,
                    commentBytes);
        }
    }
}
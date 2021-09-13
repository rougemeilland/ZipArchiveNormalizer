using System;
using System.IO;
using System.Linq;
using Utility;
using Utility.IO;

namespace ZipUtility.ZipFileHeader
{
    class ZipFileZip64EndOfCentralDirectoryLocator
    {
        private static byte[] _zip64EndOfCentralDirectoryLocatorSignature;

        static ZipFileZip64EndOfCentralDirectoryLocator()
        {
            _zip64EndOfCentralDirectoryLocatorSignature = new byte[] { 0x50, 0x4b, 0x06, 0x07 };

        }

        private ZipFileZip64EndOfCentralDirectoryLocator(Int64 offsetOfThisHeader, UInt32 numberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory, Int64 offsetOfTheZip64EndOfCentralDirectoryRecord, UInt32 totalNumberOfDisks)
        {
            OffsetOfThisHeader = offsetOfThisHeader;
            NumberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory = numberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory;
            OffsetOfTheZip64EndOfCentralDirectoryRecord = offsetOfTheZip64EndOfCentralDirectoryRecord;
            TotalNumberOfDisks = totalNumberOfDisks;
        }

        public Int64 OffsetOfThisHeader { get; }
        public UInt32 NumberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory { get; }
        public Int64 OffsetOfTheZip64EndOfCentralDirectoryRecord { get; }
        public UInt32 TotalNumberOfDisks { get; }


        public static ZipFileZip64EndOfCentralDirectoryLocator Find(Stream zipInputStream, Int64 zipStartOffset, ZipFileEOCD previoudHeader)
        {
            var lengthOfZip64EndOfCentralDirectoryLocator = 20;
            if (previoudHeader.OffsetOfThisHeader < zipStartOffset + lengthOfZip64EndOfCentralDirectoryLocator)
                return null;
            var offsetOfThisHeader = previoudHeader.OffsetOfThisHeader - lengthOfZip64EndOfCentralDirectoryLocator;
            zipInputStream.Seek(offsetOfThisHeader, SeekOrigin.Begin);
            var header = zipInputStream.ReadBytes(22);
            var signature = header.GetSequence(0, _zip64EndOfCentralDirectoryLocatorSignature.Length);
            if (!signature.SequenceEqual(_zip64EndOfCentralDirectoryLocatorSignature))
                return null;
            var numberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory = header.ToUInt32LE(4);
            var offsetOfTheZip64EndOfCentralDirectoryRecord = header.ToInt64LE(8);
            var totalNumberOfDisks = header.ToUInt32LE(16);
            return
                new ZipFileZip64EndOfCentralDirectoryLocator(
                    offsetOfThisHeader,
                    numberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory,
                    offsetOfTheZip64EndOfCentralDirectoryRecord + zipStartOffset,
                    totalNumberOfDisks);
        }
    }
}
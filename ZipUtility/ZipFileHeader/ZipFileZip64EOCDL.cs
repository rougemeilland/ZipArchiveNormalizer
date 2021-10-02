using System;
using System.IO;
using System.Linq;
using Utility;
using Utility.IO;

namespace ZipUtility.ZipFileHeader
{
    class ZipFileZip64EOCDL
    {
        private static byte[] _zip64EndOfCentralDirectoryLocatorSignature;

        static ZipFileZip64EOCDL()
        {
            _zip64EndOfCentralDirectoryLocatorSignature = new byte[] { 0x50, 0x4b, 0x06, 0x07 };

        }

        private ZipFileZip64EOCDL(UInt64 offsetOfThisHeader, UInt32 numberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory, UInt64 offsetOfTheZip64EndOfCentralDirectoryRecord, UInt32 totalNumberOfDisks)
        {
            OffsetOfThisHeader = offsetOfThisHeader;
            NumberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory = numberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory;
            OffsetOfTheZip64EndOfCentralDirectoryRecord = offsetOfTheZip64EndOfCentralDirectoryRecord;
            TotalNumberOfDisks = totalNumberOfDisks;
        }

        public UInt64 OffsetOfThisHeader { get; }
        public UInt32 NumberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory { get; }
        public UInt64 OffsetOfTheZip64EndOfCentralDirectoryRecord { get; }
        public UInt32 TotalNumberOfDisks { get; }


        public static ZipFileZip64EOCDL Find(IRandomInputByteStream<UInt64> zipInputStream, ZipFileEOCDR previoudHeader)
        {
            var lengthOfZip64EndOfCentralDirectoryLocator = 20U;
            if (previoudHeader.OffsetOfThisHeader < lengthOfZip64EndOfCentralDirectoryLocator)
                return null;
            var offsetOfThisHeader = previoudHeader.OffsetOfThisHeader - lengthOfZip64EndOfCentralDirectoryLocator;
            zipInputStream.Seek(offsetOfThisHeader);
            var header = zipInputStream.ReadBytes(22);
            var signature = header.GetSequence(0, _zip64EndOfCentralDirectoryLocatorSignature.Length);
            if (!signature.SequenceEqual(_zip64EndOfCentralDirectoryLocatorSignature))
                return null;
            var numberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory = header.ToUInt32LE(4);
            var offsetOfTheZip64EndOfCentralDirectoryRecord = header.ToUInt64LE(8);
            var totalNumberOfDisks = header.ToUInt32LE(16);
            return
                new ZipFileZip64EOCDL(
                    offsetOfThisHeader,
                    numberOfTheDiskWithTheStartOfTheZip64EndOfCentralDirectory,
                    offsetOfTheZip64EndOfCentralDirectoryRecord,
                    totalNumberOfDisks);
        }
    }
}
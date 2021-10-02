using System;
using System.IO;
using System.Linq;
using Utility;
using Utility.IO;

namespace ZipUtility.ZipFileHeader
{
    class ZipFileEOCDR
    {
        private static byte[] _eocdSignature;
        private UInt16 _numberOfCentralDirectoryRecordsOnThisDisk;

        static ZipFileEOCDR()
        {
            _eocdSignature = new byte[] { 0x50, 0x4b, 0x05, 0x06 };
        }

        private ZipFileEOCDR(UInt64 offsetOfThisHeader, UInt16 numberOfThisDisk, UInt16 diskWhereCentralDirectoryStarts, UInt16 numberOfCentralDirectoryRecordsOnThisDisk, UInt16 totalNumberOfCentralDirectoryRecords, UInt32 sizeOfCentralDirectory, UInt32 offsetOfStartOfCentralDirectory, IReadOnlyArray<byte> commentBytes)
        {
            OffsetOfThisHeader = offsetOfThisHeader;
            NumberOfThisDisk = numberOfThisDisk;
            DiskWhereCentralDirectoryStarts = diskWhereCentralDirectoryStarts;
            _numberOfCentralDirectoryRecordsOnThisDisk = numberOfCentralDirectoryRecordsOnThisDisk;
            TotalNumberOfCentralDirectoryRecords = totalNumberOfCentralDirectoryRecords;
            SizeOfCentralDirectory = sizeOfCentralDirectory;
            OffsetOfStartOfCentralDirectory = offsetOfStartOfCentralDirectory;
            CommentBytes = commentBytes;
            IsRequiresZip64 =
                NumberOfThisDisk == UInt16.MaxValue ||
                DiskWhereCentralDirectoryStarts == UInt16.MaxValue ||
                _numberOfCentralDirectoryRecordsOnThisDisk == UInt16.MaxValue ||
                TotalNumberOfCentralDirectoryRecords == UInt16.MaxValue ||
                SizeOfCentralDirectory == UInt32.MaxValue ||
                OffsetOfStartOfCentralDirectory == UInt32.MaxValue;
        }

        public UInt64 OffsetOfThisHeader { get; }
        public UInt16 NumberOfThisDisk { get; }
        public UInt16 DiskWhereCentralDirectoryStarts { get; }

        /// <summary>
        /// EOCDのあるディスクに含まれるセントラルディレクトリレコードの数。
        /// </summary>
        /// <remarks>
        /// PKZIPの実装では、最後のディスクがEOCDから始まっている場合でも、このプロパティが1になってしまう。
        /// このプロパティの値をもとにセントラルディレクトリヘッダを探してはならない。
        /// </remarks>
        [Obsolete]
        public UInt16 NumberOfCentralDirectoryRecordsOnThisDisk => _numberOfCentralDirectoryRecordsOnThisDisk;

        public UInt16 TotalNumberOfCentralDirectoryRecords { get; }
        public UInt32 SizeOfCentralDirectory { get; }
        public UInt32 OffsetOfStartOfCentralDirectory { get; }
        public IReadOnlyArray<byte> CommentBytes { get; }
        public bool IsRequiresZip64 { get; }

        public static ZipFileEOCDR Find(IRandomInputByteStream<UInt64> zipInputStream)
        {
            var zipFileLength = zipInputStream.Length;
            var minimumLengthOfHeader = 22U;
            var maximumLengthOfHeader = 22U + UInt16.MaxValue;
            var offsetLowerLimit =
                zipFileLength > maximumLengthOfHeader
                ? zipFileLength - maximumLengthOfHeader
                : 0;
            var offsetUpperLimit =
                zipFileLength > minimumLengthOfHeader - (uint)_eocdSignature.Length
                ? zipFileLength - minimumLengthOfHeader + (uint)_eocdSignature.Length
                : 0;
            if (zipFileLength < offsetLowerLimit + minimumLengthOfHeader)
                throw new BadZipFileFormatException("Too short Zip file");
            while (offsetUpperLimit >= offsetLowerLimit + (uint)_eocdSignature.Length)
            {
                var offsetOfThisHeader =
                    zipInputStream.FindLastSigunature(_eocdSignature, offsetLowerLimit, offsetUpperLimit - offsetLowerLimit)
                    ?? throw new BadZipFileFormatException("EOCD Not found in Zip file");
                zipInputStream.Seek(offsetOfThisHeader);
                try
                {
                    var minimumHeader = zipInputStream.ReadBytes(minimumLengthOfHeader);
                    var signature = minimumHeader.GetSequence(0, _eocdSignature.Length);
                    if (!signature.SequenceEqual(_eocdSignature))
                        throw new BadZipFileFormatException();
                    var numberOfThisDisk = minimumHeader.ToUInt16LE(4);
                    var diskWhereCentralDirectoryStarts = minimumHeader.ToUInt16LE(6);
                    var numberOfCentralDirectoryRecordsOnThisDisk = minimumHeader.ToUInt16LE(8);
                    var totalNumberOfCentralDirectoryRecords = minimumHeader.ToUInt16LE(10);
                    var sizeOfCentralDirectory = minimumHeader.ToUInt32LE(12);
                    var offsetOfStartOfCentralDirectory = minimumHeader.ToUInt32LE(16);
                    var commentLength = minimumHeader.ToUInt16LE(20);
                    var commentBytes = zipInputStream.ReadBytes(commentLength);
                    return
                        new ZipFileEOCDR(
                            offsetOfThisHeader,
                            numberOfThisDisk,
                            diskWhereCentralDirectoryStarts,
                            numberOfCentralDirectoryRecordsOnThisDisk,
                            totalNumberOfCentralDirectoryRecords,
                            sizeOfCentralDirectory,
                            offsetOfStartOfCentralDirectory,
                            commentBytes);
                }
                catch (UnexpectedEndOfStreamException)
                {
                }
                offsetUpperLimit = (offsetOfThisHeader + (uint)_eocdSignature.Length - 1U).Maximum(0U);
            }
            throw new BadZipFileFormatException("EOCD Not found in Zip file");
        }
    }
}
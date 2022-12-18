using System;
using System.Linq;
using Utility;
using Utility.IO;

namespace ZipUtility.ZipFileHeader
{
    class ZipFileEOCDR
    {
        private static readonly ReadOnlyMemory<byte> _eocdSignature;

        private readonly UInt16 _numberOfCentralDirectoryRecordsOnThisDisk;

        static ZipFileEOCDR()
        {
            _eocdSignature = new byte[] { 0x50, 0x4b, 0x05, 0x06 }.AsReadOnly();
        }

        private ZipFileEOCDR(UInt64 offsetOfThisHeader, UInt16 numberOfThisDisk, UInt16 diskWhereCentralDirectoryStarts, UInt16 numberOfCentralDirectoryRecordsOnThisDisk, UInt16 totalNumberOfCentralDirectoryRecords, UInt32 sizeOfCentralDirectory, UInt32 offsetOfStartOfCentralDirectory, ReadOnlyMemory<byte> commentBytes)
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
        /// EOCDRのあるディスクに含まれるセントラルディレクトリレコードの数。
        /// </summary>
        /// <remarks>
        /// PKZIPの実装では、最後のディスクがEOCDRから始まっている場合 (つまり最後のディスクにセントラルディレクトリヘッダが存在しない) でも、このプロパティが1になってしまう。
        /// このプロパティの値をもとにセントラルディレクトリヘッダを探してはならない。
        /// </remarks>
        [Obsolete]
        public UInt16 NumberOfCentralDirectoryRecordsOnThisDisk => _numberOfCentralDirectoryRecordsOnThisDisk;

        public UInt16 TotalNumberOfCentralDirectoryRecords { get; }
        public UInt32 SizeOfCentralDirectory { get; }
        public UInt32 OffsetOfStartOfCentralDirectory { get; }
        public ReadOnlyMemory<byte> CommentBytes { get; }
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
                zipFileLength > minimumLengthOfHeader - (UInt32)_eocdSignature.Length
                ? zipFileLength - minimumLengthOfHeader + (UInt32)_eocdSignature.Length
                : 0;
            if (zipFileLength < offsetLowerLimit + minimumLengthOfHeader)
                throw new BadZipFileFormatException("Too short Zip file");
            while (offsetUpperLimit >= offsetLowerLimit + (UInt32)_eocdSignature.Length)
            {
                var offsetOfThisHeader =
                    zipInputStream.FindLastSigunature(_eocdSignature, offsetLowerLimit, offsetUpperLimit - offsetLowerLimit)
                    ?? throw new BadZipFileFormatException("EOCD Not found in Zip file");
                zipInputStream.Seek(offsetOfThisHeader);
                try
                {
                    var minimumHeader = zipInputStream.ReadBytes(minimumLengthOfHeader);
                    var signature = minimumHeader[.._eocdSignature.Length];
                    if (!signature.Span.SequenceEqual(_eocdSignature.Span))
                        throw new BadZipFileFormatException();
                    var numberOfThisDisk = minimumHeader[4..].ToUInt16LE();
                    var diskWhereCentralDirectoryStarts = minimumHeader[6..].ToUInt16LE();
                    var numberOfCentralDirectoryRecordsOnThisDisk = minimumHeader[8..].ToUInt16LE();
                    var totalNumberOfCentralDirectoryRecords = minimumHeader[10..].ToUInt16LE();
                    var sizeOfCentralDirectory = minimumHeader[12..].ToUInt32LE();
                    var offsetOfStartOfCentralDirectory = minimumHeader[16..].ToUInt32LE();
                    var commentLength = minimumHeader[20..].ToUInt16LE();
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
                offsetUpperLimit = (offsetOfThisHeader + (UInt32)_eocdSignature.Length - 1U).Maximum(0U);
            }
            throw new BadZipFileFormatException("EOCD Not found in Zip file");
        }
    }
}

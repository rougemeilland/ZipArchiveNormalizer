using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utility;
using ZipUtility.ZipExtraField;

namespace ZipUtility.ZipFileHeader
{
    class ZipEntryCentralDirectoryHeader
        : IZip64ExtendedInformationExtraFieldValueSource
    {
        private static byte[] _centralHeaderSignature;
        private UInt32 _packedSizeValueInCentralDirectory;
        private UInt32 _sizeValueInCentralDirectory;
        private UInt32 _localFileHeaderOffsetValueInCentralDirectory;
        private UInt16 _diskStartNumberValueInCentralDirectory;

        static ZipEntryCentralDirectoryHeader()
        {
            _centralHeaderSignature = new byte[] { 0x50, 0x4b, 0x01, 0x02 };
        }

        private ZipEntryCentralDirectoryHeader(Int64 index, Int64 zipStartOffset, ZipFileEntryGeneralPurposeBitFlag generalPurposeBitFlag, DateTime dosTime, UInt32 packedSizeValueInCentralDirectory, UInt32 sizeValueInCentralDirectory, UInt16 diskStartNumberValueInCentralDirectory, UInt32 localFileHeaderOffsetValueInCentralDirectory, IReadOnlyArray<byte> fullNameBytes, IReadOnlyArray<byte> commentBytes, IReadOnlyArray<byte> extraDataSource)
        {
            Index = index;
            DosTime = dosTime;
            _packedSizeValueInCentralDirectory = packedSizeValueInCentralDirectory;
            _sizeValueInCentralDirectory = sizeValueInCentralDirectory;
            _localFileHeaderOffsetValueInCentralDirectory = localFileHeaderOffsetValueInCentralDirectory;
            _diskStartNumberValueInCentralDirectory = diskStartNumberValueInCentralDirectory;
            FullNameBytes = fullNameBytes;
            CommentBytes = commentBytes;
            ExtraFields = new ExtraFieldStorage(ZipEntryHeaderType.CentralDirectoryHeader, extraDataSource);

            var zip64ExtraData = ExtraFields.GetData<Zip64ExtendedInformationExtraFieldForCentraHeader>();
            if (zip64ExtraData != null)
            {
                zip64ExtraData.Source = this;
                DiskStartNumber = zip64ExtraData.DiskStartNumber;
                LocalFileHeaderOffset = zipStartOffset + zip64ExtraData.RelativeHeaderOffset;
            }
            else
            {
                DiskStartNumber = _diskStartNumberValueInCentralDirectory;
                LocalFileHeaderOffset = zipStartOffset + _localFileHeaderOffsetValueInCentralDirectory;
            }
        }

        public long Index { get; }

        public ZipFileEntryGeneralPurposeBitFlag GeneralPurposeBitFlag { get; }
        public DateTime DosTime { get; }
        public UInt32 DiskStartNumber { get; }
        public long LocalFileHeaderOffset { get; }
        public IReadOnlyArray<byte> FullNameBytes { get; }
        public IReadOnlyArray<byte> CommentBytes { get; }
        public ExtraFieldStorage ExtraFields { get; }

        public static IEnumerable<ZipEntryCentralDirectoryHeader> Enumerate(Stream zipFileBaseStream, Int64 zipStartOffset, Int64 centralHeadersStartOffset, Int64 centralHeadersCount)
        {
            zipFileBaseStream.Seek(centralHeadersStartOffset, SeekOrigin.Begin);
            var centralHeaders = new List<ZipEntryCentralDirectoryHeader>();
            for (var index = 0L; index < centralHeadersCount; index++)
                centralHeaders.Add(Parse(zipFileBaseStream, index, zipStartOffset));
            return centralHeaders;
        }

        private static ZipEntryCentralDirectoryHeader Parse(Stream zipFileBaseStream, Int64 index, Int64 zipStartOffset)
        {
            var minimumLengthOfHeader = 46;
            var minimumHeaderBytes = zipFileBaseStream.ReadBytes(minimumLengthOfHeader);
            var signature = minimumHeaderBytes.GetSequence(0, _centralHeaderSignature.Length);
            if (!signature.SequenceEqual(_centralHeaderSignature))
                throw new BadZipFormatException("Not found central header in expected position");
            //var versionMadeBy = minimumHeaderBytes.ToUInt16(4);
            //var hostSystem = (versionMadeBy >> 8);
            var generalPurposeBitFlag = (ZipFileEntryGeneralPurposeBitFlag)minimumHeaderBytes.ToUInt16(8);
            //var compressionMethod = minimumHeaderBytes.ToUInt16(10);
            var dosTime = minimumHeaderBytes.ToUInt16(12);
            var dosDate = minimumHeaderBytes.ToUInt16(14);
            //var crc = minimumHeaderBytes.ToUInt32(16);
            var packedSize = minimumHeaderBytes.ToUInt32(20);
            var size = minimumHeaderBytes.ToUInt32(24);
            var fileNameLength = minimumHeaderBytes.ToUInt16(28);
            var extraFieldLength = minimumHeaderBytes.ToUInt16(30);
            var commentLength = minimumHeaderBytes.ToUInt16(32);
            var diskStartNumber = minimumHeaderBytes.ToUInt16(34);
            // var externalFileAttribute = (ZipFileEntryExternalFileAttributes)minimumHeaderBytes.ToUInt32(38);
            var relativeLocalFileHeaderOffset = minimumHeaderBytes.ToUInt32(42);

            // ファイルサイズ/圧縮後ファイルサイズ/CRC を真面目に取得しようとすると、(ファイルサイズがわからないまま)ファイルを全部読み取り、
            // その後にある Data Descriptor を読まねばならないことがある。
            // 圧縮元のデータがファイルシステム上のファイルではなくストリームだとそういう圧縮がされることがあるらしい。
            // このプログラムの目的としては、ファイルサイズやCRCを独自に取得することは必須ではないので、それらの情報の取得は断念することにした。
            // ただし、セントラルディレクトリ上のファイルサイズと圧縮後ファイルサイスの値はZip64かどうかの検出に必要となるため、取得はすることにする。

            var fullNameBytes = zipFileBaseStream.ReadBytes(fileNameLength);
            var extraDataSource = zipFileBaseStream.ReadBytes(extraFieldLength);
            var commentBytes = zipFileBaseStream.ReadBytes(commentLength);

            // MS-DOS形式の日時はタイムゾーンが規定されていないが現地時刻とみなして(というよりそう解釈する以外に選択の余地がない)、その後にUTCに変換して使用する。
            var dosDateTime = new[] { dosDate, dosTime }.FromDosDateTimeToDateTime(DateTimeKind.Local).ToUniversalTime();
            return new ZipEntryCentralDirectoryHeader(index, zipStartOffset, generalPurposeBitFlag, dosDateTime, packedSize, size, diskStartNumber, relativeLocalFileHeaderOffset, fullNameBytes, commentBytes, extraDataSource);
        }

        UInt32? IZip64ExtendedInformationExtraFieldValueSource.Size { get => _sizeValueInCentralDirectory; set => _sizeValueInCentralDirectory = value ?? throw new NullReferenceException(@"Do not set ""IZip64ExtendedInformationExtraFieldValueSource.Size"" to null."); }
        UInt32? IZip64ExtendedInformationExtraFieldValueSource.PackedSize { get => _packedSizeValueInCentralDirectory; set => _packedSizeValueInCentralDirectory = value ?? throw new NullReferenceException(@"Do not set ""IZip64ExtendedInformationExtraFieldValueSource.PackedSize"" to null."); }
        UInt32? IZip64ExtendedInformationExtraFieldValueSource.RelativeHeaderOffset { get => _localFileHeaderOffsetValueInCentralDirectory; set => _localFileHeaderOffsetValueInCentralDirectory = value ?? throw new NullReferenceException(@"Do not set ""IZip64ExtendedInformationExtraFieldValueSource.RelativeHeaderOffset"" to null."); }
        UInt16? IZip64ExtendedInformationExtraFieldValueSource.DiskStartNumber { get => _diskStartNumberValueInCentralDirectory; set => _diskStartNumberValueInCentralDirectory = value ?? throw new NullReferenceException(@"Do not set ""IZip64ExtendedInformationExtraFieldValueSource.DiskStartNumber"" to null."); }
    }
}
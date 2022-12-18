using System;
using System.Linq;
using Utility;
using Utility.IO;

namespace ZipUtility.ZipFileHeader
{
    class ZipEntryDataDescriptor
    {
        private static readonly ReadOnlyMemory<byte> _dataDescriptorSignature;

        static ZipEntryDataDescriptor()
        {
            _dataDescriptorSignature = new byte[] { 0x50, 0x4b, 0x07, 0x08 }.AsReadOnly();
        }

        private ZipEntryDataDescriptor(UInt32 crc, UInt64 packedSize, UInt64 size)
        {
            Crc = crc;
            PackedSize = packedSize;
            Size = size;
        }

        public UInt32 Crc { get; }
        public UInt64 PackedSize { get; }
        public UInt64 Size { get; }

        public static ZipEntryDataDescriptor Parse(IZipInputStream zipInputStrem, ZipStreamPosition dataPosition, UInt64 packedSizeValueInCentralDirectoryHeader, UInt64 sizeValueInCentralDirectoryHeader, bool isZip64, ZipEntryCompressionMethodId compressionMethodId, ZipEntryGeneralPurposeBitFlag flag)
        {
            var startPosition = zipInputStrem.Position;
            var (actualCrc, actualSize) =
                compressionMethodId
                    .GetCompressionMethod(flag)
                    .CalculateCrc32(zipInputStrem, dataPosition, sizeValueInCentralDirectoryHeader, packedSizeValueInCentralDirectoryHeader, null);
            var actualPackedSize = zipInputStrem.Position - startPosition;
            //zipFileInputStrem.Seek(dataOffset + packedSizeValueInCentralDirectoryHeader, RandomByteStreamSeekType.FromStart);
            var sourceData = zipInputStrem.ReadBytes(isZip64 ? 24 : 16);
            var foundDataDescriptor =
                new Func<ZipEntryDataDescriptor?>[]
                {
                    () => Create(sourceData, true, isZip64),
                    () => Create(sourceData, false, isZip64),
                }
                .Select(creater => creater())
                .Where(dataDescriptor =>
                    dataDescriptor is not null &&
                    dataDescriptor.Check(actualCrc, packedSizeValueInCentralDirectoryHeader, sizeValueInCentralDirectoryHeader) &&
                    dataDescriptor.PackedSize == actualPackedSize &&
                    dataDescriptor.Size == actualSize)
                .FirstOrDefault();
            if (foundDataDescriptor is null)
                throw new BadZipFileFormatException("Not found data descriptor.");
            return foundDataDescriptor;
        }

        private static ZipEntryDataDescriptor? Create(ReadOnlyMemory<byte> source, bool containsSignature, bool isZip64)
        {
            if (containsSignature)
            {
                if (isZip64)
                {
                    var signature = source[.._dataDescriptorSignature.Length];
                    if (!signature.Span.SequenceEqual(_dataDescriptorSignature.Span))
                        return null;
                    var crc = source[4..].ToUInt32LE();
                    var packedSize = source[8..].ToUInt64LE();
                    var size = source[16..].ToUInt64LE();
                    return new ZipEntryDataDescriptor(crc, packedSize, size);
                }
                else
                {
                    var signature = source[.._dataDescriptorSignature.Length];
                    if (!signature.Span.SequenceEqual(_dataDescriptorSignature.Span))
                        return null;
                    var crc = source[4..].ToUInt32LE();
                    var packedSize = source[8..].ToUInt32LE();
                    var size = source[12..].ToUInt32LE();
                    return new ZipEntryDataDescriptor(crc, packedSize, size);
                }
            }
            else
            {
                if (isZip64)
                {
                    var crc = source.ToUInt32LE();
                    var packedSize = source[4..].ToUInt64LE();
                    var size = source[12..].ToUInt64LE();
                    return new ZipEntryDataDescriptor(crc, packedSize, size);
                }
                else
                {
                    var crc = source.ToUInt32LE();
                    var packedSize = source[4..].ToUInt32LE();
                    var size = source[8..].ToUInt32LE();
                    return new ZipEntryDataDescriptor(crc, packedSize, size);
                }
            }
        }

        private bool Check(UInt32 crc, UInt64 packedSize, UInt64 size)
        {
            return
                Crc == crc &&
                PackedSize == packedSize &&
                Size == size;
        }

    }
}

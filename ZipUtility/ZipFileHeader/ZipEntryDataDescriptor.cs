using System;
using System.Linq;
using Utility;
using Utility.IO;

namespace ZipUtility.ZipFileHeader
{
    class ZipEntryDataDescriptor
    {
        private static byte[] _dataDescriptorSignature;

        static ZipEntryDataDescriptor()
        {
            _dataDescriptorSignature = new byte[] { 0x50, 0x4b, 0x07, 0x08 };
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
            var actualSize = 0UL;
            var actualCrc =
                compressionMethodId
                    .GetCompressionMethod(flag)
                    .GetInputStream(
                        zipInputStrem
                            .AsPartial(dataPosition, packedSizeValueInCentralDirectoryHeader),
                        sizeValueInCentralDirectoryHeader)
                .GetByteSequence(false)
                .CalculateCrc32(out actualSize);
            var actualPackedSize = zipInputStrem.Position - startPosition;
            //zipFileInputStrem.Seek(dataOffset + packedSizeValueInCentralDirectoryHeader, RandomByteStreamSeekType.FromStart);
            var sourceData = zipInputStrem.ReadBytes(isZip64 ? 24 : 16);
            var foundDataDescriptor =
                new Func<ZipEntryDataDescriptor>[]
                {
                    () => Create(sourceData, true, isZip64),
                    () => Create(sourceData, false, isZip64),
                }
                .Select(creater => creater())
                .Where(dataDescriptor =>
                    dataDescriptor != null &&
                    dataDescriptor.Check(actualCrc, packedSizeValueInCentralDirectoryHeader, sizeValueInCentralDirectoryHeader) &&
                    dataDescriptor.PackedSize == actualPackedSize &&
                    dataDescriptor.Size == actualSize)
                .FirstOrDefault();
            if (foundDataDescriptor == null)
                throw new BadZipFileFormatException("Not found data descriptor.");
            return foundDataDescriptor;
        }

        private static ZipEntryDataDescriptor Create(IReadOnlyArray<byte> source, bool containsSignature, bool isZip64)
        {
            if (containsSignature)
            {
                if (isZip64)
                {
                    var signature = source.GetSequence(0, _dataDescriptorSignature.Length);
                    if (!signature.SequenceEqual(_dataDescriptorSignature))
                        return null;
                    var crc = source.ToUInt32LE(4);
                    var packedSize = source.ToUInt64LE(8);
                    var size = source.ToUInt64LE(16);
                    return new ZipEntryDataDescriptor(crc, packedSize, size);
                }
                else
                {
                    var signature = source.GetSequence(0, _dataDescriptorSignature.Length);
                    if (!signature.SequenceEqual(_dataDescriptorSignature))
                        return null;
                    var crc = source.ToUInt32LE(4);
                    var packedSize = source.ToUInt32LE(8);
                    var size = source.ToUInt32LE(12);
                    return new ZipEntryDataDescriptor(crc, packedSize, size);
                }
            }
            else
            {
                if (isZip64)
                {
                    var crc = source.ToUInt32LE(0);
                    var packedSize = source.ToUInt64LE(4);
                    var size = source.ToUInt64LE(12);
                    return new ZipEntryDataDescriptor(crc, packedSize, size);
                }
                else
                {
                    var crc = source.ToUInt32LE(0);
                    var packedSize = source.ToUInt32LE(4);
                    var size = source.ToUInt32LE(8);
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
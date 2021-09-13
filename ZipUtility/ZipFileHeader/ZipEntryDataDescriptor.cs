using System;
using System.IO;
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

        private ZipEntryDataDescriptor(UInt32 crc, Int64 packedSize, Int64 size)
        {
            Crc = crc;
            PackedSize = packedSize;
            Size = size;
        }

        public UInt32 Crc { get; }
        public Int64 PackedSize { get; }
        public Int64 Size { get; }

        public static ZipEntryDataDescriptor Parse(Stream zipFileInputStrem, long dataOffset, long packedSizeValueInCentralDirectoryHeader, long sizeValueInCentralDirectoryHeader, bool isZip64, ZipEntryCompressionMethodId compressionMethodId, ZipEntryGeneralPurposeBitFlag flag)
        {
#if false
            // このチェック方法は zipFileInputStrem がバッファリングされていると誤った結果を得る可能性があるので取りやめる
            var startPosition = zipFileInputStrem.Position;
#endif
            var actualSize = 0L;
            var actualCrc =
                compressionMethodId.GetCompressionMethod(flag).GetInputStream(zipFileInputStrem, dataOffset, packedSizeValueInCentralDirectoryHeader, sizeValueInCentralDirectoryHeader, true)
                .GetByteSequence(false)
                .Select(b =>
                {
                    ++actualSize;
                    return b;
                })
                .CalculateCrc32();
#if false
            // このチェック方法は zipFileInputStrem がバッファリングされていると誤った結果を得る可能性があるので取りやめる
            var actualPackedSize = zipFileInputStrem.Position - startPosition;

#endif
            // この前のデータの読み込みに使用しているストリームの実装によっては、この時点での zipFileInputStrem.Position が
            // データディスクリプタの開始位置を過ぎてしまっている可能性があるので、明示的に Seek しなければならない。
            zipFileInputStrem.Seek(dataOffset + packedSizeValueInCentralDirectoryHeader, SeekOrigin.Begin);

            var sourceData = zipFileInputStrem.ReadBytes(isZip64 ? 24 : 16);
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
#if false
            // このチェック方法は zipFileInputStrem がバッファリングされていると誤った結果を得る可能性があるので取りやめる
                    dataDescriptor.PackedSize == actualPackedSize &&
#endif
                    dataDescriptor.Size == actualSize )
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
                    var packedSize = source.ToInt64LE(8);
                    var size = source.ToInt64LE(16);
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
                    var packedSize = source.ToInt64LE(4);
                    var size = source.ToInt64LE(12);
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

        private bool Check(UInt32 crc, Int64 packedSize, Int64 size)
        {
            return
                Crc == crc &&
                PackedSize == packedSize &&
                Size == size;
        }

    }
}
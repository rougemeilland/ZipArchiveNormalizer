using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Linq;
using Utility;

namespace ZipUtility.ZipFileHeader
{
    class ZipEntryDataDescriptor
    {
        private static byte[] _dataDescriptorSignature;

        static ZipEntryDataDescriptor()
        {
            _dataDescriptorSignature = new byte[] { 0x50, 0x4b, 0x07, 0x08 };
        }

        private ZipEntryDataDescriptor(UInt32 crc, UInt32 packedSize, UInt32 size)
        {
            Crc = crc;
            PackedSize = packedSize;
            Size = size;
        }

        public UInt32 Crc { get; }
        public UInt32 PackedSize { get; }
        public UInt32 Size { get; }


        public static ZipEntryDataDescriptor Parse(Stream zipFileInputStrem, long entryIndex)
        {
            using (var zipFileObject = new ZipFile(zipFileInputStrem, true))
            {
                var actualCrc = zipFileObject.GetInputStream(entryIndex).GetByteSequence().CalculateCrc32();
                var headerBytes = zipFileInputStrem.ReadBytes(16);
                var signature = headerBytes.GetSequence(0, _dataDescriptorSignature.Length);
                if (!signature.SequenceEqual(_dataDescriptorSignature))
                    throw new BadZipFormatException("Not found data descriptor.");
                var crc = headerBytes.ToUInt32(4);
                if (crc != actualCrc)
                    throw new BadZipFormatException("CRC does not match.");
                var packedSize = headerBytes.ToUInt32(8);
                var size = headerBytes.ToUInt32(12);
                return new ZipEntryDataDescriptor(crc, packedSize, size);
            }
        }
    }
}
using System;
using ZipUtility.Helper;

namespace ZipUtility.ZipExtraField
{
    class Zip64ExtendedInformationExtraFieldForCentraHeader
        : Zip64ExtendedInformationExtraField
    {
        public Zip64ExtendedInformationExtraFieldForCentraHeader()
            : base(ZipEntryHeaderType.CentralDirectoryHeader)
        {
        }

        public UInt64 Size { get => InternalSize; set => InternalSize = value; }
        public UInt64 PackedSize { get => InternalPackedSize; set => InternalPackedSize = value; }
        public UInt64 RelativeHeaderOffset { get => InternalRelativeHeaderOffset; set => InternalRelativeHeaderOffset = value; }
        public UInt32 DiskStartNumber { get => InternalDiskStartNumber; set => InternalDiskStartNumber = value; }

        protected override void GetData(ByteArrayOutputStream writer)
        {
            if (ZipHeaderSource.Size == UInt32.MaxValue)
                writer.WriteUInt64LE(InternalPackedSize);
            if (ZipHeaderSource.PackedSize == UInt32.MaxValue)
                writer.WriteUInt64LE(InternalPackedSize);
            if (ZipHeaderSource.RelativeHeaderOffset == UInt32.MaxValue)
                writer.WriteUInt64LE(InternalRelativeHeaderOffset);
            if (ZipHeaderSource.DiskStartNumber == UInt16.MaxValue)
                writer.WriteUInt32LE(InternalDiskStartNumber);
        }

        protected override void SetData(ByteArrayInputStream reader, UInt16 totalCount)
        {
            InternalSize = ZipHeaderSource.Size == UInt32.MaxValue ? reader.ReadUInt64LE() : ZipHeaderSource.Size;
            InternalPackedSize = ZipHeaderSource.PackedSize == UInt32.MaxValue ? reader.ReadUInt64LE() : ZipHeaderSource.PackedSize;
            InternalRelativeHeaderOffset = ZipHeaderSource.RelativeHeaderOffset == UInt32.MaxValue ? reader.ReadUInt64LE() : ZipHeaderSource.RelativeHeaderOffset;
            InternalDiskStartNumber = ZipHeaderSource.DiskStartNumber == UInt16.MaxValue ? reader.ReadUInt32LE() : ZipHeaderSource.DiskStartNumber;
        }
    }
}
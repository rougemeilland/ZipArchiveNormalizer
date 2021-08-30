using System;

namespace ZipUtility.ZipExtraField
{
    class Zip64ExtendedInformationExtraFieldForCentraHeader
        : Zip64ExtendedInformationExtraField
    {
        public Zip64ExtendedInformationExtraFieldForCentraHeader()
            : base(ZipEntryHeaderType.CentralDirectoryHeader)
        {
        }

        public Int64 Size { get => InternalSize; set => InternalSize = value; }
        public Int64 PackedSize { get => InternalPackedSize; set => InternalPackedSize = value; }
        public Int64 RelativeHeaderOffset { get => InternalRelativeHeaderOffset; set => InternalRelativeHeaderOffset = value; }
        public UInt32 DiskStartNumber { get => InternalDiskStartNumber; set => InternalDiskStartNumber = value; }
    }
}
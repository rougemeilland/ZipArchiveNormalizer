using System;

namespace ZipUtility.ZipExtraField
{
    class Zip64ExtendedInformationExtraFieldForLocalHeader
        : Zip64ExtendedInformationExtraField
    {
        public Zip64ExtendedInformationExtraFieldForLocalHeader()
            : base(ZipEntryHeaderType.LocalFileHeader)
        {
        }

        public Int64 Size { get => InternalSize; set => InternalSize = value; }
        public Int64 PackedSize { get => InternalPackedSize; set => InternalPackedSize = value; }
    }
}
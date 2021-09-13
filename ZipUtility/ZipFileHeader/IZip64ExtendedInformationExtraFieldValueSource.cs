using System;

namespace ZipUtility.ZipFileHeader
{
    interface IZip64ExtendedInformationExtraFieldValueSource
    {
        UInt32 Size { get; set; }
        UInt32 PackedSize { get; set; }
        UInt32 RelativeHeaderOffset { get; set; }
        UInt16 DiskStartNumber { get; set; }
    }
}
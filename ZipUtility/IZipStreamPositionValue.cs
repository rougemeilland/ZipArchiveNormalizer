using System;

namespace ZipUtility
{
    public interface IZipStreamPositionValue
    {
        UInt32 DiskNumber { get; }
        UInt64 OffsetOnTheDisk { get; }
    }
}

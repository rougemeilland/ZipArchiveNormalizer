using System;

namespace ZipUtility
{
    public enum ZipEntryCompressionMethodId
        : UInt16
    {
        Stored = 0,
        Deflate = 8,
        Deflate64 = 9,
        BZIP2 = 12,
        LZMA = 14,
        PPMd = 98,
        Unknown = 0xffff,
    }
}
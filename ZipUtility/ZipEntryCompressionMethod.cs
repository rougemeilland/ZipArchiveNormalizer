using System;

namespace ZipUtility
{
    public enum ZipEntryCompressionMethod
        : UInt16
    {
		Stored = 0,
		Deflated = 8,
    }
}
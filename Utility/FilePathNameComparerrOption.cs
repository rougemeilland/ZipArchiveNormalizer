using System;

namespace Utility
{
    [Flags]
    public enum FilePathNameComparerrOption
        : UInt32
    {
        None = 0,
        IgnoreCase = 1 << 0,
        ConsiderDigitSequenceOfsAsNumber = 1 << 1,
        ConsiderPathNameDelimiter = 1 << 2,
        ConsiderContentFile = 1 << 3,
    }
}
using System;

namespace Utility
{
    [Flags]
    public enum FilePathNameComparerrOption
    {
        None = 0x0000,
        ConsiderSequenceOfDigitsAsNumber = 0x0001,
        ContainsContentFile = 0x0002,
    }
}
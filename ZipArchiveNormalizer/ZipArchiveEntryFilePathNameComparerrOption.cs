using System;

namespace ZipArchiveNormalizer
{
    [Flags]
    enum ZipArchiveEntryFilePathNameComparerrOption
    {
        None = 0,
        ConsiderSequenceOfDigitsAsNumber = 1,
    }
}

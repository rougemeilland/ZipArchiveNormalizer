using System;
using System.Collections.Generic;
using System.Globalization;

namespace ZipArchiveNormalizer
{
    class ZipArchiveFilePathEqualityComparer
        : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return string.Equals(x, y, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return obj.ToLower(CultureInfo.InvariantCulture).GetHashCode();
        }
    }
}

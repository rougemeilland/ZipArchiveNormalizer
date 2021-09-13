using System.Collections.Generic;
using System.Linq;
using ZipUtility;

namespace ZipArchiveNormalizer.Phase1
{
    class ModifiedEntry
    {
        public ModifiedEntry(IEnumerable<string> directoryPathElements, ZipArchiveEntry entry)
        {
            SourceEntry = entry;
            NewEntryFullName = string.Join("/", directoryPathElements);
        }

        public ZipArchiveEntry SourceEntry { get; }
        public string NewEntryFullName { get; }
    }
}
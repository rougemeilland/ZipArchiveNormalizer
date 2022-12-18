using System;
using System.Collections.Generic;
using ZipUtility;

namespace ZipArchiveNormalizer.Phase1
{
    class ModifiedEntry
    {
        private readonly ZipArchiveEntry? _sourceEntry;

        public ModifiedEntry(IEnumerable<string> directoryPathElements, ZipArchiveEntry? entry)
        {
            _sourceEntry = entry;
            NewEntryFullName = string.Join("/", directoryPathElements);
        }

        public bool ExistsSourceEntry => _sourceEntry is not null;
        public ZipArchiveEntry SourceEntry => _sourceEntry ?? throw new InvalidOperationException();
        public string NewEntryFullName { get; }
    }
}
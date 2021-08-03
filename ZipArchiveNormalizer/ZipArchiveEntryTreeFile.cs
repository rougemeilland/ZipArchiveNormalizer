namespace ZipArchiveNormalizer
{
    class ZipArchiveEntryTreeFile
        : ZipArchiveEntryTreeNode
    {
        public ZipArchiveEntryTreeFile(string name, ZipArchiveEntry entry)
            : base(name)
        {
            Entry = entry;
        }

        public ZipArchiveEntry Entry { get; }
    }
}

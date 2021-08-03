using System;

namespace ZipArchiveNormalizer
{
    abstract class ZipArchiveEntryTreeNode
    {
        protected ZipArchiveEntryTreeNode(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public bool Rename(string newName)
        {
            var changed = !string.Equals(Name, newName, StringComparison.InvariantCultureIgnoreCase);
            Name = newName;
            return changed;
        }
    }
}

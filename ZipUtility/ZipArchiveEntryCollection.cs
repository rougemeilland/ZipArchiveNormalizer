using System;
using System.Collections;
using System.Collections.Generic;

namespace ZipUtility
{
    public class ZipArchiveEntryCollection
        : IReadOnlyCollection<ZipArchiveEntry>
    {
        private readonly IDictionary<UInt64, ZipArchiveEntry> _collectionByIndex;
        private readonly IDictionary<string, ZipArchiveEntry> _collectionByFullName;

        internal ZipArchiveEntryCollection(IEnumerable<ZipArchiveEntry> sourceEntryCollection)
        {
            _collectionByIndex = new SortedList<UInt64, ZipArchiveEntry>();
            _collectionByFullName = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var sourceEntry in sourceEntryCollection)
            {
                _collectionByIndex[sourceEntry.Index] = sourceEntry;
                _collectionByFullName[sourceEntry.FullName] = sourceEntry;
            }
        }

        /// <summary>
        /// Gets a <see cref="ZipArchiveEntry"/> object with a matching <see cref="ZipArchiveEntry.Index"/> property.
        /// </summary>
        /// <param name="index">
        /// The <see cref="UInt64"/> value that indicates the value of the <see cref="ZipArchiveEntry.Index"/> property of the <see cref="ZipArchiveEntry"/> object to be retrieved.
        /// </param>
        /// <returns>
        /// <para>
        /// If the corresponding <see cref="ZipArchiveEntry"/> object exists, that object will be returned.
        /// </para>
        /// <para>
        /// If it does not exist, null is returned.
        /// </para>
        /// </returns>
        public ZipArchiveEntry? this[UInt64 index]
        {
            get
            {
                if (!_collectionByIndex.TryGetValue(index, out ZipArchiveEntry entry))
                    return null;
                return entry;
            }
        }

        /// <summary>
        /// Gets a <see cref="ZipArchiveEntry"/> object with a matching <see cref="ZipArchiveEntry.FullName"/> property.
        /// </summary>
        /// <param name="entryName">
        /// The <see cref="UInt64"/> value that indicates the value of the <see cref="ZipArchiveEntry.FullName"/> property of the <see cref="ZipArchiveEntry"/> object to be retrieved.
        /// </param>
        /// <returns>
        /// <para>
        /// If the corresponding <see cref="ZipArchiveEntry"/> object exists, that object will be returned.
        /// </para>
        /// <para>
        /// If it does not exist, null is returned.
        /// </para>
        /// </returns>
        public ZipArchiveEntry? this[string entryName]
        {
            get
            {
                if (!_collectionByFullName.TryGetValue(entryName, out ZipArchiveEntry entry))
                    return null;
                return entry;
            }
        }

        public Int32 Count => _collectionByIndex.Count;
        public IEnumerator<ZipArchiveEntry> GetEnumerator() => _collectionByIndex.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;

namespace ZipUtility
{
    public class ZipArchiveEntryCollection
        : IReadOnlyCollection<ZipArchiveEntry>
    {
        private IDictionary<ulong, ZipArchiveEntry> _collectionByIndex;
        private IDictionary<string, ZipArchiveEntry> _collectionByFullName;

        internal ZipArchiveEntryCollection(IEnumerable<ZipArchiveEntry> sourceEntryCollection)
        {
            _collectionByIndex = new SortedList<ulong, ZipArchiveEntry>();
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
        public ZipArchiveEntry this[ulong index]
        {
            get
            {
                ZipArchiveEntry entry;
                if (!_collectionByIndex.TryGetValue(index, out entry))
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
        public ZipArchiveEntry this[string entryName]
        {
            get
            {
                ZipArchiveEntry entry;
                if (!_collectionByFullName.TryGetValue(entryName, out entry))
                    return null;
                return entry;
            }
        }

        public int Count => _collectionByIndex.Count;
        public IEnumerator<ZipArchiveEntry> GetEnumerator() => _collectionByIndex.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Utility;
using ZipUtility;

namespace ZipArchiveNormalizer.Phase1
{
    class EntryTreeNode
    {
        private IEnumerable<EntryTreeNode> _children;
        private ZipArchiveEntry _entry;

        public EntryTreeNode(string name, IEnumerable<EntryTreeNode> children, ZipArchiveEntry entry)
        {
            Name = name;
            _children = children;
            IsFile = _children.None();
            _entry = entry;
            if (IsFile && _entry == null)
                throw new ArgumentNullException();
        }

        public string Name { get; private set; }

        public bool IsFile { get; }

        public IEnumerable<EntryTreeNode> Children => !IsFile ? _children : throw new InvalidOperationException();
        public ZipArchiveEntry Entry => IsFile ? _entry : throw new InvalidOperationException();

        public bool Rename(string newName)
        {
            var changed = !string.Equals(Name, newName, StringComparison.OrdinalIgnoreCase);
            Name = newName;
            return changed;
        }

        public bool RemoveUselessFileEntry(IEnumerable<string> directoryPathElements, Regex excludedFilePattern, IReporter reporter)
        {
            var updated = false;
            var newChildren = new List<EntryTreeNode>();
            foreach (var child in _children)
            {
                if (child.IsFile)
                {
                    if (excludedFilePattern.IsMatch(child.Name))
                    {
                        reporter.ReportInformationMessage(
                            string.Format(
                                "不要なエントリを削除します。: \"{0}\", ...",
                                string.Join("/", directoryPathElements.Concat(new[] { child.Name }))));
                        updated = true;
                    }
                    else
                        newChildren.Add(child);
                }
                else
                {
                    var result = child.RemoveUselessFileEntry(directoryPathElements.Concat(new[] { child.Name }), excludedFilePattern, reporter);
                    updated |= result;
                    if (result == true && child._children.None())
                    {
                        // child の配下で不要なファイルを削除して、その結果 child の子要素が空になった場合

                        // newChildren に child を登録しない (== child を this._children から削除する)
                    }
                    else
                        newChildren.Add(child);
                }
            }
            _children = newChildren;
            return updated;
        }

        public bool RemoveLeadingUselessDirectoryEntry(IEnumerable<string> directoryPathElements, IReporter reporter)
        {
            var firstElements = _children.Take(2).ToArray();
            if (firstElements.Length != 1 || firstElements[0].IsFile == true)
                return false;

            // この時点で _rootEntries の要素は単一のディレクトリであることが確定
            var singleDirectoryElement = firstElements[0];

            // 先に子要素の短縮を試みる
            var concatinatedDirectoryPathElements =
                directoryPathElements.Concat(new[] { singleDirectoryElement.Name });
            if (!singleDirectoryElement.RemoveLeadingUselessDirectoryEntry(concatinatedDirectoryPathElements, reporter))
            {
                reporter.ReportInformationMessage(
                    string.Format(
                        "無駄なディレクトリエントリを短縮します。: \"{0}\"",
                        string.Join("/", concatinatedDirectoryPathElements)));
            }
            _children = singleDirectoryElement._children.ToList();
            return true;
        }

        public bool RemoveUselessDirectoryEntry(IEnumerable<string> directoryPathElements, IReporter reporter)
        {
            var updated = false;
            foreach (var child in _children.Where(child => child.IsFile == false))
                updated |= child.RemoveUselessDirectoryEntry(directoryPathElements.Concat(new[] { child.Name }), reporter);

            // 子要素がただ一つでありかつそれがディレクトリであるかどうかを調べる
            var firstElements = _children.Take(2).ToArray();
            if (firstElements.Length == 1 && firstElements[0].IsFile == false)
            {
                var singleDirectoryElement = firstElements[0];
                if (updated == false)
                {
                    reporter.ReportInformationMessage(
                        string.Format(
                            "無駄なディレクトリエントリを短縮します。: \"{0}\"",
                            string.Join("/", directoryPathElements.Concat(new[] { singleDirectoryElement.Name, "" }))));
                }
                _children = singleDirectoryElement._children.ToList();
                updated = true;
            }
            return updated;
        }

        public void SortEntries(IComparer<string> entryNameComparer)
        {
            foreach (var child in _children.Where(child => child.IsFile == false))
                child.SortEntries(entryNameComparer);
            _children = _children.OrderBy(child => child.Name, entryNameComparer).ToList();
        }

        public IEnumerable<ModifiedEntry> EnumerateEntry(IEnumerable<string> directoryPathElements)
        {
            if (IsFile == true)
                return new[] { new ModifiedEntry(directoryPathElements.Concat(new[] { Name }), Entry) };
            else
                return _children.SelectMany(child => child.EnumerateEntry(directoryPathElements.Concat(new[] { Name })));
        }

        public override string ToString()
        {
            return string.Format("{{ Name=\"{0}\"}}", Name);
        }
    }
}
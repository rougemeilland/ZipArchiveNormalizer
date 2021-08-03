using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace ZipArchiveNormalizer
{
    class ZipArchiveEntryTreeDirectory
        : ZipArchiveEntryTreeNode
    {
        private static IComparer<string> _sortingEntryComparer;
        private static Regex _digitsPattern;
        private ICollection<ZipArchiveEntryTreeNode> _children;

        static ZipArchiveEntryTreeDirectory()
        {
            _sortingEntryComparer = new ZipArchiveEntryFilePathNameComparer(ZipArchiveEntryFilePathNameComparerrOption.ConsiderSequenceOfDigitsAsNumber);
            _digitsPattern = new Regex(@"^[0-9]+$", RegexOptions.Compiled);
        }

        public ZipArchiveEntryTreeDirectory(string name)
            : base(name)
        {
            _children = new List<ZipArchiveEntryTreeNode>();
        }

        protected IEnumerable<ZipArchiveEntryTreeNode> Children => _children;

        public void AddChild(ZipArchiveEntryTreeNode child)
        {
            _children.Add(child);
        }

        protected bool RemoveUselessFileEntry(Regex excludedFilePattern, string currentPath, Action<string> reporter)
        {
#if DEBUG
            if (!_children.Any())
                throw new Exception("空のディレクトリが存在します。");
#endif
            var updated = false;
            var uselessEntryNames = new List<string>();
            foreach (var child in _children)
            {
                if (child is ZipArchiveEntryTreeDirectory)
                {
                    var childDirectory = (ZipArchiveEntryTreeDirectory)child;
                    updated |= childDirectory.RemoveUselessFileEntry(excludedFilePattern, currentPath == null ? child.Name : currentPath + "/" + child.Name, reporter);
                    if (!childDirectory._children.Any())
                        uselessEntryNames.Add(childDirectory.Name);
                }
                else if (child is ZipArchiveEntryTreeFile)
                {
                    var childFile = (ZipArchiveEntryTreeFile)child;
                    if (excludedFilePattern.IsMatch(childFile.Name))
                        uselessEntryNames.Add(childFile.Name);
                }
                else
                    throw new Exception();
            }
            if (uselessEntryNames.Any())
            {
                reporter(string.Format("不要なエントリが見つかったため削除します。: \"{0}\", ...", currentPath == null ? uselessEntryNames.First() : currentPath + "/" + uselessEntryNames.First()));
                _children = _children.Where(child => !uselessEntryNames.Contains(child.Name)).ToList();
                updated = true;
            }
            return updated;
        }

        protected bool RemoveUselessDirectoryEntry(string currentPath, Action<string> reporter)
        {
#if DEBUG
            if (!_children.Any())
                throw new Exception("空のディレクトリが存在します。");
#endif
            var updated = false;
            foreach (var child in _children)
            {
                if (child is ZipArchiveEntryTreeDirectory)
                    updated |= ((ZipArchiveEntryTreeDirectory)child).RemoveUselessDirectoryEntry(currentPath == null ? child.Name : currentPath + "/" + child.Name, reporter);
            }

            // 子要素がただ一つでありかつそれがディレクトリである場合は、子要素を削除し、子要素の子要素(孫要素)を自分の子要素として移動する
            if (_children.Count == 1)
            {
                var child = _children.Single();
                if (child is ZipArchiveEntryTreeDirectory)
                {
                    var childDirectory = (ZipArchiveEntryTreeDirectory)child;
                    reporter(string.Format("無駄なフォルダエントリが見つかったため省略します。: \"{0}\"", currentPath == null ? childDirectory.Name : currentPath + "/" + childDirectory.Name));
                    _children.Clear();
                    foreach (var newChild in childDirectory._children)
                        AddChild(newChild);
                    updated = true;
                }
            }
            return updated;
        }

        protected bool NormalizePageNumber(string currentPath, Action<string> reporter)
        {
            var updated = false;
            foreach (var child in _children)
            {
                if (child is ZipArchiveEntryTreeDirectory)
                    updated |= ((ZipArchiveEntryTreeDirectory)child).NormalizePageNumber(currentPath == null ? child.Name : currentPath + "/" + child.Name, reporter);
            }
            var prefix = _children.Select(child => child.Name).Aggregate((name1, name2) => 先頭の共通文字列を取得する(name1, name2));
            var suffix = _children.Select(child => child.Name).Aggregate((name1, name2) => 最後の共通文字列を取得する(name1, name2));
#if DEBUG
            if (_children.Count == 1)
            {
                if (prefix != _children.Single().Name)
                    throw new Exception();
                if (suffix != _children.Single().Name)
                    throw new Exception();
            }
#endif
            var minimumNameLength = prefix.Length + 1 + suffix.Length;
            if (_children.All(child => child.Name.Length >= minimumNameLength))
            {
                var midNames = _children.Select(child => child.Name.Substring(prefix.Length, child.Name.Length - prefix.Length - suffix.Length)).ToList();
                var midNameLengths = midNames.Select(name => name.Length);
                if (midNameLengths.Max() != midNameLengths.Min() && midNames.All(midName => _digitsPattern.IsMatch(midName)))
                {
                    var maximumPageNumberDigitCount = midNames.Select(midName => midName.Length).Max();
                    var padding = new string('0', maximumPageNumberDigitCount - 1);
                    foreach (var child in _children)
                    {
#if DEBUG
                        if (!child.Name.StartsWith(prefix))
                            throw new Exception();
                        if (!child.Name.EndsWith(suffix))
                            throw new Exception();
                        if (child.Name.Length <= prefix.Length + suffix.Length)
                            throw new Exception();
#endif
                        var newPageNumberText = padding + child.Name.Substring(prefix.Length, child.Name.Length - prefix.Length - suffix.Length);
                        newPageNumberText = newPageNumberText.Substring(newPageNumberText.Length - maximumPageNumberDigitCount, maximumPageNumberDigitCount);
                        var oldName = child.Name;
                        var renamed = child.Rename(prefix + newPageNumberText + suffix);
                        if (!updated && renamed)
                        {
                            reporter(string.Format("エントリ名のページ番号の桁が揃っていないためエントリ名を変更します。: \"{0}\", ...", currentPath == null ? oldName : currentPath + "/" + oldName));
                            updated = true;
                        }
                    }
                }
            }
            return updated;
        }

        protected void SortEntries()
        {
            foreach (var child in _children)
            {
                if (child is ZipArchiveEntryTreeDirectory)
                    ((ZipArchiveEntryTreeDirectory)child).SortEntries();
            }
            _children = _children.OrderBy(child => child.Name, _sortingEntryComparer).ToList();
        }

        protected void Walk(Action<ZipArchiveEntry, string> action, string path)
        {
            foreach (var child in _children)
            {
                if (child is ZipArchiveEntryTreeFile)
                {
                    var childFile = (ZipArchiveEntryTreeFile)child;
                    if (path == null)
                        action(childFile.Entry, child.Name);
                    else
                        action(childFile.Entry, path + "/" + child.Name);
                }
                else if (child is ZipArchiveEntryTreeDirectory)
                {
                    var childDirectory = (ZipArchiveEntryTreeDirectory)child;
                    if (path == null)
                        childDirectory.Walk(action, child.Name);
                    else
                        childDirectory.Walk(action, path + "/" + child.Name);
                }
                else
                    throw new Exception();
            }
        }

        private static string 先頭の共通文字列を取得する(string s1, string s2)
        {
            if (s1.Length > s2.Length)
            {
                var t = s1;
                s1 = s2;
                s2 = t;
            }
#if DEBUG
            if (s1.Length > s2.Length)
                throw new Exception();
#endif
            var found =
                s1
                .Zip(s2, (c1, c2) => new { c1, c2 })
                .Select((item, index) => new { item.c1, item.c2, index })
                .FirstOrDefault(item => item.c1 != item.c2);
            return found != null ? s1.Substring(0, found.index) : s1;
        }

        private static string 最後の共通文字列を取得する(string s1, string s2)
        {
            if (s1.Length > s2.Length)
            {
                var t = s1;
                s1 = s2;
                s2 = t;
            }
#if DEBUG
            if (s1.Length > s2.Length)
                throw new Exception();
#endif
            var found =
                s1.Reverse()
                .Zip(s2.Reverse(), (c1, c2) => new { c1, c2 })
                .Select((item, index) => new { item.c1, item.c2, index })
                .FirstOrDefault(item => item.c1 != item.c2);
            return found != null ? s1.Substring(s1.Length - found.index, found.index) : s1;
        }
    }
}

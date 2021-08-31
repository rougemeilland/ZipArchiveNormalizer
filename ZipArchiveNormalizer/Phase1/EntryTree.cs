using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Utility;
using ZipUtility;
using ZipUtility.ZipExtraField;

namespace ZipArchiveNormalizer.Phase1
{
    class EntryTree
        : IReporter
    {
        private class EntryTreeWork
        {
            public EntryTreeWork(ZipArchiveEntry entry, IEnumerable<string> nodeNames)
            {
#if DEBUG
                if (nodeNames.None())
                    throw new Exception();
#endif
                Entry = entry;
                NodeNames = nodeNames.ToArray();
                Key = new EntryKey(NodeNames[0], NodeNames.Length > 1);
            }

            public ZipArchiveEntry Entry { get; }
            public string[] NodeNames { get; }
            public EntryKey Key { get; }
        }

        private class EntryKey
            : IEquatable<EntryKey>
        {
            public EntryKey(string name, bool existsChild)
            {
                if (name == null)
                    throw new ArgumentNullException();
                if (name.Length == 0)
                    throw new ArgumentException();
                Name = name;
                ExistsChild = existsChild;
            }

            public string Name { get; }
            public bool ExistsChild { get; }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                return Equals(obj as EntryKey);
            }

            public bool Equals(EntryKey other)
            {
                if (other == null)
                    return false;
                if (!string.Equals(Name, other.Name, StringComparison.InvariantCultureIgnoreCase))
                    return false;
                if (!ExistsChild.Equals(other.ExistsChild))
                    return false;
                return true;
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode() ^ ExistsChild.GetHashCode();
            }
        }

#if false
        private class EntryFileName
        {
            public EntryFileName(IEnumerable<string> entryDirectryNames, string entryFileName, bool isFile, EntryTreeNode entry = null)
            {
                if (entryDirectryNames.None())
                {
                    FullPath = entryFileName;
                    DirectoryPath = null;
                }
                else
                {
                    FullPath = string.Join("/", entryDirectryNames.Concat(new[] { entryFileName }));
                    DirectoryPath = string.Join("/", entryDirectryNames);
                }
                FileName = entryFileName;
                FileNameWithoutExtension = isFile ? Path.GetFileNameWithoutExtension(entryFileName) : entryFileName;
                Extension = isFile ? Path.GetExtension(entryFileName) : "";
                IsFile = isFile;
                Entry = entry;
            }

            public string FullPath { get; }
            public string DirectoryPath { get; }
            public string FileName { get; }
            public string FileNameWithoutExtension { get; }
            public string Extension { get; }
            public bool IsFile { get; }
            public EntryTreeNode Entry { get; }
        }
#endif

#if false
        private class EntryFileNamePair
        {
            public EntryFileNamePair(EntryFileName source, string newFileNameWithoutExtension)
            {
                Source = source;
                Destination =
                    new EntryFileName(
                        source.DirectoryPath != null ? source.DirectoryPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries) : new string[0],
                        newFileNameWithoutExtension + source.Extension,
                        source.IsFile);
            }

            public EntryFileName Source { get; }
            public EntryFileName Destination { get; }
        }
#endif

#if false
        private class PageNumberText
            : IEquatable<PageNumberText>, IComparable<PageNumberText>
        {
            private static Regex _pageNumberFormatPattern;
            private int _pageNumberWidth;
            private BigInteger _pageNumber;
            private string _pageNumberText;
            private string _minorSymbol;
            private BigInteger _otherPageNumber;
            private string _otherPageNumberText;
            private string _otherMinorSymbol;

            static PageNumberText()
            {
                _pageNumberFormatPattern = new Regex(@"^(?<digits>[0-9]+)(?<minorsymbol>[a-z]?)([+\-_](?<otherdigits>[0-9]+)(?<otherminorsymbol>[a-z]?))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            public PageNumberText(string sourceText)
            {
                if (sourceText == null)
                    throw new ArgumentNullException();
                SourceText = sourceText;
                IsOk = false;
                _pageNumberWidth = -1;
                _pageNumber = -1;
                _pageNumberText = null;
                _minorSymbol = null;
                _otherPageNumber = -1;
                _otherPageNumberText = null;
                _otherMinorSymbol = null;
                _otherPageNumber = -1;
                _otherPageNumberText = null;
                HasSecondPart = false;
                var match = _pageNumberFormatPattern.Match(sourceText.Length == 0 ? "0" : sourceText);
                if (match.Success)
                {
                    var rawPageNumberText = match.Groups["digits"].Value;
                    _pageNumber = BigInteger.Parse(match.Groups["digits"].Value, NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat);
                    _pageNumberText = _pageNumber.ToString("D");
                    _pageNumberWidth = _pageNumberText.Length;
                    if (!match.Groups["otherdigits"].Success)
                        IsOk = true;
                    else
                    {
                        var rawOtherPageNumberText = match.Groups["otherdigits"].Value;
                        if (rawPageNumberText.Length == rawOtherPageNumberText.Length)
                        {
                            _otherPageNumber = BigInteger.Parse(match.Groups["otherdigits"].Value, NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat);
                            _otherPageNumberText = _otherPageNumber.ToString("D");
                            if (_pageNumberWidth < _otherPageNumberText.Length)
                                _pageNumberWidth = _otherPageNumberText.Length;
                            HasSecondPart = true;
                            IsOk = true;
                        }
                    }
                    if (match.Groups["minorsymbol"].Success)
                        _minorSymbol = match.Groups["minorsymbol"].Value;
                    if (match.Groups["otherminorsymbol"].Success)
                        _otherMinorSymbol = match.Groups["otherminorsymbol"].Value;
                }
            }

            public string SourceText { get; }
            public bool IsOk { get; }
            public int PageNumberWidth => IsOk ? _pageNumberWidth : throw new InvalidOperationException();
            public bool HasSecondPart { get; }
            public string FirstPageText => IsOk ? _pageNumberText + (_minorSymbol ?? "") : throw new InvalidOperationException();

            public string Format(int newPageNumberWidth)
            {
                if (!IsOk)
                    throw new InvalidOperationException();
                if (newPageNumberWidth < _pageNumberWidth)
                    throw new ArgumentException();
                var sb = new StringBuilder();
                sb.Append(_pageNumber.ToString(string.Format("D{0}", newPageNumberWidth)));
                if (_minorSymbol != null)
                    sb.Append(_minorSymbol);
                if (_otherPageNumber >= 0)
                {
                    sb.Append("-");
                    sb.Append(_otherPageNumber.ToString(string.Format("D{0}", newPageNumberWidth)));
                    if (_otherMinorSymbol != null)
                        sb.Append(_otherMinorSymbol);
                }
                return sb.ToString();
            }

            public int CompareTo(PageNumberText other)
            {
                if (!IsOk)
                    throw new InvalidOperationException();
                if (other == null)
                    return 1;
                if (!other.IsOk)
                    throw new ArgumentException();
                int c;
                if ((c = _pageNumber.CompareTo(other._pageNumber)) != 0)
                    return c;
                if ((c = string.Compare(_minorSymbol, other._minorSymbol, StringComparison.InvariantCultureIgnoreCase)) != 0)
                    return c;
                if ((c = _otherPageNumber.CompareTo(other._otherPageNumber)) != 0)
                    return c;
                if ((c = string.Compare(_otherMinorSymbol, other._otherMinorSymbol, StringComparison.InvariantCultureIgnoreCase)) != 0)
                    return c;
                return 0;
            }

            public bool Equals(PageNumberText other)
            {
                if (!IsOk)
                    throw new InvalidOperationException();
                if (other == null)
                    return false;
                if (!other.IsOk)
                    throw new ArgumentException();
                if (!_pageNumber.Equals(other._pageNumber))
                    return false;
                if (!string.Equals(_minorSymbol, other._minorSymbol, StringComparison.InvariantCultureIgnoreCase))
                    return false;
                if (!_otherPageNumber.Equals(other._otherPageNumber))
                    return false;
                if (!string.Equals(_otherMinorSymbol, other._otherMinorSymbol, StringComparison.InvariantCultureIgnoreCase))
                    return false;
                return true;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                return Equals((PageNumberText)obj);
            }

            public override int GetHashCode()
            {
                if (!IsOk)
                    throw new InvalidOperationException();
                var hashCode = _pageNumber.GetHashCode();
                if (_minorSymbol != null)
                    hashCode ^= _minorSymbol.ToLowerInvariant().GetHashCode();
                if (_otherPageNumber >= 0)
                    hashCode ^= _otherPageNumber.GetHashCode();
                if (_otherMinorSymbol != null)
                    hashCode ^= _otherMinorSymbol.ToLowerInvariant().GetHashCode();
                return hashCode;
            }
        }
#endif

        private enum EntryNameComparerMethod
        {
            Normal,
            ConsiderSequenceOfDigitsAsNumber,
            Ordinal,
        }

        private class SimpleEntryNameComparer
            : EntryNodeComparer
        {
            private IComparer<string> _comparer;
            private EntryNameComparerMethod _method;

            public SimpleEntryNameComparer(bool ignoreFileExtension, Encoding encoding, EntryNameComparerMethod method)
                : base(ignoreFileExtension, encoding)
            {
                switch (method)
                {
                    case EntryNameComparerMethod.Normal:
                        _comparer = StringComparer.CurrentCultureIgnoreCase;
                        break;
                    case EntryNameComparerMethod.ConsiderSequenceOfDigitsAsNumber:
                        _comparer = new FilePathNameComparer(FilePathNameComparerrOption.ConsiderSequenceOfDigitsAsNumber);
                        break;
                    case EntryNameComparerMethod.Ordinal:
                        _comparer = StringComparer.OrdinalIgnoreCase;
                        break;
                    default:
                        throw new Exception();
                }
                _method = method;
            }

            public override IEnumerable<string> Descriptions
            {
                get
                {
                    var descriptions = base.Descriptions;
                    switch (_method)
                    {
                        case EntryNameComparerMethod.Normal:
                            descriptions = descriptions.Concat(new[] { "通常の比較" });
                            break;
                        case EntryNameComparerMethod.ConsiderSequenceOfDigitsAsNumber:
                            descriptions = descriptions.Concat(new[] { "数字列を数値とみなして比較" });
                            break;
                        case EntryNameComparerMethod.Ordinal:
                            descriptions = descriptions.Concat(new[] { "コードポイントで比較" });
                            break;
                        default:
                            throw new Exception();
                    }
                    return descriptions;
                }
            }

            protected override IComparer<string> PathElementComparer => _comparer;
        }

        private interface IEntryNodeComparer
            : IComparer<ZipArchiveEntry>
        {
            IEnumerable<string> Descriptions { get; }
        }

        private abstract class EntryNodeComparer
            : IEntryNodeComparer
        {
            private static Regex _entryFullNameExtensionPattern;
            private bool _ignoreFileExtension;
            private Encoding _encoding;

            static EntryNodeComparer()
            {
                _entryFullNameExtensionPattern = new Regex(@"^(?<entryfullnamewithoutextension>.*?)(?<extension>\.[^\\/\.]*)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
#if DEBUG
                Match m;
                m = _entryFullNameExtensionPattern.Match("xxx");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "xxx" && m.Groups["extension"].Success == false))
                    throw new Exception();
                m = _entryFullNameExtensionPattern.Match("xxx.");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "xxx" && m.Groups["extension"].Success == true && m.Groups["extension"].Value == "."))
                    throw new Exception();
                m = _entryFullNameExtensionPattern.Match("xxx..");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "xxx." && m.Groups["extension"].Success == true && m.Groups["extension"].Value == "."))
                    throw new Exception();
                m = _entryFullNameExtensionPattern.Match("xxx.aa");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "xxx" && m.Groups["extension"].Success == true && m.Groups["extension"].Value == ".aa"))
                    throw new Exception();
                m = _entryFullNameExtensionPattern.Match("xxx.aa");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "xxx" && m.Groups["extension"].Success == true && m.Groups["extension"].Value == ".aa"))
                    throw new Exception();
                m = _entryFullNameExtensionPattern.Match("xxx.aa.bb");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "xxx.aa" && m.Groups["extension"].Success == true && m.Groups["extension"].Value == ".bb"))
                    throw new Exception();
                m = _entryFullNameExtensionPattern.Match("dd/xxx.aa.bb");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "dd/xxx.aa" && m.Groups["extension"].Success == true && m.Groups["extension"].Value == ".bb"))
                    throw new Exception();
#endif
            }

            protected EntryNodeComparer(bool ignoreFileExtension, Encoding encoding)
            {
                _ignoreFileExtension = ignoreFileExtension;
                _encoding = encoding;
            }

            public virtual IEnumerable<string> Descriptions =>
                (_ignoreFileExtension ? new[] { "拡張子を無視して比較" } : new string[0])
                .Concat(new[] { string.Format("{0}で比較", _encoding.EncodingName) });

            public int Compare(ZipArchiveEntry x, ZipArchiveEntry y)
            {
                if (x == null)
                    return y == null ? 0 : -1;
                else if (y == null)
                    return 1;
                else
                {
                    var xName = _encoding.GetString(x.FullNameBytes.ToArray());
                    if (_ignoreFileExtension)
                    {
                        var match = _entryFullNameExtensionPattern.Match(xName);
                        if (match.Success == false)
                            throw new Exception();
                        xName = match.Groups["entryfullnamewithoutextension"].Value;
                    }

                    var yName = _encoding.GetString(y.FullNameBytes.ToArray());
                    if (_ignoreFileExtension)
                    {
                        var match = _entryFullNameExtensionPattern.Match(yName);
                        if (match.Success == false)
                            throw new Exception();
                        yName = match.Groups["entryfullnamewithoutextension"].Value;
                    }

                    return
                        xName.Split('/', '\\')
                        .SequenceCompare(yName.Split('/', '\\'), PathElementComparer);
                }
            }

            protected abstract IComparer<string> PathElementComparer { get; }
        }

        public event EventHandler<FileMessageReportedEventArgs> InformationReported;
        public event EventHandler<FileMessageReportedEventArgs> WarningReported;
        public event EventHandler<FileMessageReportedEventArgs> ErrorReported;
        public event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

        private static Regex _uselessEntryNameForImageCollectionPattern;
        private static Regex _uselessEntryNameForAozoraBunkoPattern;
        private static Regex _uselessEntryNameForContentPattern;
        private static Regex _notedEntryFileNamePattern;
        private static IDictionary<long, object> _notedEntryCrcs;
        private static IComparer<string> _entryNameComparer;
#if false
        private static Regex _prefixTailingZeroPattern;
        private static Regex _generalEntryPageNumberPattern;
#endif
        private static Regex _absolutePathEntryNamePattern;
        private static Regex _entryNamePatternsThatShouldBeIgnored;
        private static Regex _notRootEntryNamePattern;
        private static IEnumerable<IEntryNodeComparer> _variousNodecomparers;
        private static IEnumerable<UInt16> _knownExtraFieldIds;
        private FileInfo _sourceArchiveFile;
        private ArchiveType _archiveType;
        private IEnumerable<ZipArchiveEntry> _sourceEntries;
        private IEnumerable<EntryTreeNode> _rootEntries;

        static EntryTree()
        {
            _uselessEntryNameForImageCollectionPattern = new Regex(Properties.Settings.Default.UselessEntryNameForImageCollectionPatternText, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _uselessEntryNameForAozoraBunkoPattern = new Regex(Properties.Settings.Default.UselessEntryNameForAozoraBunkoPatternText, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _uselessEntryNameForContentPattern = new Regex(Properties.Settings.Default.UselessEntryNameForContentPatternText, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _notedEntryFileNamePattern =
                new Regex(
                    Properties.Settings.Default.NotedEntryNamePatternText,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _notedEntryCrcs =
                Properties.Settings.Default.NotedEntryCrc32List
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s =>
                {
                    long value;
                    if (long.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture.NumberFormat, out value) &&
                        value >= UInt32.MinValue &&
                        value <= UInt32.MaxValue)
                    {
                        return value;
                    }
                    else
                        return -1;
                })
                .Where(crc32 => crc32 >= 0)
                .Distinct()
                .ToDictionary(crc => crc, crc => (object)null);
            _entryNameComparer = new FilePathNameComparer(FilePathNameComparerrOption.ConsiderSequenceOfDigitsAsNumber | FilePathNameComparerrOption.ContainsContentFile);
#if false
            _prefixTailingZeroPattern = new Regex(@"^(?<prefix>.*?)0*$", RegexOptions.Compiled);
            _generalEntryPageNumberPattern = new Regex(@"^(?<prefix>.*?)(?<digits>[0-9]+)[a-z]?$", RegexOptions.Compiled);
#endif
            _absolutePathEntryNamePattern = new Regex(@"^[/\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _entryNamePatternsThatShouldBeIgnored = new Regex(@"([/\\]\.)|(^\.)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _notRootEntryNamePattern = new Regex(@"[/\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _variousNodecomparers = new[]
            {
                new SimpleEntryNameComparer(false, Encoding.UTF8, EntryNameComparerMethod.Normal) as IEntryNodeComparer,
                new SimpleEntryNameComparer(false, Encoding.UTF8, EntryNameComparerMethod.ConsiderSequenceOfDigitsAsNumber),
                new SimpleEntryNameComparer(false, Encoding.UTF8, EntryNameComparerMethod.Ordinal),
                new SimpleEntryNameComparer(false, Encoding.GetEncoding("IBM437"), EntryNameComparerMethod.Normal),
                new SimpleEntryNameComparer(false, Encoding.GetEncoding("IBM437"), EntryNameComparerMethod.ConsiderSequenceOfDigitsAsNumber),
                new SimpleEntryNameComparer(false, Encoding.GetEncoding("IBM437"), EntryNameComparerMethod.Ordinal),
                new SimpleEntryNameComparer(false, Encoding.GetEncoding(0), EntryNameComparerMethod.Normal),
                new SimpleEntryNameComparer(false, Encoding.GetEncoding(0), EntryNameComparerMethod.ConsiderSequenceOfDigitsAsNumber),
                new SimpleEntryNameComparer(false, Encoding.GetEncoding(0), EntryNameComparerMethod.Ordinal),
                new SimpleEntryNameComparer(true, Encoding.UTF8, EntryNameComparerMethod.Normal),
                new SimpleEntryNameComparer(true, Encoding.UTF8, EntryNameComparerMethod.ConsiderSequenceOfDigitsAsNumber),
                new SimpleEntryNameComparer(true, Encoding.UTF8, EntryNameComparerMethod.Ordinal),
                new SimpleEntryNameComparer(true, Encoding.GetEncoding("IBM437"), EntryNameComparerMethod.Normal),
                new SimpleEntryNameComparer(true, Encoding.GetEncoding("IBM437"), EntryNameComparerMethod.ConsiderSequenceOfDigitsAsNumber),
                new SimpleEntryNameComparer(true, Encoding.GetEncoding("IBM437"), EntryNameComparerMethod.Ordinal),
                new SimpleEntryNameComparer(true, Encoding.GetEncoding(0), EntryNameComparerMethod.Normal),
                new SimpleEntryNameComparer(true, Encoding.GetEncoding(0), EntryNameComparerMethod.ConsiderSequenceOfDigitsAsNumber),
                new SimpleEntryNameComparer(true, Encoding.GetEncoding(0), EntryNameComparerMethod.Ordinal),
            };

            var extraFieldInterfaceName = typeof(IExtraField).FullName;
            _knownExtraFieldIds =
                new[]
                {
                    new { assembly = typeof(IExtraField).Assembly, externalAssembly = true },
                    new { assembly = typeof(EntryTree).Assembly, externalAssembly = false },
                }
                .SelectMany(item =>
                    item.assembly.GetTypes()
                    .Where(type =>
                        type.IsClass == true &&
                        (type.IsPublic == true || item.externalAssembly == false) &&
                        type.IsAbstract == false &&
                        type.GetInterface(extraFieldInterfaceName) != null)
                    .Select(type => item.assembly.CreateInstance(type.FullName) as IExtraField)
                    .Select(extrafield => extrafield.ExtraFieldId))
                .ToList();
        }

        private EntryTree(FileInfo sourceArchiveFile, ArchiveType archiveType, IEnumerable<ZipArchiveEntry> sourceEntries, IEnumerable<EntryTreeNode> rootEntries)
        {
            _sourceArchiveFile = sourceArchiveFile;
            _archiveType = archiveType;
            _sourceEntries = sourceEntries.ToList();
            _rootEntries = rootEntries.ToList();
        }

        public static EntryTree GetEntryTree(FileInfo archiveFile)
        {
            var entries =
                archiveFile.EnumerateZipArchiveEntry()
                .Where(entry => entry.IsFile)
                .ToList();

            var archiveType = archiveFile.GetArchiveType(() => entries, entry => entry.FullName);
            if (archiveType == ArchiveType.Unknown)
                return null;
            return
                new EntryTree(
                    archiveFile,
                    archiveType,
                    entries,
                    BuildTree(
                       entries
                       .Select(entry => new EntryTreeWork(entry, entry.FullName.Split('\\', '/')))));
        }

        public bool Normalize()
        {
            var uselessEntryFileNamePattern = GetUselessEntryFilePattern(_archiveType);
            if (uselessEntryFileNamePattern == null)
                throw new Exception("アーカイブファイルのタイプが特定できていません。");

            var unknownExtraFieldInformations =
                _sourceEntries
                    .Select(entry => new
                    {
                        entryName = entry.FullName,
                        unknownExtraFieldIds =
                            entry.ExtraFields.EnumerateExtraFieldIds()
                            .Except(_knownExtraFieldIds)
                            .ToList(),
                    })
                    .SelectMany(item => item.unknownExtraFieldIds.Select(unknownExtraFieldId => new { unknownExtraFieldId, item.entryName }))
                    .GroupBy(item => item.unknownExtraFieldId)
                    .Select(g => new
                    {
                        unknownExtraFieldId = g.Key,
                        entryNames =
                            g.Select(item => item.entryName)
                            .OrderBy(entryName => entryName, StringComparer.CurrentCultureIgnoreCase)
                            .Take(3)
                            .ToList(),
                    })
                    .ToList();
            foreach (var unknownExtraFieldInformation in unknownExtraFieldInformations)
            {
                RaiseWarningReportedEvent(
                    string.Format("未知の拡張フィールドを持ったエントリがあります。: extra field id=0x{0:x4}, entry names = {{{1}}}",
                    unknownExtraFieldInformation.unknownExtraFieldId,
                    string.Join(
                        ", ",
                        unknownExtraFieldInformation.entryNames
                        .Select(entryName => string.Format("\"{0}\"", entryName)))));
            }

            var needToUpdate = false;

            if (_archiveType == ArchiveType.Content)
            {
                var foundMimeTypeEntry =
                    _sourceEntries.FirstOrDefault(entry => entry.IsFile && entry.FullName == "mimetype");
                if (foundMimeTypeEntry != null)
                {
                    if (!needToUpdate && foundMimeTypeEntry.Offset > 0)
                    {
                        RaiseInformationReportedEvent("mimetype エントリを先頭に移動します。");
                        needToUpdate = true;
                    }
                    if (!needToUpdate && foundMimeTypeEntry.CompressionMethod != ZipEntryCompressionMethod.Stored)
                    {
                        RaiseInformationReportedEvent("mimetype エントリを非圧縮に変更します。");
                        needToUpdate = true;
                    }
                }
            }

            if (!needToUpdate)
            {
                var directoryPathList =
                    _sourceEntries
                    .Where(entry => entry.IsFile == false)
                    .Select(entry =>
                    {
                        var path = entry.FullName.Replace('\\', '/');
                        return path.EndsWith("/") ? path : path + "/";
                    })
                    .ToList();
                var filePathList =
                    _sourceEntries
                    .Where(entry => entry.IsFile == true)
                    .Select(entry => entry.FullName.Replace('\\', '/'))
                    .ToList();
                var emptyDirectories =
                    directoryPathList
                    .Where(directoryPath => filePathList.None(filePath => filePath.StartsWith(directoryPath, StringComparison.InvariantCultureIgnoreCase)));
                if (emptyDirectories.Any())
                {
                    if (!needToUpdate)
                        RaiseInformationReportedEvent(string.Format("空のフォルダエントリを削除します。: \"{0}\", ...", emptyDirectories.First()));
                    needToUpdate = true;
                }
            }

            if (RemoveUselessFileEntry(uselessEntryFileNamePattern))
                needToUpdate = true;

            if (RemoveLeadingUselessDirectoryEntry())
                needToUpdate = true;

            if (_archiveType == ArchiveType.ImageCollection)
            {
                if (RemoveUselessDirectoryEntry())
                    needToUpdate = true;

                WarnAboutConfusingEntryNames(_rootEntries, new string[0]);
                WarnAboutConfusingEntryNameOrder();
                WarnImageUnderDirectory();
            }

            SortEntries();

            if (!needToUpdate)
            {
                if (EnumerateEntry()
                    .NotAll(entry => string.Equals(entry.SourceEntry.FullName, entry.NewEntryFullName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    RaiseInformationReportedEvent(string.Format("エントリのパス名または順序が変更されているのでアーカイブを変更します。"));
                    needToUpdate = true;
                }
            }

            if (!needToUpdate)
            {
                ModifiedEntry previousEntry = null;
                foreach (var currentEntry in EnumerateEntry())
                {
                    if (previousEntry != null &&
                        previousEntry.SourceEntry.Offset > currentEntry.SourceEntry.Offset)
                    {
                        RaiseInformationReportedEvent(string.Format("エントリの順番を最適化します。"));
                        needToUpdate = true;
                        break;
                    }
                    previousEntry = currentEntry;
                }
            }
            if (!needToUpdate)
            {
                var entries =
                    EnumerateEntry()
                        .Select(entry => new
                        {
                            sourceEntryFullNanme = entry.SourceEntry.FullName,
                            encoding = entry.SourceEntry.EntryTextEncoding,
                            destinationEntryFullName = entry.NewEntryFullName,
                            destinationEntryComment = entry.SourceEntry.Comment,
                        })
                        .Select(item => new
                        {
                            item.sourceEntryFullNanme,
                            item.encoding,
                            isConvertableToMinimumCharacterSet =
                                item.destinationEntryFullName.IsConvertableToMinimumCharacterSet() &&
                                item.destinationEntryComment.IsConvertableToMinimumCharacterSet(),
                        });
                foreach (var entry in entries)
                {
                    if (entry.encoding == ZipEntryTextEncoding.LocalEncoding && entry.isConvertableToMinimumCharacterSet == false)
                    {
                        RaiseInformationReportedEvent(
                            string.Format(
                                "エントリ名とコメントをUTF8に変更します。: \"{0}\", ...",
                                entry.sourceEntryFullNanme));
                        needToUpdate = true;
                        break;
                    }
                    else if (entry.encoding == ZipEntryTextEncoding.UTF8Encoding && entry.isConvertableToMinimumCharacterSet == true)
                    {
                        RaiseInformationReportedEvent(
                            string.Format(
                                "エントリ名とコメントを既定の文字コードに変更します。: \"{0}\", ...",
                                entry.sourceEntryFullNanme));
                        needToUpdate = true;
                        break;
                    }
                    else
                    {
                        // NOP
                    }
                }
            }

            if (!needToUpdate)
            {
                var foundCompressableEntry =
                    EnumerateEntry()
                    .Where(entry =>
                        !string.Equals(entry.NewEntryFullName, "mimetype", StringComparison.InvariantCultureIgnoreCase) &&
                        entry.SourceEntry.Size > 0 &&
                        entry.SourceEntry.CompressionMethod == ZipEntryCompressionMethod.Stored)
                    .FirstOrDefault();
                if (foundCompressableEntry != null)
                {
                    RaiseInformationReportedEvent(string.Format("エントリの圧縮を試みます。: \"{0}\"", foundCompressableEntry.NewEntryFullName));
                    needToUpdate = true;
                }
            }

            if (!needToUpdate)
            {
                foreach (var entry in EnumerateEntry())
                {
                    var extendedTimestampExtraField = entry.SourceEntry.ExtraFields.GetData<ExtendedTimestampExtraField>();
                    if (extendedTimestampExtraField == null ||
                        extendedTimestampExtraField.LastWriteTimeUtc == null ||
                        (entry.SourceEntry.LastAccessTimeUtc.HasValue && extendedTimestampExtraField.LastAccessTimeUtc == null) ||
                        (entry.SourceEntry.CreationTimeUtc.HasValue && extendedTimestampExtraField.CreationTimeUtc == null))
                    {
                        // Extended Timestamp extra field が存在しない、あるいは
                        // Extended Timestamp extra field に最終更新日時が存在しない、あるいは
                        // 最終アクセス日時が設定されているが Extended Timestamp extra field には最終アクセス日時が存在しない、あるいは
                        // 作成日時が設定されているが Extended Timestamp extra field には作成日時存在しない場合

                        RaiseInformationReportedEvent(
                            string.Format(
                                "UNIX系ファイルシステムに互換性のある日時属性をエントリに付加します。: \"{0}\"",
                                entry.NewEntryFullName));
                        needToUpdate = true;
                        break;
                    }

                    if (entry.SourceEntry.LastAccessTimeUtc.HasValue &&
                        entry.SourceEntry.CreationTimeUtc.HasValue &&
                        !entry.SourceEntry.ExtraFields.Contains(NtfsExtraField.ExtraFieldId))
                    {
                        // 最終アクセス日時と作成日時が設定されていて、かつ NTFS extra field が存在しない場合
                        RaiseInformationReportedEvent(
                            string.Format(
                                "Windows系ファイルシステムに互換性のある日時属性をエントリに付加します。: \"{0}\"",
                                entry.NewEntryFullName));
                        needToUpdate = true;
                        break;
                    }
                }
            }

            var foundNotedEntry =
                EnumerateEntry()
                .Where(entry =>
                    entry.SourceEntry.IsFile &&
                    (_notedEntryFileNamePattern.IsMatch(Path.GetFileName(entry.NewEntryFullName)) || _notedEntryCrcs.ContainsKey(entry.SourceEntry.Crc)))
                .FirstOrDefault();
            if (foundNotedEntry != null)
            {
                RaiseWarningReportedEvent(
                    string.Format(
                        "注意すべきエントリがアーカイブファイルに含まれています。: \"{0}\", ...",
                        foundNotedEntry.NewEntryFullName));
            }

            return needToUpdate;
        }

        public bool ContainsAbsoluteEntryPathName()
        {
            return
                _rootEntries
                .Any(node => node.Name.Length > 0 && node.Name[0].IsAnyOf('\\', '/'));
        }

        public bool ContainsDuplicateName()
        {
            return ContainsDuplicateName(_rootEntries);
        }

        public bool ContainsEncryptedEntry()
        {
            return _sourceEntries.Any(entry => entry.IsEncrypted);
        }

        public bool IsEmpty => _rootEntries.None();

        public void SaveTo(FileInfo destinationFile)
        {

            var fileTimeStamp = (DateTime?)null;
            using (var sourceZipFile = new ZipFile(_sourceArchiveFile.FullName))
            using (var newZipArchiveFileStream = new FileStream(destinationFile.FullName, FileMode.Create))
            using (var newZipArchiveOutputStream = new ZipOutputStream(newZipArchiveFileStream))
            {
                // zip ファイルコメントの設定
                newZipArchiveOutputStream.SetComment(sourceZipFile.ZipFileComment);

                foreach (var modifiedEntry in EnumerateEntry())
                {
                    // ファイルのコピー
                    try
                    {
                        var newEntry = modifiedEntry.SourceEntry.CreateDesdinationEntry(modifiedEntry.NewEntryFullName);

                        // アーカイブファイルの日付の更新 (全エントリ内で最も新しい更新日付を見つける)
#if DEBUG
                        if (modifiedEntry.SourceEntry.LastWriteTimeUtc.Kind != DateTimeKind.Utc)
                            throw new Exception();
#endif
                        if (fileTimeStamp == null || fileTimeStamp.Value < modifiedEntry.SourceEntry.LastWriteTimeUtc)
                            fileTimeStamp = modifiedEntry.SourceEntry.LastWriteTimeUtc;

                        // アーカイブファイルへの書き込み
                        newZipArchiveOutputStream.PutNextEntry(newEntry);
                        using (var sourceZipArchiveInputStream = sourceZipFile.GetInputStream(modifiedEntry.SourceEntry))
                        {
                            sourceZipArchiveInputStream.CopyTo(newZipArchiveOutputStream);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        RaiseProgressUpdatedEvent();
                    }
                }
            }

            // ターゲット zip ファイルの更新日付を変更する
            if (fileTimeStamp.HasValue)
            {
                try
                {
                    destinationFile.LastWriteTimeUtc = fileTimeStamp.Value;
                }
                catch (Exception)
                {
                    // 対象のファイルが存在するファイルシステムと設定する時刻によって例外が発生することがあるが無視する。
                }
                finally
                {
                    RaiseProgressUpdatedEvent();
                }
            }
        }

        private static IEnumerable<EntryTreeNode> BuildTree(IEnumerable<EntryTreeWork> source)
        {
            return
                source
                    .Where(item => item.Entry.IsFile == true)
                    .GroupBy(item => item.Key)
                    .SelectMany(g =>
                    {
                        if (g.Key.ExistsChild)
                        {
                            return
                                new[]
                                {
                                    new EntryTreeNode(
                                        g.Key.Name,
                                        BuildTree(g.Select(item => new EntryTreeWork(item.Entry, item.NodeNames.Skip(1)))), null)
                                };
                        }
                        else
                        {
                            // 同一名のエントリは1つしかないはずだが、複数検出してしまった場合のために見つかったものは全部処理する
                            return
                                g
                                .Select(item =>
                                    new EntryTreeNode(g.Key.Name, new EntryTreeNode[0], item.Entry));
                        }
                    });
        }

        private static Regex GetUselessEntryFilePattern(ArchiveType archiveType)
        {
            switch (archiveType)
            {
                case ArchiveType.ImageCollection:
                    return _uselessEntryNameForImageCollectionPattern;
                case ArchiveType.AozoraBunko:
                    return _uselessEntryNameForAozoraBunkoPattern;
                case ArchiveType.Content:
                    return _uselessEntryNameForContentPattern;
                default:
                    return null;
            }
        }

        private bool ContainsDuplicateName(IEnumerable<EntryTreeNode> nodes)
        {
            if (nodes
                .GroupBy(node => node.Name, StringComparer.InvariantCultureIgnoreCase)
                .Select(g => g.Count())
                .Any(count => count > 1))
                return true;
            return
                nodes
                .Any(child => child.IsFile == false && ContainsDuplicateName(child.Children));
        }

        private bool RemoveUselessFileEntry(Regex excludedFilePattern)
        {
            var updated = false;
            var newChildren = new List<EntryTreeNode>();
            foreach (var child in _rootEntries)
            {
                if (child.IsFile)
                {
                    if (excludedFilePattern.IsMatch(child.Name))
                    {
                        RaiseInformationReportedEvent(string.Format("不要なエントリを削除します。: \"{0}\", ...", child.Name));
                        updated = true;
                    }
                    else
                        newChildren.Add(child);
                }
                else
                {
                    var result = child.RemoveUselessFileEntry(new[] { child.Name }, excludedFilePattern, this);
                    updated |= result;
                    if (result == true && child.Children.None())
                    {
                        // child の配下で不要なファイルを削除して、その結果 child の子要素が空になった場合

                        // newChildren に child を登録しない (== child を this._rootEntries から削除する)
                    }
                    else
                        newChildren.Add(child);
                }
            }
            _rootEntries = newChildren;
            return updated;
        }

        private bool RemoveLeadingUselessDirectoryEntry()
        {
            var firstElements = _rootEntries.Take(2).ToArray();
            if (firstElements.Length != 1 || firstElements[0].IsFile == true)
                return false;

            // この時点で _rootEntries の要素は単一のディレクトリであることが確定
            var singleDirectoryElement = firstElements[0];
            if (!singleDirectoryElement.RemoveLeadingUselessDirectoryEntry(new string[0], this))
                RaiseInformationReportedEvent(string.Format("無駄なディレクトリエントリを短縮します。: \"{0}\"", singleDirectoryElement.Name));
            _rootEntries = firstElements[0].Children.ToList();
            return true;
        }

        private bool RemoveUselessDirectoryEntry()
        {
            var updated = false;
            foreach (var child in _rootEntries.Where(child => child.IsFile == false))
                updated |= child.RemoveUselessDirectoryEntry(new[] { child.Name }, this);

            // 子要素がただ一つでありかつそれがディレクトリであるかどうかを調べる
            var firstElements = _rootEntries.Take(2).ToArray();
            if (firstElements.Length == 1 && firstElements[0].IsFile == false)
            {
                var singleDirectoryElement = firstElements[0];
                if (updated == false)
                {
                    RaiseInformationReportedEvent(
                        string.Format(
                            "無駄なディレクトリエントリを短縮します。: \"{0}\"",
                            singleDirectoryElement.Name));
                }
                _rootEntries = singleDirectoryElement.Children.ToList();
                updated = true;
            }
            return updated;
        }

        private IEnumerable<ModifiedEntry> EnumerateEntry()
        {
            return _rootEntries.SelectMany(child => child.EnumerateEntry(new string[0]));
        }

        private string GetEntryFullPath(IEnumerable<string> pathElements, EntryTreeNode node, bool appendDelimiterForDirectory)
        {
            return string.Join("/", pathElements.Concat(new[] { node.Name })) + (appendDelimiterForDirectory && node.IsFile == false ? "/" : "");
        }

        private void WarnAboutConfusingEntryNames(IEnumerable<EntryTreeNode> nodes, IEnumerable<string> pathElements)
        {
            foreach (var node in nodes.Where(node => node.IsFile == false))
                WarnAboutConfusingEntryNames(node.Children, pathElements.Concat(new[] { node.Name }));

            var foundDuplicateName =
                nodes
                    .Select(node => new { node, nameWithoutExtension = node.IsFile ? Path.GetFileNameWithoutExtension(node.Name) : node.Name })
                    .GroupBy(item => item.nameWithoutExtension)
                    .Select(g => g.Select(item => item.node).ToList())
                    .Where(item => item.Count > 1)
                    .FirstOrDefault();
            if (foundDuplicateName != null)
            {
                var foundNode1 = foundDuplicateName.First();
                var foundNode2 = foundDuplicateName.Skip(1).First();
                RaiseWarningReportedEvent(
                    string.Format(
                        "似た名前のエントリが見つかりました。: \"{0}\", \"{1}\", ...",
                        GetEntryFullPath(pathElements, foundNode1, true),
                        GetEntryFullPath(pathElements, foundNode2, true)));
            }

            var shortNames =
                nodes
                .Select(node => new { node, nameWithoutExtension = node.IsFile ? Path.GetFileNameWithoutExtension(node.Name) : node.Name })
                .OrderBy(item => item.nameWithoutExtension.Length)
                .ToArray();
            if (shortNames.Length > 0)
            {
                var foundPartialNames =
                    Enumerable.Range(0, shortNames.Length - 1)
                    .Select(index => new { element1 = shortNames[index], element2 = shortNames[index + 1] })
                    .Where(item => item.element2.nameWithoutExtension.StartsWith(item.element1.nameWithoutExtension, StringComparison.InvariantCultureIgnoreCase))
                    .Select(item => new { node1 = item.element1.node, node2 = item.element2.node })
                    .FirstOrDefault();
                if (foundPartialNames != null)
                {
                    RaiseWarningReportedEvent(
                        string.Format(
                            "部分的に一致した名前のエントリが見つかりました。: \"{0}\", \"{1}\", ...",
                            GetEntryFullPath(pathElements, foundPartialNames.node1, true),
                            GetEntryFullPath(pathElements, foundPartialNames.node2, true)));
                }
            }
#if false
            var source = nodes.Select(node => new EntryFileName(pathElements, node.Name, node.IsFile, node)).ToList();
            foreach (var renameFile in new[] { false, true })
            {
                var renamingDetails =
                    SuggestNewEntryFileNames(source.Where(item => item.IsFile == renameFile), renameFile);
                foreach (var renamingDetail in renamingDetails)
                {
                    if (nodes.None(node => string.Equals(node.Name, renamingDetail.Destination.FileName)))
                    {
                        var renamed = renamingDetail.Source.Entry.Rename(renamingDetail.Destination.FileName);
                        if (updated == false && renamed == true)
                        {
                            RaiseInformationReportedEvent(
                                string.Format(
                                    "エントリ名のページ番号の桁を揃えます。: \"{0}\" => \"{1}\", ...",
                                    renamingDetail.Source.FileName,
                                    renamingDetail.Destination.FileName));
                        }
                        updated |= renamed;
                    }
                }
            }
#endif
        }

        private void WarnAboutConfusingEntryNameOrder()
        {
            var entries =
                EnumerateEntry()
                .Select(entry => entry.SourceEntry)
                .ToList();

            var sortedNamesList =
                _variousNodecomparers
                    .Select(comparer => new
                    {
                        comparer,
                        entryNames =
                            entries
                            .Where(entry => _entryNamePatternsThatShouldBeIgnored.IsMatch(entry.FullName) == false)
                            .OrderBy(entry => entry, comparer)
                            .Select(entry => new { entry.Index, entry.FullName })
                            .ToList(),
                    })
                    .ToArray();
            if (sortedNamesList.Length > 0)
            {
                var found =
                    Enumerable.Range(0, sortedNamesList.Length - 1)
                    .Select(index => new { sequence1 = sortedNamesList[index], sequence2 = sortedNamesList[index + 1] })
                    .Where(item => !item.sequence1.entryNames.Select(entry => entry.Index).SequenceEqual(item.sequence2.entryNames.Select(entry => entry.Index)))
                    .Select(item =>
                    {
                        var desctoptionsDifference1 = item.sequence1.comparer.Descriptions.Except(item.sequence2.comparer.Descriptions).ToReadOnlyCollection();
                        var desctoptionsDifference2 = item.sequence2.comparer.Descriptions.Except(item.sequence1.comparer.Descriptions).ToReadOnlyCollection();
                        var commonPartCount =
                            item.sequence1.entryNames
                            .Zip(
                                item.sequence2.entryNames,
                                (entry1, entry2) => new { entry1, entry2 })
                            .TakeWhile(item2 => item2.entry1.Index == item2.entry2.Index)
                            .Count();
                        var samplePartOffset = commonPartCount - 2;
                        var samplePartCount = 5;
                        if (samplePartOffset < 0)
                        {
                            samplePartCount += samplePartOffset;
                            samplePartOffset = 0;
                        }
                        return new
                        {
                            description1 = string.Join(";", item.sequence1.comparer.Descriptions),
                            description2 = string.Join(";", item.sequence2.comparer.Descriptions),
                            differenceCount = desctoptionsDifference1.Count + desctoptionsDifference2.Count,
                            descriptionCount = item.sequence1.comparer.Descriptions.Count() + item.sequence2.comparer.Descriptions.Count(),
                            sampleSequence1 = item.sequence1.entryNames.Skip(samplePartOffset).Take(samplePartCount).ToArray(),
                            sampleSequence2 = item.sequence2.entryNames.Skip(samplePartOffset).Take(samplePartCount).ToArray(),
                        };
                    })
                    .OrderBy(item => item.differenceCount)
                    .ThenBy(item => item.descriptionCount)
                    .FirstOrDefault();
                if (found != null)
                {
                    RaiseWarningReportedEvent(
                        string.Format(
                            "エントリの表示順序が実行環境によって異なる可能性があります。: 例: 環境1=\"{0}\", 名前1={{{1}}}, 環境2=\"{2}\", 名前2={{{3}}}",
                            found.description1,
                            string.Join(",", found.sampleSequence1.Select(entry => string.Format("\"{0}\"", entry.FullName))),
                            found.description2,
                            string.Join(",", found.sampleSequence2.Select(entry => string.Format("\"{0}\"", entry.FullName)))));
                }
            }

        }

#if false
        private IEnumerable<EntryFileNamePair> SuggestNewEntryFileNames(IEnumerable<EntryFileName> entryFileNames, bool renameFile)
        {
#if DEBUG
            if (entryFileNames.Any(item => item.IsFile != renameFile))
                throw new Exception();
#endif
            var prefix =
                entryFileNames
                .Select(item => item.FileNameWithoutExtension)
                .Aggregate((string)null, (name1, name2) => name1.GetLeadingCommonPart(name2));
            var suffix =
                entryFileNames
                .Select(item => item.FileNameWithoutExtension)
                .Aggregate((string)null, (name1, name2) => name1.GetTrailingCommonPart(name2));
            if (prefix == null ||
                suffix == null ||
                entryFileNames.Any(item => item.FileNameWithoutExtension.Length < prefix.Length + suffix.Length))
                return new EntryFileNamePair[0];
            var matchPrefix = _prefixTailingZeroPattern.Match(prefix);
            if (!matchPrefix.Success)
                throw new Exception();
            prefix = matchPrefix.Groups["prefix"].Value;

            var pageNumbers =
                entryFileNames
                .Select(source => new
                {
                    source,
                    pageNumber =
                        new PageNumberText(
                            source.FileNameWithoutExtension
                            .Substring(
                                prefix.Length,
                                source.FileNameWithoutExtension.Length - prefix.Length - suffix.Length)),
                })
                .ToList();

            if (pageNumbers.All(item => item.pageNumber.IsOk))
            {
                // dddd-dddd (dは数字) の解釈が正しいかチェックする
                // ページ番号が dddd-dddd 形式であった場合、それがページの範囲を示すものなら前半部分が重複するエントリは存在しないはず
                var foundDuplicatePageRange =
                    pageNumbers
                    //.Where(item => item.pageNumber.HasSecondPart) // 例: "0000" と "0000-1" の同時存在時も警告を発したいため、このチェックはしない
                    .GroupBy(item => item.pageNumber.FirstPageText)
                    .Select(g => new { firstPart = g.Key, pageItems = g.ToList() })
                    .Where(item => item.pageItems.Count > 1)
                    .ToList();
                if (foundDuplicatePageRange.Any())
                {
                    // ページ番号が dddd-dddd (dは数字) の形式で、かつ前半部分が同じエントリが複数ある場合
                    // dddd-dddd (dは数字) をページ範囲として解釈していいか確証が持てないので、ユーザに警告を発する
                    RaiseWarningReportedEvent(
                        string.Format(
                            "エントリ名のページ番号の桁を揃えようとしましたが、ページ番号の形式として不明なエントリ名が見つかったため、桁揃えを中断しました。: \"{0}\", \"{1}\", ...",
                            foundDuplicatePageRange.OrderBy(item => item.firstPart).First().pageItems.OrderBy(item => item.pageNumber).First().source.FullPath,
                            foundDuplicatePageRange.OrderBy(item => item.firstPart).First().pageItems.OrderByDescending(item => item.pageNumber).First().source.FullPath));
                    RaiseManualVerificationProposedEvent();
                    return new EntryFileNamePair[0];
                }
                else
                {
                    // 全てのエントリのページ番号を取得できた場合
                    if (pageNumbers.Any(item => item.pageNumber.PageNumberWidth >= 4))
                    {
                        // ページ数が4桁以上のエントリがあった場合
                        RaiseWarningReportedEvent(
                            string.Format(
                                "エントリ名のページ番号の桁を揃えようとしましたが、ページ番号と思われる数値の桁が長すぎます。単純なページ番号ではない可能性もあるので桁揃えを中断しました。: \"{0}\", \"{1}\", ...",
                                pageNumbers.OrderBy(item => item.pageNumber.PageNumberWidth).ThenBy(item => item.pageNumber).First().source.FullPath,
                                pageNumbers.OrderByDescending(item => item.pageNumber.PageNumberWidth).ThenByDescending(item => item.pageNumber).First().source.FullPath));
                        RaiseManualVerificationProposedEvent();
                        return new EntryFileNamePair[0];
                    }
                    else
                    {
                        // すべてのエントリのページ数が4桁以下であった場合

                        // 最大の桁数のページ番号に合わせて全てのページテキストの桁を揃える
                        var maximumPageNumberWidth = pageNumbers.Max(item => item.pageNumber.PageNumberWidth);
                        var formattedPageNumbers =
                            pageNumbers
                            .Select(item => new
                            {
                                item.source,
                                item.pageNumber,
                                newFileName =
                                    item.source.IsFile
                                    ? Properties.Settings.Default.DefaultImageCollectionEntryNamePrefix + item.pageNumber.Format(maximumPageNumberWidth)
                                    : item.pageNumber.Format(maximumPageNumberWidth),
                            })
                            .ToList();

                        // 桁揃えの結果重複するページテキストがないか調べる
                        var foundDuplicateMiddleName =
                            formattedPageNumbers
                            .GroupBy(item => item.newFileName)
                            .Select(g => new { list = g.ToList() })
                            .Where(item => item.list.Count > 1)
                            .FirstOrDefault();
                        if (foundDuplicateMiddleName != null)
                        {
                            // 揃えたページ番号に重複が見つかった場合
                            RaiseWarningReportedEvent(
                                string.Format("エントリ名のページ番号の桁を揃えようとしましたが、その結果エントリ名が重複してしまうため桁揃えを中断しました。: \"{0}\", \"{1}\", ...",
                                    foundDuplicateMiddleName.list.OrderBy(item => item.source.FileName.Length).ThenBy(item => item.source.FileName).First().source.FullPath,
                                    foundDuplicateMiddleName.list.OrderByDescending(item => item.source.FileName.Length).ThenByDescending(item => item.source.FileName).First().source.FullPath));
                            RaiseManualVerificationProposedEvent();
                            return new EntryFileNamePair[0];
                        }
                        else
                        {
                            // 揃えたページ番号に重複が見つからなかった場合
                            // ページ数を揃えた変名案のうち、実際にエントリ名に変更があるものを抽出して返す
                            return
                                formattedPageNumbers
                                .Where(item => !string.Equals(item.source.FileName, item.newFileName))
                                .Select(item => new EntryFileNamePair(item.source, item.newFileName))
                                .ToList();
                        }
                    }
                }
            }
            else
            {
                // middleName に数字以外を含むエントリが1つでも存在した場合
                // 単純なページ番号パターンを適用して、ページ番号の桁数が異なるエントリが存在するか調べる
                var pageNumberLengths =
                    entryFileNames
                    .Select(source => new { source, match = _generalEntryPageNumberPattern.Match(source.FileNameWithoutExtension) })
                    .Where(item => item.match.Success)
                    .Select(item => new { item.source, prefix = item.match.Groups["prefix"].Value, lengthOfDigits = item.match.Groups["digits"].Value.Length })
                    .GroupBy(item => item.prefix)
                    .Select(g =>
                        g
                        .Select(item => new { item.source, item.lengthOfDigits })
                        .GroupBy(item => item.lengthOfDigits)
                        .Select(g2 => new { lengthOfDigits = g2.Key, sources = g2.Select(item => item.source).ToList() })
                        .ToList())
                    .Where(item => item.Count >= 2)
                    .ToList();
                if (pageNumberLengths.Any())
                {
                    // 複数の異なる桁数のページ番号のエントリが見つかった場合
                    RaiseWarningReportedEvent(
                        string.Format("異なる桁数のページ番号のエントリが見つかりました。: \"{0}\", \"{1}\", ...",
                            pageNumberLengths.First().OrderBy(item => item.lengthOfDigits).First().sources.OrderBy(item => item.FileName.Length).ThenBy(item => item.FileName).First().FullPath,
                            pageNumberLengths.First().OrderByDescending(item => item.lengthOfDigits).First().sources.OrderByDescending(item => item.FileName.Length).ThenByDescending(item => item.FileName).First().FullPath));
                    RaiseManualVerificationProposedEvent();
                }
                return new EntryFileNamePair[0];
            }
        }
#endif

        private void WarnImageUnderDirectory()
        {
            var foundNotRootEntry =
                EnumerateEntry()
                .FirstOrDefault(entry =>
                    !_entryNamePatternsThatShouldBeIgnored.IsMatch(entry.NewEntryFullName) &&
                    _notRootEntryNamePattern.IsMatch(entry.NewEntryFullName));
            if (foundNotRootEntry != null)
            {
                RaiseWarningReportedEvent(
                    string.Format(
                        "階層化されたエントリを見つけました。: \"{0}\", ...",
                        foundNotRootEntry.NewEntryFullName));
            }
        }

        private void SortEntries()
        {
            foreach (var child in _rootEntries.Where(child => child.IsFile == false))
                child.SortEntries(_entryNameComparer);
            _rootEntries = _rootEntries.OrderBy(child => child.Name, _entryNameComparer).ToList();
        }

        private void RaiseInformationReportedEvent(string message)
        {
            if (InformationReported != null)
                InformationReported(this, new FileMessageReportedEventArgs(_sourceArchiveFile, message));
        }

        private void RaiseWarningReportedEvent(string message)
        {
            if (WarningReported != null)
                WarningReported(this, new FileMessageReportedEventArgs(_sourceArchiveFile, message));
        }

        private void RaiseErrorReportedEvent(string message)
        {
            if (ErrorReported != null)
                ErrorReported(this, new FileMessageReportedEventArgs(_sourceArchiveFile, message));
        }

        private void RaiseProgressUpdatedEvent()
        {
            if (ProgressUpdated != null)
                ProgressUpdated(this, new ProgressUpdatedEventArgs());
        }

        void IReporter.ReportInformationMessage(string message)
        {
            RaiseInformationReportedEvent(message);
        }

        void IReporter.ReportWarningMessage(string message)
        {
            RaiseWarningReportedEvent(message);
        }
    }
}
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Utility;
using Utility.IO;
using ZipUtility;
using ZipUtility.ZipExtraField;

namespace ZipArchiveNormalizer.Phase1
{
    class EntryTree
        : IReporter
    {
        private class EntryTreeWork
        {
            public EntryTreeWork(ZipArchiveEntry entry, IEnumerable<string?> entryPathElements)
            {
#if DEBUG
                if (entryPathElements.None())
                    throw new Exception();
#endif

                Entry = entry;
                EntryPathElements = entryPathElements.ToReadOnlyCollection();
                var firstPathElement = EntryPathElements.First();
                if (firstPathElement is null)
                    throw new InternalLogicalErrorException();
                Key = new EntryKey(firstPathElement, !EntryPathElements.IsSingle());
            }

            public ZipArchiveEntry Entry { get; }
            public IReadOnlyCollection<string?> EntryPathElements { get; }
            public EntryKey Key { get; }
        }

        private class EntryKey
            : IEquatable<EntryKey>
        {
            public EntryKey(string name, bool existsChild)
            {
                if (name is null)
                    throw new ArgumentNullException(nameof(name));
                if (name.Length == 0)
                    throw new ArgumentException($"{nameof(name)} is empty string", nameof(name));

                Name = name;
                ExistsChild = existsChild;
            }

            public string Name { get; }
            public bool ExistsChild { get; }

            public override bool Equals(object? obj)
            {
                if (obj is null || GetType() != obj.GetType())
                    return false;
                return Equals((EntryKey)obj);
            }

            public bool Equals(EntryKey? other)
            {
                if (other is null)
                    return false;
                if (!string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase))
                    return false;
                if (!ExistsChild.Equals(other.ExistsChild))
                    return false;
                return true;
            }

            public override Int32 GetHashCode()
            {
                return Name.GetHashCode() ^ ExistsChild.GetHashCode();
            }
        }

        private enum ZipArchiveEntryComparerMethod
        {
            Normal,
            ConsiderSequenceOfDigitsAsNumber,
            Ordinal,
        }

        private class SimpleZipArchiveEntryComparer
            : ZipArchiveEntryComparer
        {
            private readonly IComparer<string> _comparer;
            private readonly ZipArchiveEntryComparerMethod _method;

            public SimpleZipArchiveEntryComparer(bool ignoreFileExtension, Encoding encoding, ZipArchiveEntryComparerMethod method)
                : base(ignoreFileExtension, encoding)
            {
                _comparer =
                    method switch
                    {
                        ZipArchiveEntryComparerMethod.Normal => StringComparer.CurrentCultureIgnoreCase,
                        ZipArchiveEntryComparerMethod.ConsiderSequenceOfDigitsAsNumber => new FilePathNameComparer(FilePathNameComparerrOption.ConsiderDigitSequenceOfsAsNumber | FilePathNameComparerrOption.IgnoreCase),
                        ZipArchiveEntryComparerMethod.Ordinal => StringComparer.OrdinalIgnoreCase,
                        _ => throw new InternalLogicalErrorException(),
                    };
                _method = method;
            }

            public override IEnumerable<string> Descriptions
            {
                get
                {
                    var descriptions = base.Descriptions;
                    descriptions =
                        _method switch
                        {
                            ZipArchiveEntryComparerMethod.Normal => descriptions.Concat(new[] { "通常の比較" }),
                            ZipArchiveEntryComparerMethod.ConsiderSequenceOfDigitsAsNumber => descriptions.Concat(new[] { "数字列を数値とみなして比較" }),
                            ZipArchiveEntryComparerMethod.Ordinal => descriptions.Concat(new[] { "コードポイントで比較" }),
                            _ => throw new InternalLogicalErrorException(),
                        };
                    return descriptions;
                }
            }

            protected override IComparer<string> PathElementComparer => _comparer;
        }

        private interface IZipArchiveEntryComparer
            : IComparer<ZipArchiveEntry>
        {
            IEnumerable<string> Descriptions { get; }
        }

        private abstract class ZipArchiveEntryComparer
            : IZipArchiveEntryComparer
        {
            private static readonly Regex _entryFullNameExtensionPattern;

            private readonly bool _ignoreFileExtension;
            private readonly Encoding _encoding;

            static ZipArchiveEntryComparer()
            {
                _entryFullNameExtensionPattern = new Regex(@"^(?<entryfullnamewithoutextension>.*?)(?<extension>\.[^\\/\.]*)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
#if DEBUG
                Match m;
                m = _entryFullNameExtensionPattern.Match("xxx");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "xxx" && !m.Groups["extension"].Success))
                    throw new Exception();
                m = _entryFullNameExtensionPattern.Match("xxx.");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "xxx" && m.Groups["extension"].Success && m.Groups["extension"].Value == "."))
                    throw new Exception();
                m = _entryFullNameExtensionPattern.Match("xxx..");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "xxx." && m.Groups["extension"].Success && m.Groups["extension"].Value == "."))
                    throw new Exception();
                m = _entryFullNameExtensionPattern.Match("xxx.aa");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "xxx" && m.Groups["extension"].Success && m.Groups["extension"].Value == ".aa"))
                    throw new Exception();
                m = _entryFullNameExtensionPattern.Match("xxx.aa");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "xxx" && m.Groups["extension"].Success && m.Groups["extension"].Value == ".aa"))
                    throw new Exception();
                m = _entryFullNameExtensionPattern.Match("xxx.aa.bb");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "xxx.aa" && m.Groups["extension"].Success && m.Groups["extension"].Value == ".bb"))
                    throw new Exception();
                m = _entryFullNameExtensionPattern.Match("dd/xxx.aa.bb");
                if (!(m.Success && m.Groups["entryfullnamewithoutextension"].Value == "dd/xxx.aa" && m.Groups["extension"].Success && m.Groups["extension"].Value == ".bb"))
                    throw new Exception();
#endif
            }

            protected ZipArchiveEntryComparer(bool ignoreFileExtension, Encoding encoding)
            {
                _ignoreFileExtension = ignoreFileExtension;
                _encoding = encoding;
            }

            public virtual IEnumerable<string> Descriptions =>
                (_ignoreFileExtension ? new[] { "拡張子を無視して比較" } : Array.Empty<string>())
                .Concat(new[] { string.Format("{0}で比較", _encoding.EncodingName) });

            public Int32 Compare(ZipArchiveEntry x, ZipArchiveEntry y)
            {
                var xName = _encoding.GetString(x.FullNameBytes);
                if (_ignoreFileExtension)
                {
                    var match = _entryFullNameExtensionPattern.Match(xName);
                    if (!match.Success)
                        throw new InternalLogicalErrorException();
                    xName = match.Groups["entryfullnamewithoutextension"].Value;
                }

                var yName = _encoding.GetString(y.FullNameBytes);
                if (_ignoreFileExtension)
                {
                    var match = _entryFullNameExtensionPattern.Match(yName);
                    if (!match.Success)
                        throw new InternalLogicalErrorException();
                    yName = match.Groups["entryfullnamewithoutextension"].Value;
                }

                return
                    xName.Split('/', '\\')
                    .SequenceCompare(yName.Split('/', '\\'), PathElementComparer);
            }

            protected abstract IComparer<string> PathElementComparer { get; }
        }

        public event EventHandler<FileMessageReportedEventArgs>? InformationReported;
        public event EventHandler<FileMessageReportedEventArgs>? WarningReported;
        public event EventHandler<FileMessageReportedEventArgs>? ErrorReported;
        public event EventHandler<ProgressUpdatedEventArgs>? ProgressUpdated;

        private static readonly Regex _uselessEntryNameForImageCollectionPattern;
        private static readonly Regex _uselessEntryNameForAozoraBunkoPattern;
        private static readonly Regex _uselessEntryNameForContentPattern;
        private static readonly Regex _notedEntryFileNamePattern;
        private static readonly IDictionary<Int64, object> _notedEntryCrcs;
        private static readonly IComparer<string> _entryNameComparer;
        private static readonly Regex _absolutePathEntryNamePattern;
        private static readonly Regex _entryNamePatternsThatShouldBeIgnored;
        private static readonly Regex _notRootEntryNamePattern;
        private static readonly IEnumerable<IZipArchiveEntryComparer> _variousNodecomparers;
        private static readonly IEnumerable<UInt16> _knownExtraFieldIds;

        private readonly FileInfo _sourceArchiveFile;
        private readonly ArchiveType _archiveType;
        private readonly ZipArchiveEntryCollection _sourceEntries;

        private IEnumerable<EntryTreeNode> _rootEntries;

        static EntryTree()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _uselessEntryNameForImageCollectionPattern = new Regex(Settings.Default.UselessEntryNameForImageCollectionPatternText, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _uselessEntryNameForAozoraBunkoPattern = new Regex(Settings.Default.UselessEntryNameForAozoraBunkoPatternText, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _uselessEntryNameForContentPattern = new Regex(Settings.Default.UselessEntryNameForContentPatternText, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _notedEntryFileNamePattern =
                new Regex(
                    Settings.Default.NotedEntryNamePatternText,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _notedEntryCrcs =
                Settings.Default.NotedEntryCrc32List
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s =>
                {
                    if (Int64.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture.NumberFormat, out Int64 value) &&
                        value.IsBetween((Int64)UInt32.MinValue, UInt32.MaxValue))
                    {
                        return value;
                    }
                    else
                        return -1;
                })
                .Where(crc32 => crc32 >= 0)
                .Distinct()
                .ToDictionary(crc => crc, crc => new object());
            _entryNameComparer = new FilePathNameComparer(FilePathNameComparerrOption.ConsiderContentFile | FilePathNameComparerrOption.ConsiderDigitSequenceOfsAsNumber | FilePathNameComparerrOption.ConsiderPathNameDelimiter | FilePathNameComparerrOption.IgnoreCase);
            _absolutePathEntryNamePattern = new Regex(@"^[/\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _entryNamePatternsThatShouldBeIgnored = new Regex(@"([/\\]\.)|(^\.)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _notRootEntryNamePattern = new Regex(@"[/\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _variousNodecomparers = new[]
            {
                new SimpleZipArchiveEntryComparer(false, Encoding.UTF8, ZipArchiveEntryComparerMethod.Normal) as IZipArchiveEntryComparer,
                new SimpleZipArchiveEntryComparer(false, Encoding.UTF8, ZipArchiveEntryComparerMethod.ConsiderSequenceOfDigitsAsNumber),
                new SimpleZipArchiveEntryComparer(false, Encoding.UTF8, ZipArchiveEntryComparerMethod.Ordinal),
                new SimpleZipArchiveEntryComparer(false, Encoding.GetEncoding("IBM437"), ZipArchiveEntryComparerMethod.Normal),
                new SimpleZipArchiveEntryComparer(false, Encoding.GetEncoding("IBM437"), ZipArchiveEntryComparerMethod.ConsiderSequenceOfDigitsAsNumber),
                new SimpleZipArchiveEntryComparer(false, Encoding.GetEncoding("IBM437"), ZipArchiveEntryComparerMethod.Ordinal),
                new SimpleZipArchiveEntryComparer(false, Encoding.GetEncoding(0), ZipArchiveEntryComparerMethod.Normal),
                new SimpleZipArchiveEntryComparer(false, Encoding.GetEncoding(0), ZipArchiveEntryComparerMethod.ConsiderSequenceOfDigitsAsNumber),
                new SimpleZipArchiveEntryComparer(false, Encoding.GetEncoding(0), ZipArchiveEntryComparerMethod.Ordinal),
                new SimpleZipArchiveEntryComparer(true, Encoding.UTF8, ZipArchiveEntryComparerMethod.Normal),
                new SimpleZipArchiveEntryComparer(true, Encoding.UTF8, ZipArchiveEntryComparerMethod.ConsiderSequenceOfDigitsAsNumber),
                new SimpleZipArchiveEntryComparer(true, Encoding.UTF8, ZipArchiveEntryComparerMethod.Ordinal),
                new SimpleZipArchiveEntryComparer(true, Encoding.GetEncoding("IBM437"), ZipArchiveEntryComparerMethod.Normal),
                new SimpleZipArchiveEntryComparer(true, Encoding.GetEncoding("IBM437"), ZipArchiveEntryComparerMethod.ConsiderSequenceOfDigitsAsNumber),
                new SimpleZipArchiveEntryComparer(true, Encoding.GetEncoding("IBM437"), ZipArchiveEntryComparerMethod.Ordinal),
                new SimpleZipArchiveEntryComparer(true, Encoding.GetEncoding(0), ZipArchiveEntryComparerMethod.Normal),
                new SimpleZipArchiveEntryComparer(true, Encoding.GetEncoding(0), ZipArchiveEntryComparerMethod.ConsiderSequenceOfDigitsAsNumber),
                new SimpleZipArchiveEntryComparer(true, Encoding.GetEncoding(0), ZipArchiveEntryComparerMethod.Ordinal),
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
                        type.IsClass &&
                        (type.IsPublic || !item.externalAssembly) &&
                        !type.IsAbstract &&
                        extraFieldInterfaceName is not null &&
                        type.GetInterface(extraFieldInterfaceName) is not null)
                    .Select(type => type.FullName is not null ? item.assembly.CreateInstance(type.FullName) as IExtraField : null)
                    .WhereNotNull()
                    .Select(extrafield => extrafield.ExtraFieldId))
                .ToReadOnlyCollection();
        }

        private EntryTree(FileInfo sourceArchiveFile, ArchiveType archiveType, ZipArchiveEntryCollection sourceEntries, IEnumerable<EntryTreeNode> rootEntries)
        {
            _sourceArchiveFile = sourceArchiveFile;
            _archiveType = archiveType;
            _sourceEntries = sourceEntries;
            _rootEntries = rootEntries.ToList();
        }

        public static EntryTree? GetEntryTree(FileInfo archiveFile, ZipArchiveFile zipFile)
        {
            var entries = zipFile.GetEntries();

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
                       .Select(entry =>
                       {
                           var entryNameElements = entry.FullName.Split('\\', '/').Select(s => (string?)s).ToArray();
                           if (entry.IsDirectory)
                           {
                               if (entryNameElements[^1] == "")
                                   entryNameElements = entryNameElements.Take(entryNameElements.Length - 1).ToArray();
                               entryNameElements = entryNameElements.Concat(new[] { (string?)null }).ToArray();
                           }
                           return new EntryTreeWork(entry, entryNameElements.ToReadOnlyCollection());
                       })));
        }

        public bool Normalize()
        {
            var uselessEntryFileNamePattern = GetUselessEntryFilePattern(_archiveType);
            if (uselessEntryFileNamePattern is null)
                throw new NotSupportedArchiveFileTypeException(_sourceArchiveFile.FullName);

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
                    _sourceEntries.Where(entry => entry.IsFile && entry.FullName == "mimetype")
                    .Take(1)
                    .ToArray();
                if (foundMimeTypeEntry.Length > 0)
                {
                    if (!needToUpdate && foundMimeTypeEntry[0].Order > 0)
                    {
                        RaiseInformationReportedEvent("mimetype エントリを先頭に移動します。");
                        needToUpdate = true;
                    }
                    if (!needToUpdate && foundMimeTypeEntry[0].CompressionMethod != ZipEntryCompressionMethodId.Stored)
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
                    .Where(entry => !entry.IsFile)
                    .Select(entry =>
                    {
                        var path = entry.FullName.Replace('\\', '/');
                        return path.EndsWith("/") ? path : path + "/";
                    })
                    .ToList();
                var filePathList =
                    _sourceEntries
                    .Where(entry => entry.IsFile)
                    .Select(entry => entry.FullName.Replace('\\', '/'))
                    .ToList();
                var emptyDirectories =
                    directoryPathList
                    .Where(directoryPath => filePathList.None(filePath => filePath.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase)));
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

                WarnAboutConfusingEntryNames(_rootEntries, Array.Empty<string>());
                WarnAboutConfusingEntryNameOrder();
                WarnImageUnderDirectory();
            }

            SortEntries();

            if (!needToUpdate)
            {
                if (EnumerateEntry()
                    .Where(entry => entry.ExistsSourceEntry)
                    .NotAll(entry => string.Equals(entry.SourceEntry.FullName, entry.NewEntryFullName, StringComparison.OrdinalIgnoreCase)))
                {
                    RaiseInformationReportedEvent(string.Format("エントリのパス名または順序が変更されているのでアーカイブを変更します。"));
                    needToUpdate = true;
                }
            }

            if (!needToUpdate)
            {
                ModifiedEntry? previousEntry = null;
                foreach (var currentEntry in EnumerateEntry().Where(entry => entry.ExistsSourceEntry))
                {
                    if (previousEntry is not null &&
                        previousEntry.SourceEntry.Order > currentEntry.SourceEntry.Order)
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
                    .Where(entry => entry.ExistsSourceEntry)
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
                    if (entry.encoding == ZipEntryTextEncoding.LocalEncoding && !entry.isConvertableToMinimumCharacterSet)
                    {
                        RaiseInformationReportedEvent(
                            string.Format(
                                "エントリ名とコメントをUTF8に変更します。: \"{0}\", ...",
                                entry.sourceEntryFullNanme));
                        needToUpdate = true;
                        break;
                    }
                    else if (entry.encoding == ZipEntryTextEncoding.UTF8Encoding && entry.isConvertableToMinimumCharacterSet)
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
                    .FirstOrDefault(entry =>
                        entry.ExistsSourceEntry &&
                        entry.SourceEntry.IsFile &&
                        !string.Equals(entry.NewEntryFullName, "mimetype", StringComparison.Ordinal) &&
                        entry.SourceEntry.Size > 0 &&
                        entry.SourceEntry.CompressionMethod == ZipEntryCompressionMethodId.Stored);
                if (foundCompressableEntry is not null)
                {
                    RaiseInformationReportedEvent(string.Format("エントリの圧縮を試みます。: \"{0}\"", foundCompressableEntry.NewEntryFullName));
                    needToUpdate = true;
                }
            }

            if (!needToUpdate)
            {
                foreach (var entry in EnumerateEntry().Where(entry => entry.ExistsSourceEntry))
                {
                    var extendedTimestampExtraField = entry.SourceEntry.ExtraFields.GetData<ExtendedTimestampExtraField>();
                    if (extendedTimestampExtraField is null ||
                        (entry.SourceEntry.LastWriteTimeUtc is not null && extendedTimestampExtraField.LastWriteTimeUtc is null) ||
                        (entry.SourceEntry.LastAccessTimeUtc is not null && extendedTimestampExtraField.LastAccessTimeUtc is null) ||
                        (entry.SourceEntry.CreationTimeUtc is not null && extendedTimestampExtraField.CreationTimeUtc is null))
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

                    if (entry.SourceEntry.LastWriteTimeUtc is not null &&
                        entry.SourceEntry.LastAccessTimeUtc is not null &&
                        entry.SourceEntry.CreationTimeUtc is not null &&
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
                    entry.ExistsSourceEntry &&
                    entry.SourceEntry.IsFile &&
                    (_notedEntryFileNamePattern.IsMatch(Path.GetFileName(entry.NewEntryFullName)) || _notedEntryCrcs.ContainsKey(entry.SourceEntry.Crc)))
                .FirstOrDefault();
            if (foundNotedEntry is not null)
            {
                RaiseWarningReportedEvent(
                    string.Format(
                        "注意すべきエントリがアーカイブファイルに含まれています。: \"{0}\", ...",
                        foundNotedEntry.NewEntryFullName));
            }

            return needToUpdate;
        }

        public bool ContainsEntryIncompatibleWithUnicode()
        {
            return
                _sourceEntries
                .Any(entry =>
                    !entry.FullNameCanBeExpressedInUnicode ||
                    !entry.CommentCanBeExpressedInUnicode);
        }

        public bool ContainsAbsoluteEntryPathName()
        {
            return
                _rootEntries
                .Any(node => node.Name.Length > 0 && _absolutePathEntryNamePattern.IsMatch(node.Name));
        }

        public bool ContainsDuplicateName()
        {
            return ContainsDuplicateName(_rootEntries);
        }

        public bool IsEmpty => _rootEntries.None();

        public void SaveTo(FileInfo destinationFile)
        {

            var fileTimeStamp = (DateTime?)null;
            using (var sourceZipArchiveFile = _sourceArchiveFile.OpenAsZipFile())
            using (var newZipArchiveFileStream = new FileStream(destinationFile.FullName, FileMode.Create))
            using (var newZipArchiveOutputStream = new ZipOutputStream(newZipArchiveFileStream))
            {
                // zip ファイルコメントの設定
                newZipArchiveOutputStream.SetComment(sourceZipArchiveFile.Comment);

                foreach (var modifiedEntry in EnumerateEntry().Where(modifiedEntry => modifiedEntry.ExistsSourceEntry))
                {
                    // ファイルのコピー
                    try
                    {
                        var newEntry = modifiedEntry.SourceEntry.CreateDesdinationEntry(modifiedEntry.NewEntryFullName);

                        // アーカイブファイルの日付の更新 (全エントリ内で最も新しい更新日付を見つける)
#if DEBUG
                        if (modifiedEntry.SourceEntry.LastWriteTimeUtc is null || modifiedEntry.SourceEntry.LastWriteTimeUtc.Value.Kind != DateTimeKind.Utc)
                            throw new Exception();
#endif
                        if (fileTimeStamp is null || (modifiedEntry.SourceEntry.LastWriteTimeUtc is not null && fileTimeStamp.Value < modifiedEntry.SourceEntry.LastWriteTimeUtc.Value))
                            fileTimeStamp = modifiedEntry.SourceEntry.LastWriteTimeUtc;

                        // アーカイブファイルへの書き込み
                        newZipArchiveOutputStream.PutNextEntry(newEntry);
                        using var sourceZipArchiveInputStream = sourceZipArchiveFile.GetContentStream(modifiedEntry.SourceEntry).AsStream();
                        sourceZipArchiveInputStream.CopyTo(newZipArchiveOutputStream, new Progress<UInt64>(_ => RaiseProgressUpdatedEvent()));
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
            if (fileTimeStamp is not null)
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
                    .GroupBy(item => item.Key)
                    .SelectMany(g =>
                    {
                        if (g.Key.ExistsChild)
                        {
                            var directoryEntry =
                                g.FirstOrDefault(item => item.EntryPathElements.Skip(1).First() is null && item.Entry.IsDirectory);
                            var otherEntries =
                                directoryEntry is not null
                                    ? g.Where(item => item.Entry.Index != directoryEntry.Entry.Index)
                                    : g.AsEnumerable();
                            return
                                new[]
                                {
                                    new EntryTreeNode(
                                        g.Key.Name,
                                        BuildTree(otherEntries.Select(item => new EntryTreeWork(item.Entry, item.EntryPathElements.Skip(1)))),
                                        directoryEntry?.Entry)
                                };
                        }
                        else
                        {
                            // 同一名のエントリは1つしかないはずだが、複数検出してしまった場合のために見つかったものは全部処理する
                            return
                                g
                                .Select(item =>
                                    new EntryTreeNode(g.Key.Name, Array.Empty<EntryTreeNode>(), item.Entry));
                        }
                    });
        }

        private static Regex GetUselessEntryFilePattern(ArchiveType archiveType)
        {
            return
                archiveType switch
                {
                    ArchiveType.ImageCollection => _uselessEntryNameForImageCollectionPattern,
                    ArchiveType.AozoraBunko => _uselessEntryNameForAozoraBunkoPattern,
                    ArchiveType.Content => _uselessEntryNameForContentPattern,
                    _ => throw new InternalLogicalErrorException(),
                };
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
                .Any(child => !child.IsFile && ContainsDuplicateName(child.Children));
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
                    if (result && child.Children.None())
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
            if (firstElements.Length != 1 || firstElements[0].IsFile)
                return false;

            // この時点で _rootEntries の要素は単一のディレクトリであることが確定
            var singleDirectoryElement = firstElements[0];
            if (!singleDirectoryElement.RemoveLeadingUselessDirectoryEntry(Array.Empty<string>(), this))
                RaiseInformationReportedEvent(string.Format("無駄なディレクトリエントリを短縮します。: \"{0}\"", singleDirectoryElement.Name));
            _rootEntries = firstElements[0].Children.ToList();
            return true;
        }

        private bool RemoveUselessDirectoryEntry()
        {
            var updated = false;
            foreach (var child in _rootEntries.Where(child => !child.IsFile))
                updated |= child.RemoveUselessDirectoryEntry(new[] { child.Name }, this);

            // 子要素がただ一つでありかつそれがディレクトリであるかどうかを調べる
            var firstElements = _rootEntries.Take(2).ToArray();
            if (firstElements.Length == 1 && firstElements[0]!.IsFile)
            {
                var singleDirectoryElement = firstElements[0];
                if (!updated)
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
            return _rootEntries.SelectMany(child => child.EnumerateEntry(Array.Empty<string>()));
        }

        private static string GetEntryFullPath(IEnumerable<string> pathElements, EntryTreeNode node, bool appendDelimiterForDirectory)
        {
            return string.Join("/", pathElements.Concat(new[] { node.Name })) + (appendDelimiterForDirectory && !node.IsFile ? "/" : "");
        }

        private void WarnAboutConfusingEntryNames(IEnumerable<EntryTreeNode> nodes, IEnumerable<string> pathElements)
        {
            foreach (var node in nodes.Where(node => !node.IsFile))
                WarnAboutConfusingEntryNames(node.Children, pathElements.Concat(new[] { node.Name }));

            var foundDuplicateName =
                nodes
                    .Select(node => new { node, nameWithoutExtension = node.IsFile ? Path.GetFileNameWithoutExtension(node.Name) : node.Name })
                    .GroupBy(item => item.nameWithoutExtension)
                    .Select(g => g.Select(item => item.node).ToList())
                    .Where(item => item.Count > 1)
                    .FirstOrDefault();
            if (foundDuplicateName is not null)
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
                    .Where(item => item.element2.nameWithoutExtension.StartsWith(item.element1.nameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                    .Select(item => new { node1 = item.element1.node, node2 = item.element2.node })
                    .FirstOrDefault();
                if (foundPartialNames is not null)
                {
                    RaiseWarningReportedEvent(
                        string.Format(
                            "部分的に一致した名前のエントリが見つかりました。: \"{0}\", \"{1}\", ...",
                            GetEntryFullPath(pathElements, foundPartialNames.node1, true),
                            GetEntryFullPath(pathElements, foundPartialNames.node2, true)));
                }
            }
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
                            .Where(entry => !_entryNamePatternsThatShouldBeIgnored.IsMatch(entry.FullName))
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
                if (found is not null)
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

        private void WarnImageUnderDirectory()
        {
            var foundNotRootEntry =
                EnumerateEntry()
                .FirstOrDefault(entry =>
                    !_entryNamePatternsThatShouldBeIgnored.IsMatch(entry.NewEntryFullName) &&
                    _notRootEntryNamePattern.IsMatch(entry.NewEntryFullName));
            if (foundNotRootEntry is not null)
            {
                RaiseWarningReportedEvent(
                    string.Format(
                        "階層化されたエントリを見つけました。: \"{0}\", ...",
                        foundNotRootEntry.NewEntryFullName));
            }
        }

        private void SortEntries()
        {
            foreach (var child in _rootEntries.Where(child => !child.IsFile))
                child.SortEntries(_entryNameComparer);
            _rootEntries = _rootEntries.OrderBy(child => child.Name, _entryNameComparer).ToList();
        }

        private void RaiseInformationReportedEvent(string message)
        {
            if (InformationReported is not null)
                InformationReported(this, new FileMessageReportedEventArgs(_sourceArchiveFile, message));
        }

        private void RaiseWarningReportedEvent(string message)
        {
            if (WarningReported is not null)
                WarningReported(this, new FileMessageReportedEventArgs(_sourceArchiveFile, message));
        }

        [SuppressMessage("CodeQuality", "IDE0051:使用されていないプライベート メンバーを削除する", Justification = "<保留中>")]
        private void RaiseErrorReportedEvent(string message)
        {
            if (ErrorReported is not null)
                ErrorReported(this, new FileMessageReportedEventArgs(_sourceArchiveFile, message));
        }

        private void RaiseProgressUpdatedEvent()
        {
            if (ProgressUpdated is not null)
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

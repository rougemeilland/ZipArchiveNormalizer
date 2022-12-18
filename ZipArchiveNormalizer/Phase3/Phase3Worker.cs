using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Utility;
using Utility.FileWorker;
using Utility.IO;
using ZipUtility;

namespace ZipArchiveNormalizer.Phase3
{
    class Phase3Worker
        : FileWorker, IPhaseWorker
    {
        /// <summary>
        /// エントリのパス、サイズ、オフセット、CRCがすべて一致しているかどうかを調べる <see cref="IEqualityComparer{ZipArchiveEntry}">IEqualityComparer&lt;<see cref="ZipArchiveEntry"/>&gt;</see> の実装
        /// </summary>
        private class ZipEntryEasilyEqualityComparer
            : IEqualityComparer<ZipArchiveEntry>
        {
            private readonly bool _compareEntryName;

            public ZipEntryEasilyEqualityComparer(bool compareEntryName)
            {
                _compareEntryName = compareEntryName;
            }

            public bool Equals(ZipArchiveEntry x, ZipArchiveEntry y)
            {
                return
                    (!_compareEntryName || string.Equals(x.FullName, y.FullName, StringComparison.OrdinalIgnoreCase)) &&
                    x.Size.Equals(y.Size) &&
                    x.Crc.Equals(y.Crc);
            }

            public Int32 GetHashCode(ZipArchiveEntry obj)
            {
                return
                    obj.FullName.GetHashCode() ^
                    obj.Size.GetHashCode() ^
                    obj.Crc.GetHashCode();
            }
        }

        /// <summary>
        /// データの内容が同じ二つのアーカイブファイルのうちどちらの重要度が高いかを比較する <see cref="IComparable{ZipArchiveEntriesOfZipFile}">IComparable&lt;<see cref="ZipArchiveEntriesOfZipFile"/>&gt;</see> の実装
        /// </summary>
        private class ArchiveFileImportanceComparer
            : IComparer<ZipArchiveEntriesOfZipFile>
        {
            private static readonly IComparer<FileInfo> _fileImportanceComparer;
            private static readonly Regex _lessImportantEntryNamePattern;

            static ArchiveFileImportanceComparer()
            {
                _fileImportanceComparer = new FileImportanceComparer();
                _lessImportantEntryNamePattern = new Regex(@"^p[0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            public Int32 Compare(ZipArchiveEntriesOfZipFile? x, ZipArchiveEntriesOfZipFile? y)
            {
                if (x is null)
                    return y is null ? 0 : -1;
                else if (y is null)
                    return 1;
                else
                    return CompareZipArchiveEntriesOfZipFile(x, y);
            }

            private static Int32 CompareZipArchiveEntriesOfZipFile(ZipArchiveEntriesOfZipFile x, ZipArchiveEntriesOfZipFile y)
            {
                // 付加されている拡張属性の数が多い方を残す。
                // それが同じなら、エントリ名の長さの合計が多い方を残す
                // それが同じなら、エントリの更新日時の最大値が低い方のファイルを残す。
                // それが同じなら、エントリの作成日時の最大値が低い方のファイルを残す。
                // それが同じなら、アーカイブファイル名が長い方を残す。
                Int32 c;

                // アーカイブファイルに含まれているエントリの拡張属性の種類の数の合計が多い方のファイルが重要
                if ((c =
                    x.ZipEntries.Sum(entry => (Int64)entry.ExtraFields.Count)
                    .CompareTo(y.ZipEntries.Sum(entry => (Int64)entry.ExtraFields.Count))) != 0)
                    return c;

                // エントリのファイル名全てが"p<数字>"で始まるわけではない方のファイルが優先
                if ((c =
                    x.ZipEntries.NotAll(entry => _lessImportantEntryNamePattern.IsMatch(Path.GetFileName(entry.FullName)))
                    .CompareTo(y.ZipEntries.NotAll(entry => _lessImportantEntryNamePattern.IsMatch(Path.GetFileName(entry.FullName))))) != 0)
                    return c;

                // アーカイブファイルに含まれているエントリの名前の長さの合計が多い方のファイルが重要
                if ((c =
                    x.ZipEntries.Sum(entry => (Int64)entry.FullName.Length)
                    .CompareTo(y.ZipEntries.Sum(entry => (Int64)entry.FullName.Length))) != 0)
                    return c;

                // アーカイブファイルに含まれているエントリの最新更新日付が古い方のファイルが重要 (日付がないエントリが一つでもあればそのファイルは「より重要ではない」)
                if ((c =
                    x.ZipEntries.Max(entry => entry.LastWriteTimeUtc?.Ticks ?? Int64.MaxValue)
                    .CompareTo(y.ZipEntries.Max(entry => entry.LastWriteTimeUtc?.Ticks ?? Int64.MaxValue))) != 0)
                    return -c;

                // アーカイブファイルに含まれているエントリの最新作成日付が古い方のファイルが重要 (日付がないエントリが一つでもあればそのファイルは「より重要ではない」)
                if ((c =
                    x.ZipEntries.Max(entry => entry.CreationTimeUtc?.Ticks ?? Int64.MaxValue)
                    .CompareTo(y.ZipEntries.Max(entry => entry.CreationTimeUtc?.Ticks ?? Int64.MaxValue))) != 0)
                    return -c;

                return _fileImportanceComparer.Compare(x.File, y.File);
            }
        }

        private class ZipArchiveEntriesSummaryOfZipFile
        {
            public ZipArchiveEntriesSummaryOfZipFile(FileInfo file, ZipArchiveFile zipArchiveFile)
            {
                File = file;
                ZipEntriesCount = zipArchiveFile.GetEntries().Count;
            }

            public FileInfo File { get; }
            public Int64 ZipEntriesCount { get; }
        }

        private class ZipArchiveEntriesOfZipFile
        {
            public ZipArchiveEntriesOfZipFile(FileInfo file, ZipArchiveFile zipArchiveFile)
            {
                File = file;
                ZipFile = zipArchiveFile;
                ZipEntries = zipArchiveFile.GetEntries();
            }

            public FileInfo File { get; }
            public ZipArchiveFile ZipFile { get; }
            public ZipArchiveEntryCollection ZipEntries { get; }
        }

        private static readonly IEqualityComparer<ZipArchiveEntry> _zipEntryMoreEasilyEqualityComparer;
        private static readonly IEqualityComparer<ZipArchiveEntry> _zipEntryEasilyEqualityComparer;
        private static readonly IComparer<ZipArchiveEntriesOfZipFile> _archiveFileImportanceComparer;

        private readonly Func<FileInfo, bool> _isBadFileSelecter;

        public event EventHandler<BadFileFoundEventArgs>? BadFileFound;

        static Phase3Worker()
        {
            _zipEntryMoreEasilyEqualityComparer = new ZipEntryEasilyEqualityComparer(false);
            _zipEntryEasilyEqualityComparer = new ZipEntryEasilyEqualityComparer(true);
            _archiveFileImportanceComparer = new ArchiveFileImportanceComparer();
        }

        public Phase3Worker(IWorkerCancellable canceller, Func<FileInfo, bool> isBadFileSelecter)
            : base(canceller)
        {
            _isBadFileSelecter = isBadFileSelecter;
        }

        public override string Description => "同一内容のZIPファイルがないか調べます。";

        protected override void ExecuteWork(IEnumerable<FileInfo> sourceFiles, IFileWorkerExecutionResult? previousWorkerResult)
        {
            UpdateProgress();

            var targetArchiveFiles =
                sourceFiles
                .Where(file =>
                    !_isBadFileSelecter(file) &&
                    file.Extension.IsAnyOf(".zip", ".epub", StringComparison.OrdinalIgnoreCase))
                .ToReadOnlyCollection();
            SetToSourceFiles(targetArchiveFiles);

            var destinationFiles = new Dictionary<string, FileInfo>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var sourceFile in targetArchiveFiles)
                destinationFiles[sourceFile.FullName] = sourceFile;

            var totalCount = (UInt64)targetArchiveFiles.Sum(file => file.Length) * 2;
            var counrOfDone = 0UL;
            var groupedArchiveFiles =
                targetArchiveFiles
                .Select(file =>
                {
                    SafetyCancellationCheck();
                    using var zipFile = file.OpenAsZipFile();
                    var o = new ZipArchiveEntriesSummaryOfZipFile(file, zipFile);
                    UpdateProgress(totalCount, Interlocked.Add(ref counrOfDone, (UInt64)file.Length));
                    return o;
                })
                .Select(archiveFile => new { archiveFile, entriesCount = archiveFile.ZipEntriesCount })
                .GroupBy(item => item.entriesCount)
                .Select(g => g.ToReadOnlyCollection())
                .ToReadOnlyCollection();
            UpdateProgress(totalCount, counrOfDone);
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                groupedArchiveFiles
                    .AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount)
                    .WithCancellation(cancellationTokenSource.Token)
                    .ForAll(item =>
                    {
                        if (IsRequestedToCancel)
                        {
                            cancellationTokenSource.Cancel();
                            return;
                        }
                        var archiveFiles = item.Select(item2 => item2.archiveFile).ToReadOnlyCollection();
                        var sizeOfArchiveFiles = (UInt64)archiveFiles.Sum(item2 => item2.File.Length);
                        while (archiveFiles.Count >= 2)
                        {
                            try
                            {
                                archiveFiles = DeleteAndExcludeUselessArchiveFile(archiveFiles, deletedFile => destinationFiles.Remove(deletedFile.FullName)).ToReadOnlyCollection();
                            }
                            catch (OperationCanceledException)
                            {
                                cancellationTokenSource.Cancel();
                            }
                            catch (Exception ex)
                            {
                                throw
                                    new Exception(
                                        string.Format(
                                            "並列処理中に例外が発生しました。: 処理クラス={0}, 対象ファイル={{{1}}}, message=\"{2}\", スタックトレース=>{3}",
                                            GetType().FullName,
                                            string.Join(", ", archiveFiles.Select(file => string.Format("\"{0}\"", file.File.FullName))),
                                            ex.Message,
                                            ex.StackTrace),
                                        ex);
                            }
                        }
                        UpdateProgress(totalCount, Interlocked.Add(ref counrOfDone, sizeOfArchiveFiles));
                    });
                if (cancellationTokenSource.IsCancellationRequested)
                    throw new OperationCanceledException();
            }
            SafetyCancellationCheck();
            UpdateProgress();
            foreach (var destinationFile in destinationFiles.Values)
                AddToDestinationFiles(destinationFile);
            UpdateProgress();
        }

        private IEnumerable<ZipArchiveEntriesSummaryOfZipFile> DeleteAndExcludeUselessArchiveFile(IEnumerable<ZipArchiveEntriesSummaryOfZipFile> zipArchives, Action<FileInfo> onDelete)
        {
            FileInfo? fileToDelete = null;
            FileInfo? otherFile = null;
            var zipArchiveFileSummary1 = zipArchives.First();
            foreach (var zipArchiveFileSummary2 in zipArchives.Skip(1))
            {
                SafetyCancellationCheck();
                UpdateProgress();

                using var zipFile1 = zipArchiveFileSummary1.File.OpenAsZipFile();
                using var zipFile2 = zipArchiveFileSummary2.File.OpenAsZipFile();
                var zipArchiveFileInfo1 = new ZipArchiveEntriesOfZipFile(zipArchiveFileSummary1.File, zipFile1);
                var zipArchiveFileInfo2 = new ZipArchiveEntriesOfZipFile(zipArchiveFileSummary2.File, zipFile2);

                var isLikelyToBeEqual = EntriesEqualMoreEasily(zipArchiveFileInfo1, zipArchiveFileInfo2);
                var isMostlyEqual = EntriesEqualEasily(zipArchiveFileInfo1, zipArchiveFileInfo2);

                if (isMostlyEqual)
                {
                    // 全てのエントリの名前、オフセット、サイズ、CRCが等しい場合
                    var isExactlyEqual = EntriesEqualStrictly(zipArchiveFileInfo1, zipArchiveFileInfo2);
                    if (isExactlyEqual)
                    {
                        // 全てのエントリのデータが一致している場合

                        // ファイルの重要度を比較する
                        if (_archiveFileImportanceComparer.Compare(zipArchiveFileInfo1, zipArchiveFileInfo2) > 0)
                        {
                            fileToDelete = zipArchiveFileInfo2.File;
                            otherFile = zipArchiveFileInfo1.File;
                        }
                        else
                        {
                            fileToDelete = zipArchiveFileInfo1.File;
                            otherFile = zipArchiveFileInfo2.File;
                        }
                        break;
                    }
                }
                else if (isLikelyToBeEqual)
                {
                    // 全てのエントリのオフセット、サイズ、CRCが等しい場合
                    var isExactlyEqual = EntriesEqualStrictly(zipArchiveFileInfo1, zipArchiveFileInfo2);
                    if (isExactlyEqual)
                    {
                        // 全てのエントリのデータが一致している場合

                        if (_archiveFileImportanceComparer.Compare(zipArchiveFileInfo1, zipArchiveFileInfo2) > 0)
                        {
                            RaiseWarningReportedEvent(
                                zipArchiveFileInfo2.File,
                                string.Format(
                                    "エントリ名を除いて、エントリのデータがエントリの順番も含めて全て同一の別のアーカイブファイルが存在します。: \"{0}\"",
                                    zipArchiveFileInfo1.File.FullName));
                        }
                        else
                        {
                            RaiseWarningReportedEvent(
                                zipArchiveFileInfo1.File,
                                string.Format(
                                    "エントリ名を除いて、エントリのデータがエントリの順番も含めて全て同一の別のアーカイブファイルが存在します。: \"{0}\"",
                                    zipArchiveFileInfo2.File.FullName));
                        }
                    }
                }
                else
                {
                    // NOP
                }
            }
            if (fileToDelete is not null && otherFile is not null)
            {
#if DEBUG
                if (string.Equals(fileToDelete.FullName, otherFile.FullName, StringComparison.OrdinalIgnoreCase))
                    throw new Exception();
#endif
                RaiseInformationReportedEvent(
                    fileToDelete,
                    string.Format("全てのエントリの内容が同じ別のアーカイブファイルを削除します。: \"{0}\"",
                    otherFile.FullName));
                fileToDelete.SendToRecycleBin();
                onDelete(fileToDelete);
                IncrementChangedFileCount();
                return
                    zipArchives
                    .Where(item => !string.Equals(item.File.FullName, fileToDelete.FullName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return
                    zipArchives.Skip(1);
            }
        }

        private static bool EntriesEqualMoreEasily(ZipArchiveEntriesOfZipFile archiveFile1, ZipArchiveEntriesOfZipFile archiveFile2)
        {
            try
            {
                return
                    archiveFile1.ZipEntries
                    .SequenceEqual(archiveFile2.ZipEntries, _zipEntryMoreEasilyEqualityComparer);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool EntriesEqualEasily(ZipArchiveEntriesOfZipFile archiveFile1, ZipArchiveEntriesOfZipFile archiveFile2)
        {
            try
            {
                return
                    archiveFile1.ZipEntries
                    .SequenceEqual(archiveFile2.ZipEntries, _zipEntryEasilyEqualityComparer);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool EntriesEqualStrictly(ZipArchiveEntriesOfZipFile archiveFile1, ZipArchiveEntriesOfZipFile archiveFile2)
        {
            try
            {
                return
                    archiveFile1.ZipEntries.Count == archiveFile2.ZipEntries.Count &&
                    archiveFile1.ZipEntries
                    .Zip(archiveFile2.ZipEntries, (entry1, entry2) => new { entry1, entry2 })
                    .All(item =>
                        archiveFile1.ZipFile.GetContentStream(item.entry1)
                        .StreamBytesEqual(archiveFile2.ZipFile.GetContentStream(item.entry2), progress: new Progress<UInt64>(_ => UpdateProgress())));
            }
            catch (Exception)
            {
                return false;
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:使用されていないプライベート メンバーを削除する", Justification = "<保留中>")]
        private void RaiseBadFileFoundEvent(FileInfo targetFile)
        {
            if (BadFileFound is not null)
            {
                BadFileFound(this, new BadFileFoundEventArgs(targetFile));
            }
        }
    }
}

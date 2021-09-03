using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Utility;
using Utility.FileWorker;
using ZipUtility;

namespace ZipArchiveNormalizer.Phase4
{
    class Phase4Worker
        : FileWorker, IPhaseWorker
    {
        private class ZipArchiveFileSummary
        {
            private IDictionary<long, IReadOnlyCollection<ZipArchiveEntry>> _entries;

            public ZipArchiveFileSummary(FileInfo archiveFile)
            {
                ArchiveFile = archiveFile;
                _entries =
                    archiveFile
                    .EnumerateZipArchiveEntry()
                    .Where(entry => entry.IsFile)
                    .GroupBy(entry => entry.Crc)
                    .ToDictionary(g => g.Key, g => g.ToReadOnlyCollection());
                TotalOfEntryCount = _entries.Values.Sum(item => item.Count);
                TotalOfExtraFieldCount = _entries.Values.Sum(item => item.Sum(entry => (long)entry.ExtraFields.Count));
                TotalOfEntryNameLength = _entries.Values.Sum(item => item.Sum(entry => (long)entry.FullName.Length));
                NewestWriteTimeTicks = _entries.Values.Max(item => item.Max(entry => entry.LastWriteTimeUtc?.Ticks ?? long.MinValue));
            }

            public FileInfo ArchiveFile { get; }
            public IEnumerable<long> Crcs => _entries.Keys;

            public IReadOnlyCollection<ZipArchiveEntry> FindEntriesByCrc(long crc)
            {
                IReadOnlyCollection<ZipArchiveEntry> entries;
                if (_entries.TryGetValue(crc, out entries))
                    return entries.ToList();
                else
                    return null;
            }

            public bool ContainsCrc(long crc) => _entries.ContainsKey(crc);
            public int TotalOfEntryCount { get; }
            public long TotalOfExtraFieldCount { get; }
            public long TotalOfEntryNameLength { get; }
            public long NewestWriteTimeTicks { get; }
        }

        private class ZipArchiveFileSummaryImportanceComparer
            : IComparer<ZipArchiveFileSummary>
        {
            private static IComparer<FileInfo> _fileImportanceComparer;

            static ZipArchiveFileSummaryImportanceComparer()
            {
                _fileImportanceComparer = new FileImportanceComparer();
            }

            public int Compare(ZipArchiveFileSummary x, ZipArchiveFileSummary y)
            {
                if (x == null)
                    return y == null ? 0 : -1;
                else if (y == null)
                    return 1;
                else
                {
                    int c;
                    if ((c = x.TotalOfEntryCount.CompareTo(y.TotalOfEntryCount)) != 0)
                        return c;
                    if ((c = x.TotalOfExtraFieldCount.CompareTo(y.TotalOfExtraFieldCount)) != 0)
                        return c;
                    if ((c = x.TotalOfEntryNameLength.CompareTo(y.TotalOfEntryNameLength)) != 0)
                        return c;
                    if ((c = x.NewestWriteTimeTicks.CompareTo(y.NewestWriteTimeTicks)) != 0)
                        return -c;
                    return _fileImportanceComparer.Compare(x.ArchiveFile, y.ArchiveFile);
                }
            }
        }

        private enum EntriesContaininfType
        {
            NotContains,
            ContansAndOnlyCrcMatched,
            ContansAndAllDataMatched,
        }

        private static IComparer<ZipArchiveFileSummary> _zipArchiveFileSummaryImportanceComparer;
        private Func<FileInfo, bool> _isBadFileSelecter;

        public event EventHandler<BadFileFoundEventArgs> BadFileFound;

        static Phase4Worker()
        {
            _zipArchiveFileSummaryImportanceComparer = new ZipArchiveFileSummaryImportanceComparer();
        }

        public Phase4Worker(IWorkerCancellable canceller, Func<FileInfo, bool> isBadFileSelecter)
            : base(canceller)
        {
            _isBadFileSelecter = isBadFileSelecter;
        }

        public override string Description => "似た内容のZIPファイルがないか調べます。";

        protected override void ExecuteWork(IEnumerable<FileInfo> sourceFiles, IFileWorkerExecutionResult previousWorkerResult)
        {
            UpdateProgress();

            var archiveFiles =
                sourceFiles
                .Where(file =>
                    _isBadFileSelecter(file) == false &&
                    file.Extension.IsAnyOf(".zip", ".epub", StringComparison.OrdinalIgnoreCase))
                .ToReadOnlyCollection();
            SetToSourceFiles(archiveFiles);

            var totalCount = archiveFiles.Count + archiveFiles.Count * (archiveFiles.Count - 1) / 2;
            var counrOfDone = 0L;
            UpdateProgress(totalCount, counrOfDone);

            var archiveFileSummaries =
                archiveFiles
                .Select(file =>
                {
                    if (IsRequestedToCancel)
                        throw new OperationCanceledException();
                    var summary = new ZipArchiveFileSummary(file);
                    UpdateProgress(totalCount, Interlocked.Increment(ref counrOfDone));
                    return summary;
                })
                .QuickSort(item => item, _zipArchiveFileSummaryImportanceComparer);

            if (archiveFileSummaries.Length > 0)
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    Enumerable.Range(0, archiveFileSummaries.Length - 1)
                    .SelectMany(
                        index1 => Enumerable.Range(index1 + 1, archiveFileSummaries.Length - index1 - 1).Where(index2 => index1 != index2),
                            (index1, index2) => new { index1, index2 })
                        .Select(item => new { summary1 = archiveFileSummaries[item.index1], summary2 = archiveFileSummaries[item.index2] })
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
                            try
                            {
                                SearchSimilarFiles(
                                    item.summary1,
                                    item.summary2,
                                    () => UpdateProgress());
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
                                            "並列処理中に例外が発生しました。: 処理クラス={0}, 対象ファイル1=\"{1}\", 対象ファイル2=\"{2}\", message=\"{3}\", スタックトレース=>{4}",
                                            GetType().FullName,
                                            item.summary1.ArchiveFile.FullName,
                                            item.summary2.ArchiveFile.FullName,
                                            ex.Message,
                                            ex.StackTrace),
                                        ex);
                            }
                            UpdateProgress(totalCount, Interlocked.Increment(ref counrOfDone));
                        });
                    if (cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException();
                }
            }
            if (IsRequestedToCancel)
                throw new OperationCanceledException();
            foreach (var archiveFileSummary in archiveFileSummaries)
                AddToDestinationFiles(archiveFileSummary.ArchiveFile);
        }

        private void SearchSimilarFiles(ZipArchiveFileSummary summary1, ZipArchiveFileSummary summary2, Action progressUpdater)
        {
            var crcAndEntryCount1 =
                summary1.Crcs
                .Select(crc => new { crc, entryCount = summary1.FindEntriesByCrc(crc).Count });
            var crcAndEntryCount2 =
                summary2.Crcs
                .Select(crc => new { crc, entryCount = summary2.FindEntriesByCrc(crc).Count });
            var equalCrcAndEntryCount =
                crcAndEntryCount1.SequenceEqual(
                    crcAndEntryCount2,
                    crcAndEntryCount2.CreateEqualityComparer(
                        (x, y) => x.crc == y.crc && x.entryCount == y.entryCount,
                        x => x.crc.GetHashCode() ^ x.entryCount.GetHashCode()));
            var result1 = CheckIfEntriesAreIncludedInOtherEntries(summary1, summary2, progressUpdater);
            var result2 = CheckIfEntriesAreIncludedInOtherEntries(summary2, summary1, progressUpdater);
            if (result1 == EntriesContaininfType.ContansAndAllDataMatched)
            {
                if (result2 == EntriesContaininfType.ContansAndAllDataMatched)
                {
                    if (equalCrcAndEntryCount)
                    {
                        RaiseWarningReportedEvent(
                            summary1.ArchiveFile,
                            string.Format(
                                "エントリ名を除いて、アーカイブに含まれるエントリがすべて別のアーカイブと完全に等しいです。(データの一致): \"{0}\"",
                                summary2.ArchiveFile.FullName));
                    }
                    else
                    {
                        RaiseWarningReportedEvent(
                            summary1.ArchiveFile,
                            string.Format(
                                "エントリ名を除いて、アーカイブに含まれるエントリが全て別のアーカイブと完全に等しいです。(データの一致) どちらかに同じ内容のファイルが重複して存在しているかもしれません。: \"{0}\"",
                                summary2.ArchiveFile.FullName));
                    }
                }
                else if (result2 == EntriesContaininfType.ContansAndOnlyCrcMatched)
                {
                    if (equalCrcAndEntryCount)
                    {
                        RaiseWarningReportedEvent(
                            summary1.ArchiveFile,
                            string.Format(
                                "エントリ名を除いて、アーカイブに含まれるエントリがすべて別のアーカイブとほぼ等しいです。(CRCの一致): \"{0}\"",
                                summary2.ArchiveFile.FullName));
                    }
                    else
                    {
                        RaiseWarningReportedEvent(
                            summary1.ArchiveFile,
                            string.Format(
                                "エントリ名を除いて、アーカイブに含まれるエントリが全て別のアーカイブとほぼ等しいです。(CRCの一致) どちらかに同じ内容のファイルが重複して存在しているかもしれません。: \"{0}\"",
                                summary2.ArchiveFile.FullName));
                    }
                }
                else
                {
                    RaiseWarningReportedEvent(
                        summary1.ArchiveFile,
                        string.Format(
                            "アーカイブに含まれるエントリがすべて別のアーカイブにも含まれています。(データの一致) エントリ名は異なるかもしれません。: \"{0}\"",
                            summary2.ArchiveFile.FullName));
                }
            }
            else if (result1 == EntriesContaininfType.ContansAndOnlyCrcMatched)
            {
                if (result2 == EntriesContaininfType.ContansAndAllDataMatched || result2 == EntriesContaininfType.ContansAndOnlyCrcMatched)
                {
                    if (equalCrcAndEntryCount)
                    {
                        RaiseWarningReportedEvent(
                            summary1.ArchiveFile,
                            string.Format(
                                "エントリ名を除いて、アーカイブに含まれるエントリがすべて別のアーカイブとほぼ等しいです。(CRCの一致): \"{0}\"",
                                summary2.ArchiveFile.FullName));
                    }
                    else
                    {
                        RaiseWarningReportedEvent(
                            summary1.ArchiveFile,
                            string.Format(
                                "エントリ名を除いて、アーカイブに含まれるエントリが全て別のアーカイブとほぼ等しいです。(CRCの一致) どちらかに同じ内容のファイルが重複して存在しているかもしれません。: \"{0}\"",
                                summary2.ArchiveFile.FullName));
                    }
                }
                else
                {
                    RaiseWarningReportedEvent(
                        summary1.ArchiveFile,
                        string.Format(
                            "アーカイブに含まれるエントリがすべて別のアーカイブにも含まれている可能性があります。(CRCの一致) エントリ名は異なるかもしれません。: \"{0}\"",
                            summary2.ArchiveFile.FullName));
                }
            }
            else
            {
                if (result2 == EntriesContaininfType.ContansAndAllDataMatched)
                {
                    RaiseWarningReportedEvent(
                        summary2.ArchiveFile,
                        string.Format(
                            "アーカイブに含まれるエントリがすべて別のアーカイブにも含まれています。(データの一致) エントリ名は異なるかもしれません。: \"{0}\"",
                            summary1.ArchiveFile.FullName));
                }
                else if (result2 == EntriesContaininfType.ContansAndOnlyCrcMatched)
                {
                    RaiseWarningReportedEvent(
                        summary2.ArchiveFile,
                        string.Format(
                            "アーカイブに含まれるエントリがすべて別のアーカイブにも含まれている可能性があります。(CRCの一致) エントリ名は異なるかもしれません。: \"{0}\"",
                            summary1.ArchiveFile.FullName));
                }
                else
                {
                    // NOP
                }
            }
        }

        private EntriesContaininfType CheckIfEntriesAreIncludedInOtherEntries(ZipArchiveFileSummary summary1, ZipArchiveFileSummary summary2, Action progressUpdater)
        {
            if (summary1.Crcs.All(crc => summary2.ContainsCrc(crc)) == false)
                return EntriesContaininfType.NotContains;
            if (summary1.Crcs.All(crc => EqualsArchiveFileEntryOfCrc(summary1, summary2, crc, progressUpdater) == true) == false)
                return EntriesContaininfType.ContansAndOnlyCrcMatched;
            return EntriesContaininfType.ContansAndAllDataMatched;
        }

        private bool? EqualsArchiveFileEntryOfCrc(ZipArchiveFileSummary summary1, ZipArchiveFileSummary summary2, long crc, Action progressUpdater)
        {
            try
            {
                if (IsRequestedToCancel)
                    throw new OperationCanceledException();
                UpdateProgress();
                var entries1 = summary1.FindEntriesByCrc(crc);
                if (!entries1.IsSingle())
                    return null;
                var entries2 = summary2.FindEntriesByCrc(crc);
                if (!entries2.IsSingle())
                    return null;
                using (var zipFile1 = new ZipFile(summary1.ArchiveFile.FullName))
                using (var zipFile2 = new ZipFile(summary2.ArchiveFile.FullName))
                {
                    return
                        zipFile1.GetInputStream(entries1.Single())
                        .StreamBytesEqual(zipFile2.GetInputStream(entries2.Single()));
                }
            }
            finally
            {
                progressUpdater();
            }
        }

        private void RaiseBadFileFoundEvent(FileInfo targetFile)
        {
            if (BadFileFound != null)
                BadFileFound(this, new BadFileFoundEventArgs(targetFile));
        }
    }
}
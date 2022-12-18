using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utility;
using Utility.FileWorker;
using Utility.IO;
using ZipUtility;

namespace ZipArchiveNormalizer.Phase4
{
    class Phase4Worker
        : FileWorker, IPhaseWorker
    {
        private class ZipArchiveFileSummary
        {
            public ZipArchiveFileSummary(FileInfo file, ZipArchiveFile zipFile)
            {
                File = file;
                var entries =
                    zipFile.GetEntries()
                    .Where(entry => entry.IsFile)
                    .ToReadOnlyCollection();
                TotalOfEntryCount = entries.Count;
                TotalOfExtraFieldCount = entries.Sum(entry => (Int64)entry.ExtraFields.Count);
                TotalOfEntryNameLength = entries.Sum(entry => (Int64)entry.FullName.Length);
                NewestWriteTimeTicks = entries.Max(entry => entry.LastWriteTimeUtc?.Ticks ?? Int64.MinValue);
            }

            public FileInfo File { get; }
            public Int32 TotalOfEntryCount { get; }
            public Int64 TotalOfExtraFieldCount { get; }
            public Int64 TotalOfEntryNameLength { get; }
            public Int64 NewestWriteTimeTicks { get; }
        }

        private class ZipArchiveFileDetail
        {
            private readonly ZipArchiveFile _zipFile;
            private readonly IDictionary<Int64, IReadOnlyCollection<ZipArchiveEntry>> _entries;

            public ZipArchiveFileDetail(FileInfo file, ZipArchiveFile zipFile)
            {
                File = file;
                _zipFile = zipFile;
                _entries =
                    zipFile.GetEntries()
                    .Where(entry => entry.IsFile)
                    .GroupBy(entry => entry.Crc)
                    .ToDictionary(g => g.Key, g => g.ToReadOnlyCollection());
            }

            public FileInfo File { get; }
            public IEnumerable<Int64> Crcs => _entries.Keys;

            public IReadOnlyCollection<ZipArchiveEntry> FindEntriesByCrc(Int64 crc)
            {
                if (!_entries.TryGetValue(crc, out IReadOnlyCollection<ZipArchiveEntry>? entries))
                    throw new InternalLogicalErrorException();
                return entries.ToList();
            }

            public bool ContainsCrc(Int64 crc) => _entries.ContainsKey(crc);
            public IInputByteStream<UInt64> GetInputStream(ZipArchiveEntry entry) => _zipFile.GetContentStream(entry);
        }

        private class ZipArchiveFileSummaryImportanceComparer
            : IComparer<ZipArchiveFileSummary>
        {
            private static readonly IComparer<FileInfo> _fileImportanceComparer;

            static ZipArchiveFileSummaryImportanceComparer()
            {
                _fileImportanceComparer = new FileImportanceComparer();
            }

            public Int32 Compare(ZipArchiveFileSummary? x, ZipArchiveFileSummary? y)
            {
                if (x is null)
                    return y is null ? 0 : -1;
                else if (y is null)
                    return 1;
                else
                {
                    Int32 c;
                    if ((c = x.TotalOfEntryCount.CompareTo(y.TotalOfEntryCount)) != 0)
                        return c;
                    if ((c = x.TotalOfExtraFieldCount.CompareTo(y.TotalOfExtraFieldCount)) != 0)
                        return c;
                    if ((c = x.TotalOfEntryNameLength.CompareTo(y.TotalOfEntryNameLength)) != 0)
                        return c;
                    if ((c = x.NewestWriteTimeTicks.CompareTo(y.NewestWriteTimeTicks)) != 0)
                        return -c;
                    return _fileImportanceComparer.Compare(x.File, y.File);
                }
            }
        }

        private enum EntriesContaininfType
        {
            NotContains,
            ContansAndOnlyCrcMatched,
            ContansAndAllDataMatched,
        }

        private static readonly IComparer<ZipArchiveFileSummary> _zipArchiveFileSummaryImportanceComparer;

        private readonly Func<FileInfo, bool> _isBadFileSelecter;

        public event EventHandler<BadFileFoundEventArgs>? BadFileFound;

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

        protected override void ExecuteWork(IEnumerable<FileInfo> sourceFiles, IFileWorkerExecutionResult? previousWorkerResult)
        {
            UpdateProgress();

            var archiveFiles =
                sourceFiles
                .Where(file =>
                    !_isBadFileSelecter(file) &&
                    file.Extension.IsAnyOf(".zip", ".epub", StringComparison.OrdinalIgnoreCase))
                .ToReadOnlyCollection();
            SetToSourceFiles(archiveFiles);

            var totalCount = (UInt64)archiveFiles.Count + (UInt64)archiveFiles.Count * ((UInt64)archiveFiles.Count - 1) / 2;
            var counrOfDone = 0UL;
            UpdateProgress(totalCount, counrOfDone);

            var archiveFileSummaries =
                archiveFiles
                .Select(file =>
                {
                    SafetyCancellationCheck();
                    using var zipFile = file.OpenAsZipFile();
                    var summary = new ZipArchiveFileSummary(file, zipFile);
                    UpdateProgress(totalCount, Interlocked.Increment(ref counrOfDone));
                    return summary;
                })
                .QuickSort(item => item, _zipArchiveFileSummaryImportanceComparer);

            if (archiveFileSummaries.Length > 0)
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                Enumerable.Range(0, archiveFileSummaries.Length - 1)
                .SelectMany(
                    index1 => Enumerable.Range(index1 + 1, archiveFileSummaries.Length - index1 - 1).Where(index2 => index1 != index2),
                        (index1, index2) => new { index1, index2 })
                    .Select(item => new { summary1 = archiveFileSummaries.Span[item.index1], summary2 = archiveFileSummaries.Span[item.index2] })
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
                            using var zipFile1 = item.summary1.File.OpenAsZipFile();
                            using var zipFile2 = item.summary2.File.OpenAsZipFile();
                            var detail1 = new ZipArchiveFileDetail(item.summary1.File, zipFile1);
                            var detail2 = new ZipArchiveFileDetail(item.summary2.File, zipFile2);
                            SearchSimilarFiles(detail1, detail2, () => UpdateProgress());
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
                                        item.summary1.File.FullName,
                                        item.summary2.File.FullName,
                                        ex.Message,
                                        ex.StackTrace),
                                    ex);
                        }
                        UpdateProgress(totalCount, Interlocked.Increment(ref counrOfDone));
                    });
                if (cancellationTokenSource.IsCancellationRequested)
                    throw new OperationCanceledException();
            }
            SafetyCancellationCheck();
            foreach (var archiveFileSummary in archiveFileSummaries.Span)
                AddToDestinationFiles(archiveFileSummary.File);
        }

        private void SearchSimilarFiles(ZipArchiveFileDetail detail1, ZipArchiveFileDetail detail2, Action progressUpdater)
        {
            var crcAndEntryCount1 =
                detail1.Crcs
                .Select(crc => new { crc, entryCount = detail1.FindEntriesByCrc(crc).Count });
            var crcAndEntryCount2 =
                detail2.Crcs
                .Select(crc => new { crc, entryCount = detail2.FindEntriesByCrc(crc).Count });
            var equalCrcAndEntryCount = crcAndEntryCount1.SequenceEqual(crcAndEntryCount2);
            var result1 = CheckIfEntriesAreIncludedInOtherEntries(detail1, detail2, progressUpdater);
            var result2 = CheckIfEntriesAreIncludedInOtherEntries(detail2, detail1, progressUpdater);
            if (result1 == EntriesContaininfType.ContansAndAllDataMatched)
            {
                if (result2 == EntriesContaininfType.ContansAndAllDataMatched)
                {
                    if (equalCrcAndEntryCount)
                    {
                        RaiseWarningReportedEvent(
                            detail1.File,
                            string.Format(
                                "エントリ名を除いて、アーカイブに含まれるエントリがすべて別のアーカイブと完全に等しいです。(データの一致): \"{0}\"",
                                detail2.File.FullName));
                    }
                    else
                    {
                        RaiseWarningReportedEvent(
                            detail1.File,
                            string.Format(
                                "エントリ名を除いて、アーカイブに含まれるエントリが全て別のアーカイブと完全に等しいです。(データの一致) どちらかに同じ内容のファイルが重複して存在しているかもしれません。: \"{0}\"",
                                detail2.File.FullName));
                    }
                }
                else if (result2 == EntriesContaininfType.ContansAndOnlyCrcMatched)
                {
                    if (equalCrcAndEntryCount)
                    {
                        RaiseWarningReportedEvent(
                            detail1.File,
                            string.Format(
                                "エントリ名を除いて、アーカイブに含まれるエントリがすべて別のアーカイブとほぼ等しいです。(CRCの一致): \"{0}\"",
                                detail2.File.FullName));
                    }
                    else
                    {
                        RaiseWarningReportedEvent(
                            detail1.File,
                            string.Format(
                                "エントリ名を除いて、アーカイブに含まれるエントリが全て別のアーカイブとほぼ等しいです。(CRCの一致) どちらかに同じ内容のファイルが重複して存在しているかもしれません。: \"{0}\"",
                                detail2.File.FullName));
                    }
                }
                else
                {
                    RaiseWarningReportedEvent(
                        detail1.File,
                        string.Format(
                            "アーカイブに含まれるエントリがすべて別のアーカイブにも含まれています。(データの一致) エントリ名は異なるかもしれません。: \"{0}\"",
                            detail2.File.FullName));
                }
            }
            else if (result1 == EntriesContaininfType.ContansAndOnlyCrcMatched)
            {
                if (result2 == EntriesContaininfType.ContansAndAllDataMatched || result2 == EntriesContaininfType.ContansAndOnlyCrcMatched)
                {
                    if (equalCrcAndEntryCount)
                    {
                        RaiseWarningReportedEvent(
                            detail1.File,
                            string.Format(
                                "エントリ名を除いて、アーカイブに含まれるエントリがすべて別のアーカイブとほぼ等しいです。(CRCの一致): \"{0}\"",
                                detail2.File.FullName));
                    }
                    else
                    {
                        RaiseWarningReportedEvent(
                            detail1.File,
                            string.Format(
                                "エントリ名を除いて、アーカイブに含まれるエントリが全て別のアーカイブとほぼ等しいです。(CRCの一致) どちらかに同じ内容のファイルが重複して存在しているかもしれません。: \"{0}\"",
                                detail2.File.FullName));
                    }
                }
                else
                {
                    RaiseWarningReportedEvent(
                        detail1.File,
                        string.Format(
                            "アーカイブに含まれるエントリがすべて別のアーカイブにも含まれている可能性があります。(CRCの一致) エントリ名は異なるかもしれません。: \"{0}\"",
                            detail2.File.FullName));
                }
            }
            else
            {
                if (result2 == EntriesContaininfType.ContansAndAllDataMatched)
                {
                    RaiseWarningReportedEvent(
                        detail2.File,
                        string.Format(
                            "アーカイブに含まれるエントリがすべて別のアーカイブにも含まれています。(データの一致) エントリ名は異なるかもしれません。: \"{0}\"",
                            detail1.File.FullName));
                }
                else if (result2 == EntriesContaininfType.ContansAndOnlyCrcMatched)
                {
                    RaiseWarningReportedEvent(
                        detail2.File,
                        string.Format(
                            "アーカイブに含まれるエントリがすべて別のアーカイブにも含まれている可能性があります。(CRCの一致) エントリ名は異なるかもしれません。: \"{0}\"",
                            detail1.File.FullName));
                }
                else
                {
                    // NOP
                }
            }
        }

        private EntriesContaininfType CheckIfEntriesAreIncludedInOtherEntries(ZipArchiveFileDetail detail1, ZipArchiveFileDetail detail2, Action progressUpdater)
        {
            if (detail1.Crcs.NotAll(crc => detail2.ContainsCrc(crc)))
                return EntriesContaininfType.NotContains;
            if (detail1.Crcs.NotAll(crc => EqualsArchiveFileEntryOfCrc(detail1, detail2, crc, progressUpdater) == true))
                return EntriesContaininfType.ContansAndOnlyCrcMatched;
            return EntriesContaininfType.ContansAndAllDataMatched;
        }

        private bool? EqualsArchiveFileEntryOfCrc(ZipArchiveFileDetail detail1, ZipArchiveFileDetail detail2, Int64 crc, Action progressUpdater)
        {
            try
            {
                SafetyCancellationCheck();
                UpdateProgress();
                var entries1 = detail1.FindEntriesByCrc(crc);
                if (!entries1.IsSingle())
                    return null;
                var entries2 = detail2.FindEntriesByCrc(crc);
                if (!entries2.IsSingle())
                    return null;
                return
                    detail1.GetInputStream(entries1.Single())
                    .StreamBytesEqual(detail2.GetInputStream(entries2.Single()), progress: new Progress<UInt64>(_ => UpdateProgress()));
            }
            finally
            {
                progressUpdater();
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

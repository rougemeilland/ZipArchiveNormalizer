﻿using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Utility;
using Utility.FileWorker;
using ZipUtility;


namespace ZipArchiveNormalizer.Phase3
{
    class Phase3Worker
        : FileWorker, IPhaseWorker
    {
        /// <summary>
        /// エントリのパス、サイズ、オフセット、CRCがすべて一致しているかどうかを調べる <see cref="IEqualityComparer{ZipArchiveEntry}"/> の実装
        /// </summary>
        private class ZipEntryEasilyEqualityComparer
            : IEqualityComparer<ZipArchiveEntry>
        {
            private bool _compareEntryName;

            public ZipEntryEasilyEqualityComparer(bool compareEntryName)
            {
                _compareEntryName = compareEntryName;
            }

            public bool Equals(ZipArchiveEntry x, ZipArchiveEntry y)
            {
                if (x == null)
                    return y == null;
                else if (y == null)
                    return false;
                else
                {
                    // compareEntryName が true の場合はエントリ名の違いを無視するが、
                    // エントリ名が違う場合にはオフセットの一致も期待できないので、オフセットも無視する
                    return
                        (_compareEntryName == false || string.Equals(x.FullName, y.FullName, StringComparison.InvariantCultureIgnoreCase)) &&
                        (_compareEntryName == false || x.Offset.Equals(y.Offset)) &&
                        x.Size.Equals(y.Size) &&
                        x.Crc.Equals(y.Crc);
                }
            }

            public int GetHashCode(ZipArchiveEntry obj)
            {
                return
                    obj.FullName.GetHashCode() ^
                    obj.Offset.GetHashCode() ^
                    obj.Size.GetHashCode() ^
                    obj.Crc.GetHashCode();
            }
        }

        /// <summary>
        /// データの内容が同じ二つのアーカイブファイルのうちどちらの重要度が高いかを比較する <see cref="IComparable{ZipArchiveEntriesOfZipFile}"/> の実装
        /// </summary>
        private class ArchiveFileImportanceComparer
            : IComparer<ZipArchiveEntriesOfZipFile>
        {
            private static IComparer<FileInfo> _fileImportanceComparer;
            private static Regex _lessImportantEntryNamePattern ;

            static ArchiveFileImportanceComparer()
            {
                _fileImportanceComparer = new FileImportanceComparer();
                _lessImportantEntryNamePattern = new Regex(@"^p[0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            public int Compare(ZipArchiveEntriesOfZipFile x, ZipArchiveEntriesOfZipFile y)
            {
                if (x == null)
                    return y == null ? 0 : -1;
                else if (y == null)
                    return 1;
                else
                    return CompareZipArchiveEntriesOfZipFile(x, y);
            }

            private int CompareZipArchiveEntriesOfZipFile(ZipArchiveEntriesOfZipFile x, ZipArchiveEntriesOfZipFile y)
            {
                // 付加されている拡張属性の数が多い方を残す。
                // それが同じなら、エントリ名の長さの合計が多い方を残す
                // それが同じなら、エントリの更新日時の最大値が低い方のファイルを残す。
                // それが同じなら、エントリの作成日時の最大値が低い方のファイルを残す。
                // それが同じなら、アーカイブファイル名が長い方を残す。
                int c;

                // アーカイブファイルに含まれているエントリの拡張属性の種類の数の合計が多い方のファイルが重要
                if ((c =
                    x.ZipEntries.Sum(entry => (long)entry.ExtraFields.Count)
                    .CompareTo(y.ZipEntries.Sum(entry => (long)entry.ExtraFields.Count))) != 0)
                    return c;

                // エントリのファイル名全てが"p<数字>"で始まるわけではない方のファイルが優先
                if ((c =
                    x.ZipEntries.NotAll(entry => _lessImportantEntryNamePattern.IsMatch(Path.GetFileName(entry.FullName)))
                    .CompareTo(y.ZipEntries.NotAll(entry => _lessImportantEntryNamePattern.IsMatch(Path.GetFileName(entry.FullName))))) != 0)
                    return c;

                // アーカイブファイルに含まれているエントリの名前の長さの合計が多い方のファイルが重要
                if ((c =
                    x.ZipEntries.Sum(entry => (long)entry.FullName.Length)
                    .CompareTo(y.ZipEntries.Sum(entry => (long)entry.FullName.Length))) != 0)
                    return c;

                // アーカイブファイルに含まれているエントリの最新更新日付が古い方のファイルが重要
                if ((c =
                    x.ZipEntries.Max(entry => entry.LastWriteTimeUtc.Ticks)
                    .CompareTo(y.ZipEntries.Max(entry => entry.LastWriteTimeUtc.Ticks))) != 0)
                    return -c;

                // アーカイブファイルに含まれているエントリの最新作成日付が古い方のファイルが重要
                if ((c =
                    x.ZipEntries.Max(entry => (entry.CreationTimeUtc ?? DateTime.MaxValue).Ticks)
                    .CompareTo(y.ZipEntries.Max(entry => (entry.CreationTimeUtc ?? DateTime.MaxValue).Ticks))) != 0)
                    return -c;

                return _fileImportanceComparer.Compare(x.ZipFile, y.ZipFile);
            }
        }

        private class ZipArchiveEntriesOfZipFile
        {
            public ZipArchiveEntriesOfZipFile(FileInfo zipFile)
            {
                ZipFile = zipFile;
                ZipEntries = zipFile.EnumerateZipArchiveEntry().ToList().AsReadOnly();
            }

            public FileInfo ZipFile { get; }
            public IReadOnlyCollection<ZipArchiveEntry> ZipEntries { get; }
        }

        private static IEqualityComparer<ZipArchiveEntry> _zipEntryMoreEasilyEqualityComparer;
        private static IEqualityComparer<ZipArchiveEntry> _zipEntryEasilyEqualityComparer;
        private static IComparer<ZipArchiveEntriesOfZipFile> _archiveFileImportanceComparer;
        private Func<FileInfo, bool> _isBadFileSelecter;

        public event EventHandler<BadFileFoundEventArgs> BadFileFound;

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

        protected override void ExecuteWork(IEnumerable<FileInfo> sourceFiles, IFileWorkerExecutionResult previousWorkerResult)
        {
            UpdateProgress();

            var targetArchiveFiles =
                sourceFiles
                .Where(file =>
                    _isBadFileSelecter(file) == false &&
                    file.Extension.IsAnyOf(".zip", ".epub", StringComparison.InvariantCultureIgnoreCase))
                .ToReadOnlyCollection();
            SetToSourceFiles(targetArchiveFiles);

            var destinationFiles = new Dictionary<string, FileInfo>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var sourceFile in targetArchiveFiles)
                destinationFiles[sourceFile.FullName] = sourceFile;

            var totalCount = (long)targetArchiveFiles.Count * 2;
            var counrOfDone = 0L;
            var groupedArchiveFiles =
                targetArchiveFiles
                .Select(file =>
                {
                    if (IsRequestedToCancel)
                        throw new OperationCanceledException();
                    var o = new ZipArchiveEntriesOfZipFile(file);
                    UpdateProgress(totalCount, Interlocked.Increment(ref counrOfDone));
                    return o;
                })
                .Select(archiveFile => new { archiveFile, entriesCount = archiveFile.ZipEntries.Count })
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
                                            string.Join(", ", archiveFiles.Select(file => string.Format("\"{0}\"", file.ZipFile.FullName))),
                                            ex.Message,
                                            ex.StackTrace),
                                        ex);
                            }
                        }
                        UpdateProgress(totalCount, Interlocked.Add(ref counrOfDone, item.Count));
                    });
                if (cancellationTokenSource.IsCancellationRequested)
                    throw new OperationCanceledException();
            }
            if (IsRequestedToCancel)
                throw new OperationCanceledException();
            UpdateProgress();
            foreach (var destinationFile in destinationFiles.Values)
                AddToDestinationFiles(destinationFile);
            UpdateProgress();
        }

        private IEnumerable<ZipArchiveEntriesOfZipFile> DeleteAndExcludeUselessArchiveFile(IEnumerable<ZipArchiveEntriesOfZipFile> zipArchives, Action<FileInfo> onDelete)
        {
            FileInfo fileToDelete = null;
            FileInfo otherFile = null;
            var zipArchiveFileInfo1 = zipArchives.First();
            foreach (var zipArchiveFileInfo2 in zipArchives.Skip(1))
            {
                if (IsRequestedToCancel)
                    throw new OperationCanceledException();
                UpdateProgress();

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
                            fileToDelete = zipArchiveFileInfo2.ZipFile;
                            otherFile = zipArchiveFileInfo1.ZipFile;
                        }
                        else
                        {
                            fileToDelete = zipArchiveFileInfo1.ZipFile;
                            otherFile = zipArchiveFileInfo2.ZipFile;
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
                                zipArchiveFileInfo2.ZipFile,
                                string.Format("エントリ名を除いて、エントリのデータがエントリの順番も含めて全て同一の別のアーカイブファイルが存在します。: \"{0}\"",
                                    zipArchiveFileInfo1.ZipFile.FullName));
                        }
                        else
                        {
                            RaiseWarningReportedEvent(
                                zipArchiveFileInfo1.ZipFile,
                                string.Format("エントリ名を除いて、エントリのデータがエントリの順番も含めて全て同一の別のアーカイブファイルが存在します。: \"{0}\"",
                                    zipArchiveFileInfo2.ZipFile.FullName));
                        }
                    }
                }
                else
                {
                    // NOP
                }
            }
            if (fileToDelete != null)
            {
#if DEBUG
                if (string.Equals(fileToDelete.FullName, otherFile.FullName, StringComparison.InvariantCultureIgnoreCase))
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
                    .Where(item => !string.Equals(item.ZipFile.FullName, fileToDelete.FullName, StringComparison.InvariantCultureIgnoreCase));
            }
            else
            {
                return
                    zipArchives.Skip(1);
            }
        }

        private bool EntriesEqualMoreEasily(ZipArchiveEntriesOfZipFile archiveFile1, ZipArchiveEntriesOfZipFile archiveFile2)
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

        private bool EntriesEqualEasily(ZipArchiveEntriesOfZipFile archiveFile1, ZipArchiveEntriesOfZipFile archiveFile2)
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
                using (var zipFile1 = new ZipFile(archiveFile1.ZipFile.FullName))
                using (var zipFile2 = new ZipFile(archiveFile2.ZipFile.FullName))
                {
                    return
                        archiveFile1.ZipEntries.Count == archiveFile2.ZipEntries.Count &&
                        archiveFile1.ZipEntries
                        .Zip(archiveFile2.ZipEntries, (entry1, entry2) => new { entry1, entry2 })
                        .All(item =>
                            zipFile1.GetInputStream(item.entry1)
                            .StreamBytesEqual(zipFile2.GetInputStream(item.entry2)));
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void RaiseBadFileFoundEvent(FileInfo targetFile)
        {
            if (BadFileFound != null)
                BadFileFound(this, new BadFileFoundEventArgs(targetFile));
        }
    }
}
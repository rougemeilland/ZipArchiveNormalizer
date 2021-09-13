using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Utility;
using Utility.FileWorker;
using Utility.IO;

namespace ZipArchiveNormalizer.Phase2
{
    class Phase2Worker
        : FileWorker, IPhaseWorker
    {
        private static IComparer<FileInfo> _archiveFileImportanceComparer;
        private Func<FileInfo, bool> _isBadFileSelecter;

        public event EventHandler<BadFileFoundEventArgs> BadFileFound;

        static Phase2Worker()
        {
            _archiveFileImportanceComparer = new FileImportanceComparer();
        }

        public Phase2Worker(IWorkerCancellable canceller, Func<FileInfo, bool> isBadFileSelecter)
            : base(canceller)
        {
            _isBadFileSelecter = isBadFileSelecter;
        }

        public override string Description => "同一内容のファイル(.zip/.pdf/.epub)がないか調べます。";

        protected override void ExecuteWork(IEnumerable<FileInfo> sourceFiles, IFileWorkerExecutionResult previoousWorkerResult)
        {
            UpdateProgress();

            var targetSourceFiles =
                sourceFiles
                .Where(file =>
                    _isBadFileSelecter(file) == false &&
                    file.Extension.IsAnyOf(".zip", ".epub", ".pdf", StringComparison.OrdinalIgnoreCase))
                .ToReadOnlyCollection();
            SetToSourceFiles(targetSourceFiles);

            var destinationFiles = new Dictionary<string, FileInfo>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var sourceFile in targetSourceFiles)
                destinationFiles[sourceFile.FullName] = sourceFile;

            var groupedArchiveFiles =
                targetSourceFiles
                .GroupBy(file => file.Length)
                .Select(g => new { length = g.Key, files = g.ToReadOnlyCollection() })
                .Where(item => item.files.Count > 1);

            var totalCount = groupedArchiveFiles.Sum(item => item.files.Count) * 2;
            var counrOfDone = 0L;

            var workingList =
                groupedArchiveFiles
                .SelectMany(item =>
                    item.files
                    .Select(file =>
                    {
                        SafetyCancellationCheck();
                        var result =
                            new
                            {
                                file,
                                crc = file.CalculateCrc32(),
                            };
                        UpdateProgress(totalCount, Interlocked.Increment(ref counrOfDone));
                        return result;
                    })
                    .GroupBy(item2 => item2.crc)
                    .Select(g => new
                    {
                        item.length,
                        crc = g.Key,
                        files = g.Select(item2 => item2.file).ToList(),
                    }))
                .ToList();
            UpdateProgress(totalCount, counrOfDone);
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                workingList
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
                        if (item.files.Count > 1)
                        {
                            try
                            {
                                DeleteSameFiles(item.files, deletedFile => destinationFiles.Remove(deletedFile.FullName));
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
                                            string.Join(", ", item.files.Select(file => string.Format("\"{0}\"", file.FullName))),
                                            ex.Message,
                                            ex.StackTrace),
                                        ex);
                            }
                        }
                        UpdateProgress(totalCount, Interlocked.Add(ref counrOfDone, item.files.Count));
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

        private void DeleteSameFiles(IEnumerable<FileInfo> filesOfSameLength, Action<FileInfo> onDeleteFile)
        {
            var fileInfos = filesOfSameLength.ToReadOnlyCollection();
            while (fileInfos.Count >= 2)
            {
                SafetyCancellationCheck();
                UpdateProgress();
                fileInfos = DeleteAndExcludeUselessFile(fileInfos, onDeleteFile).ToReadOnlyCollection();
            }
        }

        private IEnumerable<FileInfo> DeleteAndExcludeUselessFile(IEnumerable<FileInfo> files, Action<FileInfo> onDeleteFile)
        {
            FileInfo fileToDelete = null;
            FileInfo otherFile = null;
            var fileInfo1 = files.First();
            foreach (var fileInfo2 in files.Skip(1))
            {
                SafetyCancellationCheck();
                UpdateProgress();
                if (fileInfo1.OpenRead().StreamBytesEqual(fileInfo2.OpenRead(), progressNotification: count => UpdateProgress()))
                {
                    // ファイルの内容が一致している場合

                    // ファイルの重要度を比較する
                    if (_archiveFileImportanceComparer.Compare(fileInfo1, fileInfo2) > 0)
                    {
                        fileToDelete = fileInfo2;
                        otherFile = fileInfo1;
                    }
                    else
                    {
                        fileToDelete = fileInfo1;
                        otherFile = fileInfo2;
                    }
                    break;
                }
            }
            if (fileToDelete != null)
            {
#if DEBUG
                if (string.Equals(fileToDelete.FullName, otherFile.FullName, StringComparison.OrdinalIgnoreCase))
                    throw new Exception();
#endif
                RaiseInformationReportedEvent(
                    fileToDelete,
                    string.Format("内容が同じ別のファイルを削除します。: \"{0}\"",
                    otherFile.FullName));
                fileToDelete.SendToRecycleBin();
                onDeleteFile(fileToDelete);
                IncrementChangedFileCount();
                return
                    files
                    .Where(file => !string.Equals(file.FullName, fileToDelete.FullName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return
                    files.Skip(1);
            }
        }

        private void RaiseBadFileFoundEvent(FileInfo targetFile)
        {
            if (BadFileFound != null)
                BadFileFound(this, new BadFileFoundEventArgs(targetFile));
        }
    }
}
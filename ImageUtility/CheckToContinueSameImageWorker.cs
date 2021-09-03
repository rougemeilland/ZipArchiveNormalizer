using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Utility;
using Utility.FileWorker;

namespace ImageUtility
{
    public class CheckToContinueSameImageWorker
        : FileWorker
    {
        private class ExecutionResult
            : IFileWorkerExecutionResult
        {
            public ExecutionResult(IEnumerable<FileInfo> sourceFiles, IEnumerable<FileInfo> destinationFiles, long totalChangedFileCount)
            {
                SourceFiles = sourceFiles.ToReadOnlyCollection();
                DestinationFiles = destinationFiles.ToReadOnlyCollection();
                TotalChangedFileCount = totalChangedFileCount;
            }

            public IReadOnlyCollection<FileInfo> SourceFiles { get; }

            public IReadOnlyCollection<FileInfo> DestinationFiles { get; }

            public long TotalChangedFileCount { get; }
        }

        public CheckToContinueSameImageWorker(IWorkerCancellable canceller)
            : base(canceller)
        {
        }

        public override string Description => "同一の画像が連続していないかチェックします。";

        protected override void ExecuteWork(IEnumerable<FileInfo> sourceFiles, IFileWorkerExecutionResult previousWorkerResult)
        {
            var totalCount = -1L;
            var countOfDone = -1L;
            var copyOfSourceFile =
                previousWorkerResult.DestinationFiles
                .ToReadOnlyCollection();
            SetToSourceFiles(copyOfSourceFile);

            totalCount = copyOfSourceFile.Count;
            countOfDone = 0;
            UpdateProgress(totalCount, countOfDone);
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                copyOfSourceFile
                    .GroupBy(file => file.DirectoryName)
                    .Select(g => new
                    {
                        directory = g.First().Directory,
                        files =
                            g.OrderBy(file => file.Name, StringComparer.CurrentCultureIgnoreCase)
                            .ToArray()
                            .AsReadOnly()
                    })
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
                            UpdateProgress(totalCount, Interlocked.Increment(ref countOfDone));
                            AddToDestinationFiles(item.files[0]);
                            if (item.files.Length > 1)
                            {
                                for (int index = 0; index < item.files.Length - 1; index++)
                                {
                                    try
                                    {
                                        if (item.files[index].Length == item.files[index + 1].Length &&
                                            item.files[index].OpenRead().StreamBytesEqual(item.files[index + 1].OpenRead(), progressNotification: () => UpdateProgress()))
                                        {
                                            try
                                            {
                                                ActionForSameImageFile(item.files[index], item.files[index + 1]);
                                            }
                                            catch (Exception)
                                            {
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        UpdateProgress(totalCount, Interlocked.Increment(ref countOfDone));
                                        AddToDestinationFiles(item.files[index + 1]);
                                    }
                                }
                            }
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
                                        "並列処理中に例外が発生しました。: 処理クラス={0}, 対象ディレクトリ=\"{1}\", message=\"{2}\", スタックトレース=>{3}",
                                        GetType().FullName,
                                        item.directory.FullName,
                                        ex.Message,
                                        ex.StackTrace),
                                    ex);
                        }
                    });
                if (cancellationTokenSource.IsCancellationRequested)
                    throw new OperationCanceledException();
            }
            SafetyCancellationCheck();
        }

        private void ActionForSameImageFile(FileInfo imageFile1, FileInfo imageFile2)
        {
            RaiseWarningReportedEvent(
                string.Format(
                    "同じ内容のファイルが連続しています。: directory=\"{0}\", files={{\"{1}\", \"{2}\"}}",
                    imageFile1.DirectoryName,
                    imageFile1.Name,
                    imageFile2.Name));
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utility;
using Utility.FileWorker;
using Utility.IO;

namespace ImageUtility
{
    public class CheckToContinueSameImageWorker
        : FileWorker
    {
        private class ExecutionResult
            : IFileWorkerExecutionResult
        {
            public ExecutionResult(IEnumerable<FileInfo> sourceFiles, IEnumerable<FileInfo> destinationFiles, Int64 totalChangedFileCount)
            {
                SourceFiles = sourceFiles.ToReadOnlyCollection();
                DestinationFiles = destinationFiles.ToReadOnlyCollection();
                TotalChangedFileCount = totalChangedFileCount;
            }

            public IReadOnlyCollection<FileInfo> SourceFiles { get; }

            public IReadOnlyCollection<FileInfo> DestinationFiles { get; }

            public Int64 TotalChangedFileCount { get; }
        }

        public CheckToContinueSameImageWorker(IWorkerCancellable canceller)
            : base(canceller)
        {
        }

        public override string Description => "同一の画像が連続していないかチェックします。";

        protected override void ExecuteWork(IEnumerable<FileInfo> sourceFiles, IFileWorkerExecutionResult? previousWorkerResult)
        {
            if (sourceFiles is null)
                throw new ArgumentNullException(nameof(sourceFiles));

            var copyOfSourceFile = previousWorkerResult?.DestinationFiles ?? sourceFiles.ToReadOnlyCollection();
            SetToSourceFiles(copyOfSourceFile);

            var totalCount = (UInt64)copyOfSourceFile.Count;
            var countOfDone = 0UL;
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
                            AddToDestinationFiles(item.files.Span[0]);
                            if (item.files.Length > 1)
                            {
                                var files = item.files.Span;
                                for (Int32 index = 0; index < item.files.Length - 1; index++)
                                {
                                    try
                                    {
                                        if (files[index].Length == files[index + 1].Length &&
                                            files[index].OpenRead().StreamBytesEqual(files[index + 1].OpenRead(), progress: new Progress<UInt64>(_ => UpdateProgress())))
                                        {
                                            try
                                            {
                                                ActionForSameImageFile(files[index], files[index + 1]);
                                            }
                                            catch (Exception)
                                            {
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        UpdateProgress(totalCount, Interlocked.Increment(ref countOfDone));
                                        AddToDestinationFiles(files[index + 1]);
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
                                        item.directory?.FullName ?? ".",
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

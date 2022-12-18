using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utility;
using Utility.FileWorker;
using Utility.IO;

namespace ImageUtility
{
    public class ImageFileSummaryEnumerationWorker
        : FileWorker
    {
        private class ImageFileSize
            : IImageFileSize
        {
            public ImageFileSize(FileInfo imageFile, UInt32 imageFileCrc, Size imageSize)
            {
                ImageFile = imageFile;
                ImageFileCrc = imageFileCrc;
                ImageSize = imageSize;
            }

            public FileInfo ImageFile { get; }
            public UInt32 ImageFileCrc { get; }
            public Size ImageSize { get; }

        }

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

        public ImageFileSummaryEnumerationWorker(IWorkerCancellable canceller)
            : base(canceller)
        {
        }

        public override string Description => "不自然にサイズが異なる画像が混じっていないか調べます。";

        protected override void ExecuteWork(IEnumerable<FileInfo> sourceFiles, IFileWorkerExecutionResult? previousActionResult)
        {
            if (sourceFiles is null)
                throw new ArgumentNullException(nameof(sourceFiles));

            var imageFiles =
                previousActionResult?.DestinationFiles ?? sourceFiles.ToReadOnlyCollection();
            SetToSourceFiles(imageFiles);

            var totalCount = (UInt64)imageFiles.Count;
            var countOfDone = 0UL;
            UpdateProgress(totalCount, countOfDone);
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                imageFiles
                    .GroupBy(imageFile => imageFile.DirectoryName)
                    .AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount)
                    .WithCancellation(cancellationTokenSource.Token)
                    .ForAll(g =>
                    {
                        if (IsRequestedToCancel)
                        {
                            cancellationTokenSource.Cancel();
                            return;
                        }
                        try
                        {
                            var directory = g.First().Directory;
                            if (directory is not null)
                            {
                                var summaries =
                                    g
                                    .Select(file =>
                                    {
                                        try
                                        {
                                            SafetyCancellationCheck();
                                            return new ImageFileSize(file, file.CalculateCrc32().Crc, file.GetImageSize());
                                        }
                                        finally
                                        {
                                            UpdateProgress(totalCount, Interlocked.Increment(ref countOfDone));
                                        }
                                    })
                                    .ToReadOnlyCollection();
                                ActionForImageFileDirectory(new ImageFileDirectorySummary(directory, summaries.ToReadOnlyCollection()));
                                foreach (var summary in summaries)
                                    AddToDestinationFiles(summary.ImageFile);
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
                                        g.Key,
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

        private void ActionForImageFileDirectory(ImageFileDirectorySummary summary)
        {
            if (summary.ImageFileOfMaximumWidth.ImageSize.Width * 10 > summary.ImageFileOfMinimumWidth.ImageSize.Width * 11)
            {
                RaiseWarningReportedEvent(
                    string.Format(
                        "画像の幅に差がありすぎます。: directory=\"{0}\", files={{\"{1}\"({2}pixel), \"{3}\"({4}pixel)}}",
                        summary.Directory.FullName,
                        summary.ImageFileOfMinimumWidth.ImageFile.Name,
                        summary.ImageFileOfMinimumWidth.ImageSize.Width,
                        summary.ImageFileOfMaximumWidth.ImageFile.Name,
                        summary.ImageFileOfMaximumWidth.ImageSize.Width));
            }
            if (summary.ImageFileOfMaximumHeight.ImageSize.Width * 10 > summary.ImageFileOfMinimumHeight.ImageSize.Width * 11)
            {
                RaiseWarningReportedEvent(
                    string.Format(
                        "画像の高さに差がありすぎます。: directory=\"{0}\", files={{\"{1}\"({2}pixel), \"{3}\"({4}pixel)}}",
                        summary.Directory.FullName,
                        summary.ImageFileOfMinimumHeight.ImageFile.Name,
                        summary.ImageFileOfMinimumHeight.ImageSize.Height,
                        summary.ImageFileOfMaximumHeight.ImageFile.Name,
                        summary.ImageFileOfMaximumHeight.ImageSize.Height));
            }
        }
    }
}

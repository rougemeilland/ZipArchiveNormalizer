using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Utility.FileWorker
{
    public abstract class FileWorkerFromMainArgument
        : FileWorker
    {
        private class ActionParameter
            : IFileWorkerActionParameter
        {
            public ActionParameter(int fileIndexOnSameDirectory, IFileWorkerActionDirectoryParameter directoryParameter, IFileWorkerActionFileParameter fileParameter)
            {
                FileIndexOnSameDirectory = fileIndexOnSameDirectory;
                DirectoryParameter = directoryParameter;
                FileParameter = fileParameter;
            }

            public int FileIndexOnSameDirectory { get; }

            public IFileWorkerActionDirectoryParameter DirectoryParameter { get; }

            public IFileWorkerActionFileParameter FileParameter { get; }
        }

        private class EmptyDirectoryParameter
            : IFileWorkerActionDirectoryParameter
        {
        }

        private class EmptyFileParameter
            : IFileWorkerActionFileParameter
        {
        }

        private class FileWorkerItem
        {
            public FileInfo SourceFile { get; set; }
            public int Index { get; set; }
            public IFileWorkerActionFileParameter FileParameter { get; set; }
        }

        private class DirectoryWorkerItem
        {
            public DirectoryInfo SourceFileDirectory { get; set; }
            public IFileWorkerActionDirectoryParameter DirectoryParameter { get; set; }
            public ICollection<FileWorkerItem> FileItems { get; set; }
        }

        private FileWorkerConcurrencyMode _concurrencyMode;
        private int _suffixCount;
        private string _previousFileName;

        protected FileWorkerFromMainArgument(IWorkerCancellable canceller, FileWorkerConcurrencyMode concurrencyMode)
            : base(canceller)
        {
            _concurrencyMode = concurrencyMode;
            _suffixCount = 0;
            _previousFileName = "";
        }

        protected override void ExecuteWork(IEnumerable<FileInfo> sourceFiles, IFileWorkerExecutionResult previousWorkerResult)
        {
            var sourceFileItems = GetWorkingSource(sourceFiles);
            SetToSourceFiles(sourceFileItems.SelectMany(sourceFileItem => sourceFileItem.FileItems).Select(fileItem => fileItem.SourceFile));

            if (IsRequestedToCancel)
                throw new OperationCanceledException();

            var totalCount = sourceFileItems.Sum(item => item.FileItems.Count);
            long countOfDone = 0;
            UpdateProgress(totalCount, countOfDone);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {

                switch (_concurrencyMode)
                {
                    case FileWorkerConcurrencyMode.ParallelProcessingForEachFile:
                        sourceFileItems
                            .SelectMany(item =>
                                item.FileItems
                                .Select(item2 => new
                                {
                                    item2.SourceFile,
                                    item2.Index,
                                    item.DirectoryParameter,
                                    item2.FileParameter,
                                }))
                            .AsParallel()
                            .WithDegreeOfParallelism(Environment.ProcessorCount)
                            .WithCancellation(cancellationTokenSource.Token)
                            .ForAll(parameter =>
                            {
                                if (IsRequestedToCancel)
                                {
                                    cancellationTokenSource.Cancel();
                                    return;
                                }
                                try
                                {
                                    ExecuteAction(
                                        parameter.SourceFile,
                                        parameter.Index,
                                        parameter.DirectoryParameter,
                                        parameter.FileParameter,
                                        totalCount,
                                        ref countOfDone);
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
                                                "並列処理中に例外が発生しました。: 処理クラス={0}, 対象ファイル=\"{1}\", message=\"{2}\", スタックトレース=>{3}",
                                                GetType().FullName,
                                                parameter.SourceFile.FullName,
                                                ex.Message,
                                                ex.StackTrace),
                                            ex);
                                }
                            });
                        break;
                    case FileWorkerConcurrencyMode.ParallelProcessingForEachDirectory:
                        sourceFileItems
                            .AsParallel()
                            .WithDegreeOfParallelism(Environment.ProcessorCount)
                            .WithCancellation(cancellationTokenSource.Token)
                            .ForAll(parameter =>
                            {
                                if (IsRequestedToCancel)
                                {
                                    cancellationTokenSource.Cancel();
                                    return;
                                }
                                var fileNames = parameter.FileItems.Select(item => item.SourceFile.Name).ToList();
                                foreach (var fileItem in parameter.FileItems)
                                {
                                    try
                                    {
                                        ExecuteAction(fileItem.SourceFile, fileItem.Index, parameter.DirectoryParameter, fileItem.FileParameter, totalCount, ref countOfDone);
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
                                                    "並列処理中に例外が発生しました。: 処理クラス={0}, 対象ファイル=\"{1}\", message=\"{2}\", スタックトレース=>{3}",
                                                    GetType().FullName,
                                                    fileItem.SourceFile.FullName,
                                                    ex.Message,
                                                    ex.StackTrace),
                                                ex);
                                    }
                                }
                            });
                        break;
                    default:
                        throw new Exception();
                }
                if (cancellationTokenSource.IsCancellationRequested)
                    throw new OperationCanceledException();
            }
            if (IsRequestedToCancel)
                throw new OperationCanceledException();
        }

        protected virtual IFileWorkerActionFileParameter IsMatchFile(FileInfo sourceFile)
        {
            return new EmptyFileParameter();
        }

        protected virtual IFileWorkerActionDirectoryParameter IsMatchDirectory(DirectoryInfo directory, IEnumerable<string> fileNames)
        {
            return new EmptyDirectoryParameter();
        }

        protected virtual IComparer<FileInfo> FileComparer => null;

        protected virtual IEqualityComparer<DirectoryInfo> DirectoryEqualityComparer =>
            new CustomizableEqualityComparer<DirectoryInfo>(
                (dir1, dir2) => StringComparer.OrdinalIgnoreCase.Equals(dir1.FullName, dir2.FullName),
                dir => dir.FullName.ToUpper().GetHashCode());

        protected abstract void ActionForFile(FileInfo sourceFile, IFileWorkerActionParameter parameter);

        protected string GetUniqueFileNameFromTimeStamp()
        {
            return GetUniqueFileNameFromTimeStamp(DateTime.Now);
        }

        private IEnumerable<DirectoryWorkerItem> GetWorkingSource(IEnumerable<FileInfo> sourceFiles)
        {
            return
                sourceFiles
                .Select(sourceFile =>
                {
                    if (IsRequestedToCancel)
                        throw new OperationCanceledException();
                    try
                    {
                        return new { sourceFile = sourceFile, fileParameter = IsMatchFile(sourceFile) };
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                    finally
                    {
                        UpdateProgress();
                    }
                })
                .Where(item => item != null && item.fileParameter != null)
                .GroupBy(item => item.sourceFile.Directory, DirectoryEqualityComparer)
                .Select(g =>
                {
                    var directory = g.First().sourceFile.Directory;
                    var groupedItems = g.AsEnumerable();
                    var fileComparer = FileComparer;
                    if (fileComparer != null)
                        groupedItems = groupedItems.OrderBy(groupedItem => groupedItem.sourceFile, fileComparer);
                    return new
                    {
                        directory,
                        fileItems =
                            groupedItems
                            .Select((item, index) => new FileWorkerItem { SourceFile = item.sourceFile, Index = index, FileParameter = item.fileParameter })
                            .ToList()
                    };
                })
                .Select(item =>
                {
                    if (IsRequestedToCancel)
                        throw new OperationCanceledException();
                    try
                    {
                        return new DirectoryWorkerItem { SourceFileDirectory = item.directory, FileItems = item.fileItems, DirectoryParameter = IsMatchDirectory(item.directory, item.fileItems.Select(fileItem => fileItem.SourceFile.Name)) };
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                    finally
                    {
                        UpdateProgress();
                    }
                })
                .Where(item => item != null && item.DirectoryParameter != null)
                .ToList();
        }

        private void ExecuteAction(FileInfo sourceFile, int index, IFileWorkerActionDirectoryParameter directoryParameter, IFileWorkerActionFileParameter fileParameter, int totalCount, ref long countOfDone)
        {
            if (IsRequestedToCancel)
                throw new OperationCanceledException();
            try
            {
                // 呼び出された関数で FileInfo.MoveTo などを実行すると呼び出し元の FileInfo オブジェクトも改変されてしまうため、
                // FileInfo オブジェクトは複製してから渡す
                ActionForFile(
                    new FileInfo(sourceFile.FullName),
                    new ActionParameter(index, directoryParameter, fileParameter));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
            }
            finally
            {
                UpdateProgress(totalCount, Interlocked.Increment(ref countOfDone));
            }
        }

        private string GetUniqueFileNameFromTimeStamp(DateTime now)
        {
            if (now.Kind == DateTimeKind.Unspecified)
                throw new ArgumentException();
            now = now.ToLocalTime();
            var newFileName = string.Format("{0:D4}{1:D2}{2:D2}{3:D2}{4:D2}{5:D2}{6:D3}", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond);
            lock (this)
            {
                if (_previousFileName == newFileName)
                    ++_suffixCount;
                else
                {
                    _suffixCount = 0;
                    _previousFileName = newFileName;
                }
                return newFileName + _suffixCount.ToString("D4");
            }
        }
    }
}
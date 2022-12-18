using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.FileWorker
{
    public abstract class FileWorkerFromMainArgument
        : FileWorker
    {
        private class ActionParameter
            : IFileWorkerActionParameter
        {
            public ActionParameter(Int32 fileIndexOnSameDirectory, IFileWorkerActionDirectoryParameter directoryParameter, IFileWorkerActionFileParameter fileParameter)
            {
                FileIndexOnSameDirectory = fileIndexOnSameDirectory;
                DirectoryParameter = directoryParameter;
                FileParameter = fileParameter;
            }

            public Int32 FileIndexOnSameDirectory { get; }

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
            public FileWorkerItem(FileInfo sourceFile, Int32 index, IFileWorkerActionFileParameter fileParameter)
            {
                SourceFile = sourceFile;
                Index = index;
                FileParameter = fileParameter;
            }

            public FileInfo SourceFile { get; }
            public Int32 Index { get; }
            public IFileWorkerActionFileParameter FileParameter { get; }
        }

        private class DirectoryWorkerItem
        {
            public DirectoryWorkerItem(DirectoryInfo sourceFileDirectory, IFileWorkerActionDirectoryParameter directoryParameter, ICollection<FileWorkerItem> fileItems)
            {
                SourceFileDirectory = sourceFileDirectory;
                DirectoryParameter = directoryParameter;
                FileItems = fileItems;
            }

            public DirectoryInfo SourceFileDirectory { get; }
            public IFileWorkerActionDirectoryParameter DirectoryParameter { get; }
            public ICollection<FileWorkerItem> FileItems { get; }
        }

        private readonly FileWorkerConcurrencyMode _concurrencyMode;

        private Int32 _suffixCount;
        private string _previousFileName;

        protected FileWorkerFromMainArgument(IWorkerCancellable canceller, FileWorkerConcurrencyMode concurrencyMode)
            : base(canceller)
        {
            _concurrencyMode = concurrencyMode;
            _suffixCount = 0;
            _previousFileName = "";
        }

        protected override void ExecuteWork(IEnumerable<FileInfo> sourceFiles, IFileWorkerExecutionResult? previousWorkerResult)
        {
            if (sourceFiles is null)
                throw new ArgumentNullException(nameof(sourceFiles));

            var sourceFileItems = GetWorkingSource(sourceFiles);
            SetToSourceFiles(sourceFileItems.SelectMany(sourceFileItem => sourceFileItem.FileItems).Select(fileItem => fileItem.SourceFile));
            SafetyCancellationCheck();
            var totalCount = (UInt64)sourceFileItems.Sum(item => item.FileItems.Sum(fileItem => fileItem.SourceFile.Length));
            UInt64 countOfDone = 0;
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
                        throw new InternalLogicalErrorException();
                }
                if (cancellationTokenSource.IsCancellationRequested)
                    throw new OperationCanceledException();
            }
            SafetyCancellationCheck();
        }

        protected virtual IFileWorkerActionFileParameter? IsMatchFile(FileInfo sourceFile)
        {
            if (sourceFile is null)
                throw new ArgumentNullException(nameof(sourceFile));

            return new EmptyFileParameter();
        }

        protected virtual IFileWorkerActionDirectoryParameter? IsMatchDirectory(DirectoryInfo directory, IEnumerable<string> fileNames)
        {
            if (directory is null)
                throw new ArgumentNullException(nameof(directory));
            if (fileNames is null)
                throw new ArgumentNullException(nameof(fileNames));

            return new EmptyDirectoryParameter();
        }

        protected virtual IComparer<FileInfo> FileComparer => StringComparer.OrdinalIgnoreCase.MapComparer<FileInfo, string>(file => file.FullName);

        protected virtual IEqualityComparer<DirectoryInfo?> DirectoryEqualityComparer =>
            new CustomizableEqualityComparer<DirectoryInfo?>(
                (dir1, dir2) => StringComparer.OrdinalIgnoreCase.Equals(dir1?.FullName, dir2?.FullName),
                dir => dir?.FullName.ToUpper().GetHashCode() ?? 0);

        protected abstract void ActionForFile(FileInfo sourceFile, IFileWorkerActionParameter parameter);

        protected string GetUniqueFileNameFromTimeStamp(DateTime dateTime, Int32 index)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
                throw new ArgumentException($"Unexpected {nameof(DateTime.Kind)} value", nameof(dateTime));

            dateTime = dateTime.ToLocalTime();
            var newFileName = string.Format($"{index:D4}{dateTime.Year:D4}{dateTime.Month:D2}{dateTime.Day:D2}{dateTime.Hour:D2}{dateTime.Minute:D2}{dateTime.Second:D2}{dateTime.Millisecond:D3}");
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

        private IEnumerable<DirectoryWorkerItem> GetWorkingSource(IEnumerable<FileInfo> sourceFiles)
        {
            return
                sourceFiles
                .Select(sourceFile =>
                {
                    SafetyCancellationCheck();
                    try
                    {
                        var fileParameter = IsMatchFile(sourceFile);
                        return fileParameter is null ? null : new { sourceFile, fileParameter };
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
                .WhereNotNull()
                .GroupBy(item => item.sourceFile.Directory, DirectoryEqualityComparer)
                .Select(g =>
                {
                    var directory = g.First().sourceFile.Directory;
                    var groupedItems = g.AsEnumerable();
                    var fileComparer = FileComparer;
                    if (fileComparer is not null)
                        groupedItems = groupedItems.OrderBy(groupedItem => groupedItem.sourceFile, fileComparer);
                    if (directory is null)
                        return null;
                    else
                    {
                        return new
                        {
                            directory,
                            fileItems =
                                groupedItems
                                .Select((item, index) => new FileWorkerItem(item.sourceFile, index, item.fileParameter))
                                .ToList()
                        };
                    }
                })
                .WhereNotNull()
                .Select(item =>
                {
                    SafetyCancellationCheck();
                    try
                    {
                        var directoryParameter = IsMatchDirectory(item.directory, item.fileItems.Select(fileItem => fileItem.SourceFile.Name));
                        return directoryParameter is null ? null : new DirectoryWorkerItem(item.directory, directoryParameter, item.fileItems);
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
                .WhereNotNull()
                .ToList();
        }

        private void ExecuteAction(FileInfo sourceFile, Int32 index, IFileWorkerActionDirectoryParameter directoryParameter, IFileWorkerActionFileParameter fileParameter, UInt64 totalCount, ref UInt64 countOfDone)
        {
            var sourceFileSize = (UInt64)sourceFile.Length;
            SafetyCancellationCheck();
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
                UpdateProgress(totalCount, Interlocked.Add(ref countOfDone, sourceFileSize));
            }
        }
    }
}
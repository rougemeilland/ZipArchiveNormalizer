using System;
using System.IO;
using Utility;
using ZipUtility;
using Utility.FileWorker;

namespace ZipArchiveNormalizer.Phase1
{
    class Phase1Worker
        : FileWorkerFromMainArgument, IPhaseWorker
    {
        private Func<FileInfo, bool> _isBadFileSelecter;

        public event EventHandler<BadFileFoundEventArgs> BadFileFound;

        public Phase1Worker(IWorkerCancellable canceller, Func<FileInfo, bool> isBadFileSelecter)
            : base(canceller, FileWorkerConcurrencyMode.ParallelProcessingForEachFile)
        {
            _isBadFileSelecter = isBadFileSelecter;
        }

        public override string Description => "ZIPファイルに最適化を試みます。";

        protected override IFileWorkerActionFileParameter IsMatchFile(FileInfo sourceFile)
        {
            // 拡張子が ".zip", ".epub" のいずれかのファイルのみを対象とする
            return
                _isBadFileSelecter(sourceFile) == false &&
                sourceFile.Extension.IsAnyOf(".zip", ".epub", StringComparison.InvariantCultureIgnoreCase)
                ? DefaultFileParameter
                : null;
        }

        protected override void ActionForFile(FileInfo sourceFile, IFileWorkerActionParameter parameter)
        {
            try
            {
                if (!sourceFile.IsCorrectZipFile())
                {
                    RaiseErrorReportedEvent(sourceFile, "アーカイブファイルが正しくないため無視します。");
                    RaiseBadFileFoundEvent(sourceFile);
                    return;
                }

                EventHandler<FileMessageReportedEventArgs> informationReportedEventHander = (s, e) => RaiseInformationReportedEvent(e.TargetFile, e.Message);
                EventHandler<FileMessageReportedEventArgs> warningReportedEventHander = (s, e) => RaiseWarningReportedEvent(e.TargetFile, e.Message);
                EventHandler<FileMessageReportedEventArgs> errorReportedEventHander = (s, e) => RaiseErrorReportedEvent(e.TargetFile, e.Message);
                EventHandler<ProgressUpdatedEventArgs> progressUpdatedHander = (s, e) => UpdateProgress();
                var entryTree = EntryTree.GetEntryTree(sourceFile);
                entryTree.InformationReported += informationReportedEventHander;
                entryTree.WarningReported += warningReportedEventHander;
                entryTree.ErrorReported += errorReportedEventHander;
                entryTree.ProgressUpdated += progressUpdatedHander;
                try
                {
                    if (entryTree.ContainsAbsoluteEntryPathName())
                    {
                        RaiseErrorReportedEvent(sourceFile, "アーカイブファイルに絶対パスのエントリが含まれているため無視します。");
                        RaiseBadFileFoundEvent(sourceFile);
                        return;
                    }

                    if (entryTree.ContainsDuplicateName())
                    {
                        RaiseErrorReportedEvent(sourceFile, "アーカイブファイルに重複したエントリが含まれているため無視します。");
                        RaiseBadFileFoundEvent(sourceFile);
                        return;
                    }

                    var needToUpdate = entryTree.Normalize();
                    UpdateProgress();

                    if (entryTree.IsEmpty)
                    {
                        sourceFile.Delete();
                        RaiseInformationReportedEvent(sourceFile, "空のアーカイブファイルを削除します。");
                        UpdateProgress();
                        IncrementChangedFileCount();
                    }
                    else if (needToUpdate)
                    {
                        SaveEntryTreeToArchiveFile(sourceFile, entryTree);
                        UpdateProgress();
                        IncrementChangedFileCount();
                        AddToDestinationFiles(sourceFile);
                    }
                    else
                    {
                        UpdateProgress();
                        AddToDestinationFiles(sourceFile);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("アーカイブファイルの処理中に例外が発生しました。: \"{0}\"", sourceFile.FullName), ex);
                }
                finally
                {
                    entryTree.InformationReported -= informationReportedEventHander;
                    entryTree.WarningReported -= warningReportedEventHander;
                    entryTree.ErrorReported -= errorReportedEventHander;
                    entryTree.ProgressUpdated -= progressUpdatedHander;
                }
            }
            catch (IOException)
            {
            }
        }

        private void SaveEntryTreeToArchiveFile(FileInfo sourceFile, EntryTree entryTree)
        {
            var newArchiveFile = new FileInfo(Path.Combine(Path.GetDirectoryName(sourceFile.FullName), "." + Path.GetFileName(sourceFile.FullName) + ".temp"));
            try
            {
                newArchiveFile.Delete();
                entryTree.SaveTo(newArchiveFile);
                sourceFile.SendToRecycleBin();
                // MoveTo メソッドは FileInfo オブジェクトを改変してしまうため、
                // 複製してから呼び出している
                new FileInfo(newArchiveFile.FullName).MoveTo(sourceFile.FullName);
#if DEBUG

                if (newArchiveFile.Exists)
                    throw new Exception();
#endif
            }
            catch (Exception)
            {
                newArchiveFile.Delete();
                throw;
            }
            finally
            {
#if DEBUG
                if (newArchiveFile.Exists)
                    throw new Exception();
#endif
            }
        }

        private void RaiseBadFileFoundEvent(FileInfo targetFile)
        {
            if (BadFileFound != null)
                BadFileFound(this, new BadFileFoundEventArgs(targetFile));
        }
    }
}
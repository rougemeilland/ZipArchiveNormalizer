﻿using System;
using System.IO;
using Utility;
using Utility.FileWorker;
using ZipUtility;

namespace ZipArchiveNormalizer.Phase1
{
    class Phase1Worker
        : FileWorkerFromMainArgument, IPhaseWorker
    {
        private readonly Func<FileInfo, bool> _isBadFileSelecter;

        public event EventHandler<BadFileFoundEventArgs>? BadFileFound;

        public Phase1Worker(IWorkerCancellable canceller, Func<FileInfo, bool> isBadFileSelecter)
            : base(canceller, FileWorkerConcurrencyMode.ParallelProcessingForEachFile)
        {
            _isBadFileSelecter = isBadFileSelecter;
        }

        public override string Description => "ZIPファイルに最適化を試みます。";

        protected override IFileWorkerActionFileParameter? IsMatchFile(FileInfo sourceFile)
        {
            // 拡張子が ".zip", ".epub" のいずれかのファイルのみを対象とする
            return
                !_isBadFileSelecter(sourceFile) &&
                sourceFile.Extension.IsAnyOf(".zip", ".epub", StringComparison.OrdinalIgnoreCase)
                ? base.IsMatchFile(sourceFile)
                : null;
        }

        protected override void ActionForFile(FileInfo sourceFile, IFileWorkerActionParameter parameter)
        {
            try
            {
                var detail = "???";
                if (sourceFile.CheckZipFile(s => detail = s) != ZipFileCheckResult.Ok)
                {
                    RaiseErrorReportedEvent(sourceFile, string.Format("処理できないアーカイブファイルであるため無視します。: {0}", detail));
                    RaiseBadFileFoundEvent(sourceFile);
                    return;
                }

                void informationReportedEventHander(object? s, FileMessageReportedEventArgs e) => RaiseInformationReportedEvent(e.TargetFile, e.Message);
                void warningReportedEventHander(object? s, FileMessageReportedEventArgs e) => RaiseWarningReportedEvent(e.TargetFile, e.Message);
                void errorReportedEventHander(object? s, FileMessageReportedEventArgs e) => RaiseErrorReportedEvent(e.TargetFile, e.Message);
                void progressUpdatedHander(object? s, ProgressUpdatedEventArgs e) => UpdateProgress();
                FileInfo? newZipFile = null;
                try
                {
                    using var sourceZipFile = sourceFile.OpenAsZipFile();
                    var entryTree = EntryTree.GetEntryTree(sourceFile, sourceZipFile);
                    if (entryTree is not null)
                    {
                        entryTree.InformationReported += informationReportedEventHander;
                        entryTree.WarningReported += warningReportedEventHander;
                        entryTree.ErrorReported += errorReportedEventHander;
                        entryTree.ProgressUpdated += progressUpdatedHander;
                        try
                        {
                            if (entryTree.ContainsEntryIncompatibleWithUnicode())
                            {
                                RaiseErrorReportedEvent(sourceFile, "このアプリケーションでは扱えない名前またはコメントを持つエントリがアーカイブファイルに含まれているため、無視します。");
                                RaiseBadFileFoundEvent(sourceFile);
                                return;
                            }

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
                                newZipFile = SaveEntryTreeToArchiveFile(sourceFile, entryTree);
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
                }
                finally
                {
                    if (newZipFile is not null)
                    {
                        sourceFile.SendToRecycleBin();
                        new FileInfo(newZipFile.FullName).MoveTo(sourceFile.FullName);
#if DEBUG

                        //if (newZipFile.Exists)
                        //    throw new Exception();
#endif
                    }
                }
            }
            catch (IOException)
            {
            }
        }

        private static FileInfo SaveEntryTreeToArchiveFile(FileInfo sourceFile, EntryTree entryTree)
        {
            var newArchiveFile = new FileInfo(Path.Combine(sourceFile.DirectoryName ?? ".", "." + Path.GetFileName(sourceFile.FullName) + ".temp"));
            try
            {
                newArchiveFile.Delete();
                entryTree.SaveTo(newArchiveFile);
                return newArchiveFile;
            }
            catch (Exception)
            {
                newArchiveFile.Delete();
                throw;
            }
            finally
            {
#if DEBUG
                //if (newArchiveFile.Exists)
                //    throw new Exception();
#endif
            }
        }

        private void RaiseBadFileFoundEvent(FileInfo targetFile)
        {
            if (BadFileFound is not null)
                BadFileFound(this, new BadFileFoundEventArgs(targetFile));
        }
    }
}
using System;
using System.IO;
using System.Linq;
using Utility;
using Utility.FileWorker;
using ZipUtility;
using ZipUtility.ZipExtraField;

namespace ZipArchiveNormalizer.Phase0
{
    class Phase0Worker
        : FileWorkerFromMainArgument, IPhaseWorker
    {
        private readonly Func<FileInfo, bool> _isBadFileSelecter;

        public event EventHandler<BadFileFoundEventArgs>? BadFileFound;

        static Phase0Worker()
        {
        }

        public Phase0Worker(IWorkerCancellable canceller, Func<FileInfo, bool> isBadFileSelecter)
            : base(canceller, FileWorkerConcurrencyMode.ParallelProcessingForEachFile)
        {
            _isBadFileSelecter = isBadFileSelecter;
        }

        public override string Description => "ZIPファイルに未知の拡張フィールドがないか調べます。";

        protected override IFileWorkerActionFileParameter? IsMatchFile(FileInfo sourceFile)
        {
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
                using var zipFile = sourceFile.OpenAsZipFile();
                foreach (var entry in zipFile.GetEntries())
                {
                    UpdateProgress();
                    var extraFieldIds =
                        entry.ExtraFields.EnumerateExtraFieldIds()
                        .ToList();
                    extraFieldIds.Remove(CodePageExtraField.ExtraFieldId);
                    extraFieldIds.Remove(ExtendedTimestampExtraField.ExtraFieldId);
                    extraFieldIds.Remove(NewUnixExtraField.ExtraFieldId);
                    extraFieldIds.Remove(NtfsExtraField.ExtraFieldId);
                    extraFieldIds.Remove(UnicodeCommentExtraField.ExtraFieldId);
                    extraFieldIds.Remove(UnicodePathExtraField.ExtraFieldId);
                    extraFieldIds.Remove(UnixExtraFieldType0.ExtraFieldId);
                    extraFieldIds.Remove(UnixExtraFieldType1.ExtraFieldId);
                    extraFieldIds.Remove(UnixExtraFieldType2.ExtraFieldId);
                    extraFieldIds.Remove(WindowsSecurityDescriptorExtraField.ExtraFieldId);
                    extraFieldIds.Remove(0x0001);
                    if (extraFieldIds.Any())
                    {
                        RaiseWarningReportedEvent(
                            sourceFile,
                            string.Format(
                                "Unknown extra field used.: entry=\"{0}\", id={{{1}}}",
                                entry.FullName,
                                string.Join(
                                    ", ",
                                    extraFieldIds
                                        .OrderBy(id => id)
                                        .Select(id => string.Format("0x{0:x4}", id)))));
                    }
                }
            }
            catch (Exception)
            {
                RaiseErrorReportedEvent(sourceFile, "zipファイルの解析に失敗しました。");
            }
            finally
            {
                AddToDestinationFiles(sourceFile);
            }
        }

#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        private void RaiseBadFileFoundEvent(FileInfo targetFile)
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            if (BadFileFound is not null)
            {
                BadFileFound(this, new BadFileFoundEventArgs(targetFile));
            }
        }
    }
}
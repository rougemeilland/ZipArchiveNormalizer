﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Utility;
using Utility.FileWorker;
using ZipUtility;

namespace Test.ZipUtility
{
    static class ValidationOfZipFile
    {
        private class Walker
            : FileWorkerFromMainArgument
        {
            private static readonly Regex _zipFileNamePattern;

            static Walker()
            {
                _zipFileNamePattern = new Regex(@"\.(zip|zipx|epub|exe|zip\.001)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            public Walker(IWorkerCancellable canceller)
                : base(canceller, FileWorkerConcurrencyMode.ParallelProcessingForEachFile)
            {
            }

            public override string Description => "ZIPファイルの検査をします。";

            protected override IFileWorkerActionFileParameter? IsMatchFile(FileInfo sourceFile)
            {
                return
                    _zipFileNamePattern.IsMatch(sourceFile.Name)
                    && (
                        false
                        //|| (sourceFile.Name.Contains("DEFLATE", StringComparison.OrdinalIgnoreCase) && !sourceFile.Name.Contains("DEFLATE64", StringComparison.OrdinalIgnoreCase))
                        //|| sourceFile.Name.Contains("DEFLATE64", StringComparison.OrdinalIgnoreCase)
                        || sourceFile.Name.Contains("LZMA", StringComparison.OrdinalIgnoreCase)
                        //|| sourceFile.Name.Contains("BZIP2", StringComparison.OrdinalIgnoreCase)
                        //|| sourceFile.Name.Contains("PPMD", StringComparison.OrdinalIgnoreCase)
                    )
                        ? base.IsMatchFile(sourceFile)
                        : null;
            }

            protected override void ActionForFile(FileInfo sourceFile, IFileWorkerActionParameter parameter)
            {
                try
                {
                    var detail = "???";
                    switch (sourceFile.CheckZipFile(s => detail = s, () => UpdateProgress()))
                    {
                        case ZipFileCheckResult.Ok:
                            RaiseInformationReportedEvent(sourceFile, string.Format("正しいZIPファイルです。: {0}", detail));
                            break;
                        case ZipFileCheckResult.Encrypted:
                            RaiseErrorReportedEvent(sourceFile, string.Format("暗号化されているZIPファイルです。: {0}", detail));
                            break;
                        case ZipFileCheckResult.UnsupportedCompressionMethod:
                            RaiseErrorReportedEvent(sourceFile, string.Format("サポートされていない圧縮方式が使用されているZIPファイルです。: {0}", detail));
                            break;
                        case ZipFileCheckResult.UnsupportedFunction:
                            RaiseErrorReportedEvent(sourceFile, string.Format("サポートされていない機能が使用されているZIPファイルです。: {0}", detail));
                            break;
                        case ZipFileCheckResult.Corrupted:
                        default:
                            RaiseErrorReportedEvent(sourceFile, string.Format("正しいZIPファイルではありません。: {0}", detail));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    RaiseErrorReportedEvent(sourceFile, string.Format("例外が発生しました。: type=\"{0}\", message=\"{1}\"", ex.GetType(), ex.Message));
                }
            }
        }

        private class Worker
            : ConsoleWorker
        {
            private readonly IReadOnlyCollection<IFileWorker> _workers;

            public Worker(IWorkerCancellable canceller)
                : base(canceller)
            {
                _workers = new[] { new Walker(canceller) }.ToReadOnlyCollection();
            }

            protected override IReadOnlyCollection<IFileWorker> Workers => _workers;

            protected override string FirstMessage => "";

            protected override string NormalCompletionMessage => "正常に終了しました。";

            protected override string CancellationMessage => "中断されました。";

            protected override string CompletionMessageOnError => "エラーにより終了しました。";
        }

        public static void Test(string[] args)
        {
            using var canceller = new ConsoleBreakCancellale();
            new Worker(canceller).Execute(args);
        }
    }
}

using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ZipUtility;
using Utility;
using Utility.FileWorker;
using System.Collections.Generic;

namespace Experiment
{
    class Program
    {
        private class Worker
            : FileWorkerFromMainArgument
        {
            public Worker(IWorkerCancellable canceller)
                : base(canceller, FileWorkerConcurrencyMode.ParallelProcessingForEachFile)
            {

            }

            protected override IFileWorkerActionFileParameter IsMatchFile(FileInfo sourceFile)
            {
                if (string.Equals(sourceFile.Extension, ".zip", StringComparison.InvariantCultureIgnoreCase) == false)
                    return null;
                return base.IsMatchFile(sourceFile);
            }

            public override string Description => "ZIPファイルのエントリが読み取れることを確認します。";

            protected override void ActionForFile(FileInfo sourceFile, IFileWorkerActionParameter parameter)
            {
                try
                {
                    foreach (var entry in sourceFile.EnumerateZipArchiveEntry())
                    {
                        if (entry.GeneralPurposeBitFlag.HasFlag(ZipEntryGeneralPurposeBitFlag.HasDataDescriptor))
                        {
                            RaiseInformationReportedEvent(
                                sourceFile,
                                string.Format(
                                    "{0}: crc=0x{1:x8}, packed size={2}, size={3}",
                                    entry.FullName,
                                    entry.Crc,
                                    entry.PackedSize,
                                    entry.Size));
                        }
                    }
                }
                catch (Exception ex)
                {
                    RaiseErrorReportedEvent(sourceFile, string.Format("例外が発生しました。: {0}", ex.Message));
                }
            }
        }

        private class MainWorker
            : ConsoleWorker
        {
            private IReadOnlyCollection<IFileWorker> _workers;


            public MainWorker(IWorkerCancellable canceller)
                : base(canceller)
            {
                _workers = new[] { new Worker(canceller) }.ToReadOnlyCollection();
            }

            protected override IReadOnlyCollection<IFileWorker> Workers => _workers;

            protected override string FirstMessage => "ZIPファイルのエントリが読み取れるかどうか調べます。";

            protected override string NormalCompletionMessage => "正常に終了しました。";

            protected override string CancellationMessage => "ユーザにより中断されました。";

            protected override string CompletionMessageOnError => "エラーにより中断されました。";
        }

        static void Main(string[] args)
        {
            using (var canceller = new ConsoleBreakCancellale())
            {
                var worker = new MainWorker(canceller);
                worker.Execute(args);

            }
            Console.ReadLine();
        }
    }
}
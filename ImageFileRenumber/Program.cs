using ImageUtility;
using System;
using System.Collections.Generic;
using Utility;
using Utility.FileWorker;

namespace ImageFileRenumber
{
    class Program
    {
        private class Worker
            : ConsoleWorker
        {
            private readonly IReadOnlyCollection<IFileWorker> _workers;

            public Worker(IWorkerCancellable canceller)
                : base(canceller)
            {
                _workers = new[]
                {
                    new ImageFileNameRenumberWorker(canceller) as IFileWorker,
                    new CheckToContinueSameImageWorker(canceller),
                    new ImageFileSummaryEnumerationWorker(canceller),
                }
                .ToReadOnlyCollection();
            }

            protected override IReadOnlyCollection<IFileWorker> Workers => _workers;

            protected override string FirstMessage => "画像の一括変名をします。";

            protected override string NormalCompletionMessage => "正常に終了しました。";

            protected override string CancellationMessage => "中断されました。";

            protected override string CompletionMessageOnError => "エラーが発生しました。";
        }

        static void Main(string[] args)
        {
            using (var canceller = new ConsoleBreakCancellale())
            {
                var worker = new Worker(canceller);
                worker.Execute(args);
            }
            Console.WriteLine("Enterを押してください。");
            Console.Beep();
            Console.ReadLine();
        }
    }
}
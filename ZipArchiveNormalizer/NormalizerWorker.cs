using System;
using System.Collections.Generic;
using System.IO;
using Utility;
using Utility.FileWorker;
using ZipArchiveNormalizer.Phase1;
using ZipArchiveNormalizer.Phase2;
using ZipArchiveNormalizer.Phase3;
using ZipArchiveNormalizer.Phase4;
using ZipArchiveNormalizer.Phase5;
using ZipArchiveNormalizer.Phase6;

namespace ZipArchiveNormalizer
{
    class NormalizerWorker
        : ConsoleWorker, IDisposable
    {
        private static FileInfo _errorLogFile;
        private static FileInfo _warningLogFile;
        private bool _isDisposed;
        private IDictionary<string, object> _badArchiveFiles;
        private IReadOnlyCollection<IPhaseWorker> _workers;

        static NormalizerWorker()
        {
            _errorLogFile = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Properties.Settings.Default.ApplicationName, "errorlog.txt"));
            _warningLogFile = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Properties.Settings.Default.ApplicationName, "warninglog.txt"));
        }

        public NormalizerWorker(IWorkerCancellable canceller)
            : base(canceller)
        {
            _isDisposed = false;
            _badArchiveFiles = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            _workers = new[]
            {
                //new Phase0Worker(file => _badArchiveFiles.ContainsKey(file.FullName)),
                new Phase1Worker(canceller, file => _badArchiveFiles.ContainsKey(file.FullName)) as IPhaseWorker,
                new Phase2Worker(canceller, file => _badArchiveFiles.ContainsKey(file.FullName)),
                new Phase3Worker(canceller, file => _badArchiveFiles.ContainsKey(file.FullName)),
                new Phase4Worker(canceller, file => _badArchiveFiles.ContainsKey(file.FullName)),
                new Phase5Worker(canceller, file => _badArchiveFiles.ContainsKey(file.FullName)),
                new Phase6Worker(canceller, file => _badArchiveFiles.ContainsKey(file.FullName)),
            };
            foreach (var worker in _workers)
                worker.BadFileFound += Worker_BadFileFound;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected override IReadOnlyCollection<IFileWorker> Workers => _workers;
        protected override string FirstMessage => "指定されたファイル(.zip/.pdf/.epub)を正規化します。";
        protected override string CancellationMessage => "ユーザによって中断されました。";
        protected override string CompletionMessageOnError => "エラーが発生しました。";
        protected override string NormalCompletionMessage => "正常に終了しました。";

        protected override void InitializeBeforeExecution()
        {
            base.InitializeBeforeExecution();
        }

        protected override void OnError(DateTime now, FileInfo sourceFile, string message)
        {
            _errorLogFile.Directory.Create();
            using (var writer = _errorLogFile.AppendText())
            {
                if (sourceFile == null)
                    writer.WriteLine(string.Format("{0:O}:message={1}", now, message));
                else
                    writer.WriteLine(string.Format("{0:O}:file=\"{1}\":message={2}", now, sourceFile, message));
            }
        }

        protected override void OnWarning(DateTime now, FileInfo sourceFile, string message)
        {
            _warningLogFile.Directory.Create();
            using (var writer = _warningLogFile.AppendText())
            {
                if (sourceFile == null)
                    writer.WriteLine(string.Format("{0:O}:message={1}", now, message));
                else
                    writer.WriteLine(string.Format("{0:O}:file=\"{1}\":message={2}", now, sourceFile, message));
            }
        }

        protected override void OnWarningExists()
        {
            using (var proccess = System.Diagnostics.Process.Start(_warningLogFile.FullName))
            {
            }
        }

        protected override void OnErrorExists()
        {
            using (var proccess = System.Diagnostics.Process.Start(_errorLogFile.FullName))
            {
            }
        }

        protected override void OnExceptionExists(Exception ex, DateTime now)
        {
            _errorLogFile.Directory.Create();
            using (var writer = _errorLogFile.AppendText())
            {
                writer.WriteLine("====================");
                while (ex != null)
                {
                    writer.WriteLine(string.Format("{0}:", now));
                    writer.WriteLine(ex.Message);
                    writer.WriteLine(ex.StackTrace);
                    writer.WriteLine("----------");
                    ex = ex.InnerException;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                }

                foreach (var worker in _workers)
                    worker.BadFileFound -= Worker_BadFileFound;
                _workers = new IPhaseWorker[0];

                _isDisposed = true;
            }
        }

        private void Worker_BadFileFound(object sender, BadFileFoundEventArgs e)
        {
            _badArchiveFiles[e.TargetFile.FullName] = null;
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utility.FileWorker;

namespace Utility
{
    public abstract class ConsoleWorker
    {
        private const string _carriageReturnConsoleControlText = "\r";
        private static string _eraseLineConsoleControlText;
        private static TimeSpan _minimumProgressInternal;
        private IWorkerCancellable _canceller;
        private bool _existsError;
        private bool _existsWarning;
        private string _previousPrintedPercentageText;
        private DateTime? _previousPrintedTime;
        private int _progressState;

        static ConsoleWorker()
        {
            _eraseLineConsoleControlText = _carriageReturnConsoleControlText + new string(' ', Console.WindowWidth - 5) + _carriageReturnConsoleControlText;
            _minimumProgressInternal = TimeSpan.FromMilliseconds(300);
        }

        protected ConsoleWorker(IWorkerCancellable canceller)
        {
            _canceller = canceller;
            _existsError = false;
            _existsWarning = false;
            _previousPrintedPercentageText = null;
            _previousPrintedTime = null;
            _progressState = 0;
        }

        public void Execute(IEnumerable<string> args)
        {
            try
            {
                if (!string.IsNullOrEmpty(FirstMessage))
                    Console.WriteLine(FirstMessage);
                Console.ResetColor();
                _existsError = false;
                _existsWarning = false;
                InitializeBeforeExecution();

                var previousWorkerResult = null as IFileWorkerExecutionResult;
                foreach (var item in Workers.Select((worker, index) => new { worker, index }))
                {
                    PrintInformationMessage(
                        null,
                        string.Format(
                            "{0}Phase {1}/{2}開始。{3:}",
                            _eraseLineConsoleControlText,
                            item.index + 1,
                            Workers.Count,
                            item.worker.Description));
                    previousWorkerResult = ExecutePhase(item.worker, args, previousWorkerResult, Workers.Count, item.index + 1);
                    PrintInformationMessage(
                        null,
                        string.Format(
                            previousWorkerResult.TotalChangedFileCount > 0
                            ? "{0}Phase {1}/{2}完了。{3:N0} 個のファイルが処理され、{4:N0} 個のファイルが変更/削除されました。"
                            : "{0}Phase {1}/{2}完了。{3:N0} 個のファイルが処理されました。",
                            _eraseLineConsoleControlText,
                            item.index + 1,
                            Workers.Count,
                            previousWorkerResult.SourceFiles.Count,
                            previousWorkerResult.TotalChangedFileCount));
                }
                Console.ResetColor();
                Console.WriteLine(_eraseLineConsoleControlText);
                Console.WriteLine(_eraseLineConsoleControlText + NormalCompletionMessage);
            }
            catch (OperationCanceledException)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                try
                {
                    Console.WriteLine(_eraseLineConsoleControlText);
                    Console.WriteLine(_eraseLineConsoleControlText + CancellationMessage);
                }
                finally
                {
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                var now = DateTime.Now;
                try
                {
                    lock (this)
                    {
                        try
                        {
                            OnExceptionExists(ex, now);
                        }
                        catch (Exception)
                        {
                        }
                        _existsError = true;
                    }
                }
                catch (IOException)
                {
                }
                Console.WriteLine(_eraseLineConsoleControlText);
                Console.WriteLine(_eraseLineConsoleControlText + CompletionMessageOnError);
            }
            finally
            {
                lock (this)
                {
                    if (_existsWarning)
                    {
                        try
                        {
                            OnWarningExists();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    if (_existsError)
                    {
                        try
                        {
                            OnErrorExists();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        protected abstract IReadOnlyCollection<IFileWorker> Workers { get; }
        protected abstract string FirstMessage { get; }
        protected abstract string NormalCompletionMessage { get; }
        protected abstract string CancellationMessage { get; }
        protected abstract string CompletionMessageOnError { get; }

        protected virtual void InitializeBeforeExecution()
        {
        }

        protected virtual void OnExceptionExists(Exception ex, DateTime now)
        {
        }

        protected virtual void OnWarningExists()
        {
        }

        protected virtual void OnErrorExists()
        {
        }

        protected virtual void OnError(DateTime now, FileInfo sourceFile, string message)
        {
        }

        protected virtual void OnWarning(DateTime now, FileInfo sourceFile, string message)
        {
        }


        private IFileWorkerExecutionResult ExecutePhase(IFileWorker worker, IEnumerable<string> args, IFileWorkerExecutionResult previousWorkerResult, int maximumPhaseNumber, int currentPhaseNumber)
        {
            long totalCount = -1;
            long countOfDone = -1;

            EventHandler<FileMessageReportedEventArgs> informationReportedEventHandler = (s, e) => PrintInformationMessage(e.TargetFile, e.Message);
            EventHandler<FileMessageReportedEventArgs> warningReportedEventHandler = (s, e) => PrintWarningMessage(e.TargetFile, e.Message);
            EventHandler<FileMessageReportedEventArgs> errorReportedEventHandler = (s, e) => PrintErrorMessage(e.TargetFile, e.Message);
            EventHandler<ProgressChangedEventArgs> progressChangedEventHandler = (s, e) =>
            {
                if (e.IsCounterEnabled)
                {
                    totalCount = e.TotalCount;
                    countOfDone = e.CountOfDone;
                }
                PrintProgress(
                    worker.IsRequestedToCancel
                    ? "中断処理中..."
                    : totalCount > 0
                        ? string.Format("Phase {0}/{1}: {2}%完了", currentPhaseNumber, maximumPhaseNumber, countOfDone * 100 / totalCount)
                        : string.Format("Phase {0}/{1}: 準備中...", currentPhaseNumber, maximumPhaseNumber),
                    worker.IsRequestedToCancel);
            };
            worker.InformationReported += informationReportedEventHandler;
            worker.WarningReported += warningReportedEventHandler;
            worker.ErrorReported += errorReportedEventHandler;
            worker.ProgressChanged += progressChangedEventHandler;
            try
            {
                return worker.Execute(args, previousWorkerResult);
            }
            finally
            {
                worker.InformationReported -= informationReportedEventHandler;
                worker.WarningReported -= warningReportedEventHandler;
                worker.ErrorReported -= errorReportedEventHandler;
                worker.ProgressChanged -= progressChangedEventHandler;
            }
        }

        private void PrintInformationMessage(FileInfo sourceFile, string message)
        {
            lock (this)
            {
                Console.Write(_eraseLineConsoleControlText);
                if (sourceFile != null)
                    Console.Write(string.Format("\"{0}\":", sourceFile.FullName));
                _previousPrintedPercentageText = null;
                _previousPrintedTime = null;
                Console.ForegroundColor = ConsoleColor.Cyan;
                try
                {
                    Console.WriteLine(message);

                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }

        private void PrintWarningMessage(FileInfo sourceFile, string message)
        {
            var now = DateTime.Now;
            lock (this)
            {
                Console.Write(_eraseLineConsoleControlText);
                if (sourceFile != null)
                    Console.Write(string.Format("\"{0}\":", sourceFile.FullName));
                _previousPrintedPercentageText = null;
                _previousPrintedTime = null;
                Console.ForegroundColor = ConsoleColor.Yellow;
                try
                {
                    Console.WriteLine(message);
                    OnWarning(now, sourceFile, message);
                    _existsWarning = true;
                }
                catch (IOException)
                {
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }

        private void PrintErrorMessage(FileInfo sourceFile, string message)
        {
            var now = DateTime.Now;
            lock (this)
            {
                Console.Write(_eraseLineConsoleControlText);
                if (sourceFile != null)
                    Console.Write(string.Format("\"{0}\":", sourceFile.FullName));
                Console.ForegroundColor = ConsoleColor.Magenta;
                try
                {
                    Console.WriteLine(message);
                    _previousPrintedPercentageText = null;
                    _previousPrintedTime = null;
                    OnError(now, sourceFile, message);
                    _existsError = true;
                }
                catch (IOException)
                {
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }

        private void PrintProgress(string percentageText, bool requestedToCancel)
        {
            lock (this)
            {
                var now = DateTime.Now;
                if (_previousPrintedPercentageText == null ||
                    _previousPrintedPercentageText != percentageText ||
                    _previousPrintedTime.HasValue == false ||
                    (now - _previousPrintedTime.Value) >= _minimumProgressInternal)
                {
                    Console.Write(
                        string.Format(
                            "{0}{1}  {2}  ({3}){4}",
                            _eraseLineConsoleControlText,
                            percentageText,
                            GetProgressText(),
                            requestedToCancel ? "しばらくお待ちください" : _canceller.Usage,
                            _carriageReturnConsoleControlText));
                    _previousPrintedPercentageText = percentageText;
                    _previousPrintedTime = now;
                }
            }
        }

        private string GetProgressText()
        {
            switch (_progressState)
            {
                case 0:
                    _progressState = 1;
                    return "■□□□□";
                case 1:
                    _progressState = 2;
                    return "□■□□□";
                case 2:
                    _progressState = 3;
                    return "□□■□□";
                case 3:
                    _progressState = 4;
                    return "□□□■□";
                case 4:
                    _progressState = 5;
                    return "□□□□■";
                case 5:
                    _progressState = 6;
                    return "□□□■□";
                case 6:
                    _progressState = 7;
                    return "□□■□□";
                default:
                    _progressState = 0;
                    return "□■□□□";
            }
        }
    }
}

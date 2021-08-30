using System;
using System.Threading;
using System.IO;
using System.Linq;

namespace AutoImageTrimmer
{
    abstract class FileWalkerFromMainArgument
    {
        private int _suffixCount;
        private string _previousFileName;
        private bool _walkingNow;
        private bool _canceled;

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        protected FileWalkerFromMainArgument()
        {
            _suffixCount = 0;
            _previousFileName = "";
            _walkingNow = false;
            _canceled = false;
        }

        protected virtual bool IsMatchFile(FileInfo sourceFile)
        {
            return true;
        }

        protected abstract void ActionForFile(FileInfo sourceFile);

        public void CancelToWalk()
        {
            lock (this)
            {
                if (_walkingNow)
                {
                    _canceled = true;
                }
            }
        }

        public bool Walk(string[]  args)
        {
            lock (this)
            {
                if (_walkingNow)
                    throw new InvalidOperationException();
                _walkingNow = true;
                _canceled = false;
            }
            try
            {
                var files =
                    args
                    .SelectMany(arg =>
                    {
                        if (IsRequestedToCancel)
                            return new FileInfo[0];
                        RaiseProgressChangedEvent(-1, -1);
                        var file = TryParseAsFilePath(arg);
                        if (file != null)
                            return new[] { file };
                        var directory = TryParseAsDirectoryPath(arg);
                        if (directory != null)
                            return directory.EnumerateFiles("*", SearchOption.AllDirectories);
                        return new FileInfo[0];
                    })
                    .ToList();
                if (IsRequestedToCancel)
                    return false;
                var totalCount = files.Count();
#if false
                var processerCount = Environment.ProcessorCount;
#else
                var processerCount = 1;
#endif
                long countOfDone = 0;
                RaiseProgressChangedEvent(totalCount, countOfDone);

                files
                    .AsParallel()
                    .WithDegreeOfParallelism(processerCount)
                    .ForAll(sourceFile =>
                    {
                        if (IsRequestedToCancel)
                            return;

                        bool executeAction;
                        try
                        {
                            executeAction = IsMatchFile(sourceFile);
                        }
                        catch (Exception)
                        {
                            executeAction = false;
                        }
                        if (executeAction)
                        {
                            try
                            {
                                ActionForFile(sourceFile);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        RaiseProgressChangedEvent(totalCount, Interlocked.Increment(ref countOfDone));
                    });
                return IsRequestedToCancel ? false : true;
            }
            finally
            {
                lock (this)
                {
                    _walkingNow = false;
                    _canceled = false;
                }
            }
        }

        protected string GetFileNameFromTimeStamp()
        {
            return GetFileNameFromTimeStamp(DateTime.Now);
        }

        protected string GetFileNameFromTimeStamp(DateTime now)
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

        private bool IsRequestedToCancel
        {
            get
            {
                lock (this)
                {
                    return _canceled;
                }
            }
        }

        private void RaiseProgressChangedEvent(long totalCount, long countOfDone)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, new ProgressChangedEventArgs(totalCount, countOfDone));
        }

        private static FileInfo TryParseAsFilePath(string path)
        {
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists)
                    return null;
                return file;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static DirectoryInfo TryParseAsDirectoryPath(string path)
        {
            try
            {
                var directory = new DirectoryInfo(path);
                if (!directory.Exists)
                    return null;
                return directory;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

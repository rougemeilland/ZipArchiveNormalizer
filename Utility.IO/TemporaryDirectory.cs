using System;
using System.IO;

namespace Utility.IO
{
    public class TemporaryDirectory
        : IDisposable
    {
        private readonly string _lockFilePath;
        private readonly string _directoryPath;

        private bool _isDisposed;

        private TemporaryDirectory(string lockFilePath, string directoryPath)
        {
            if (string.IsNullOrEmpty(lockFilePath))
                throw new ArgumentException($"'{nameof(lockFilePath)}' を NULL または空にすることはできません。", nameof(lockFilePath));
            if (string.IsNullOrEmpty(directoryPath))
                throw new ArgumentException($"'{nameof(directoryPath)}' を NULL または空にすることはできません。", nameof(directoryPath));

            _lockFilePath = lockFilePath;
            _directoryPath = directoryPath;
        }

        ~TemporaryDirectory()
        {
            Dispose(disposing: false);
        }

        public string FullName
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (_directoryPath is null)
                    throw new InvalidOperationException();

                return _directoryPath;
            }
        }

        public static TemporaryDirectory Create()
        {
            while (true)
            {
                var success = false;
                var lockFilePath = (string?)null;
                var directoryPath = (string?)null;
                try
                {
                    try
                    {
                        lockFilePath = Path.GetTempFileName();
                        directoryPath = lockFilePath + ".dir";
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                            success = true;
                            return new TemporaryDirectory(lockFilePath, directoryPath);
                        }
                    }
                    catch (IOException)
                    {
                    }
                }
                finally
                {
                    if (!success)
                    {
                        if (directoryPath is not null)
                            Directory.Delete(directoryPath, true);
                        if (lockFilePath is not null)
                            File.Delete(lockFilePath);
                    }
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

                // ファイルはアンマネージリソース扱い
                Directory.Delete(_directoryPath, true);
                File.Delete(_lockFilePath);
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
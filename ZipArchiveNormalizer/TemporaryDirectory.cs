#if false
using System;
using System.IO;

namespace ZipCabinetNormalizer
{
    class TemporaryDirectory
        : IDisposable
    {
        private bool _isDisposed;
        private string _lockFilePath;
        private string _directoryPath;

        private TemporaryDirectory(string lockFilePath, string directoryPath)
        {
            _lockFilePath = lockFilePath;
            _directoryPath = directoryPath;
        }

        public string FullName => _directoryPath;

        public static TemporaryDirectory Create()
        {
            while (true)
            {
                var lockFilePath = Path.GetTempFileName();
                try
                {
                    var directoryPath = lockFilePath + ".dir";
                    Directory.CreateDirectory(directoryPath);
                    return new TemporaryDirectory(lockFilePath, directoryPath);
                }
                catch (Exception)
                {
                    File.Delete(lockFilePath);
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
                if (_directoryPath != null)
                {
                    Directory.Delete(_directoryPath, true);
                    _directoryPath = null;
                }
                if (_lockFilePath != null)
                {
                    File.Delete(_lockFilePath);
                    _lockFilePath = null;
                }
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
#endif

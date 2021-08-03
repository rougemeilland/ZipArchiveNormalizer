using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ZipArchiveNormalizer
{
    class ZipFileEntryEnumerable
        : IEnumerable<ZipArchiveEntry>
    {
        private class Enumerator
            : IEnumerator<ZipArchiveEntry>
        {
            private bool _isDisposed;
            private ZipFile _zipFile;
            private IEnumerable<ZipEntry> _source;
            private IEnumerator<ZipEntry> _enumerator;


            public Enumerator(string zipFilePath)
            {
                _zipFile = new ZipFile(zipFilePath);
                _source = _zipFile.Cast<ZipEntry>();
                _enumerator = _source.GetEnumerator();
            }

            public ZipArchiveEntry Current => new ZipArchiveEntry(_enumerator.Current);

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = _source.GetEnumerator();
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_isDisposed)
                {
                    if (disposing)
                    {
                    }

                    if (_enumerator != null)
                    {
                        _enumerator.Dispose();
                        _enumerator = null;
                    }
                    _source = null;
                    if (_zipFile != null)
                    {
                        ((IDisposable)_zipFile).Dispose();
                        _zipFile = null;
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

        private string _zipFilePath;

        public ZipFileEntryEnumerable(string zipFilePath)
        {
            _zipFilePath = zipFilePath;
        }

        public IEnumerator<ZipArchiveEntry> GetEnumerator()
        {
            return new Enumerator(_zipFilePath);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

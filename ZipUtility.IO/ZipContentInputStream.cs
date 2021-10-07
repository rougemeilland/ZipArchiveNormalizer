using System;
using System.IO;
using Utility.IO;

namespace ZipUtility.IO
{
    public abstract class ZipContentInputStream
        : IInputByteStream<UInt64>
    {
        private const UInt64 _PROGRESS_STEP = 80 * 1024;
        private bool _isDisposed;
        private UInt64 _size;
        private ICodingProgressReportable _progressReporter;
        private UInt64 _position;
        private IBasicInputByteStream _sourceStream;
        private bool _isEndOfStream;
        private UInt64 _progressCount;

        public ZipContentInputStream(IBasicInputByteStream baseStream, UInt64 size)
            : this(baseStream, size, null)
        {
        }

        public ZipContentInputStream(IBasicInputByteStream baseStream, UInt64 size, ICodingProgressReportable progressReporter)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));

            _isDisposed = false;
            _size = size;
            _progressReporter = progressReporter;
            _sourceStream = null;
            _position = 0;
            _progressCount = 0;
        }

        public UInt64 Position => !_isDisposed ? _position : throw new ObjectDisposedException(GetType().FullName);

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_sourceStream == null)
                throw new Exception("'InputStream.SetSourceStream' has not been called yet.");
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentException("'offset' must not be negative.", nameof(offset));
            if (count < 0)
                throw new ArgumentException("'count' must not be negative.", nameof(count));
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException("'offset + count' is greater than 'buffer.Length'.");
            if (_isEndOfStream)
                return 0;

            var length = ReadFromSourceStream(_sourceStream, buffer, offset, count);
            if (length > 0)
            {
                _position += (UInt64)length;
                _progressCount += (UInt64)length;
                if ( _progressCount >= _PROGRESS_STEP)
                    ReportProgress();
            }
            else
            {
                _isEndOfStream = true;
                if (_position != _size)
                    throw new IOException("Size not match");
                else
                    OnEndOfStream();
                ReportProgress();
            }
            return length;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected void SetSourceStream(IBasicInputByteStream sourceStream)
        {
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));
            _sourceStream = sourceStream;
        }

        protected virtual int ReadFromSourceStream(IBasicInputByteStream sourceStream, byte[] buffer, int offset, int count)
        {
            return sourceStream.Read(buffer, offset, count);
        }

        protected virtual void OnEndOfStream()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_sourceStream != null)
                    {
                        _sourceStream.Dispose();
                        _sourceStream = null;
                    }
                }
                _isDisposed = true;
            }
        }

        private void ReportProgress()
        {
            if (_progressReporter != null && _progressCount > 0)
            {
                try
                {
                    _progressReporter.SetProgress(_progressCount);
                }
                catch (Exception)
                {
                }
            }
            _progressCount = 0;
        }
    }
}

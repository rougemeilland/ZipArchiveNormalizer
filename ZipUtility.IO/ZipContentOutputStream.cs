using System;
using System.IO;
using Utility;
using Utility.IO;

namespace ZipUtility.IO
{
    public abstract class ZipContentOutputStream
        : IOutputByteStream<UInt64>
    {
        private const UInt64 _PROGRESS_STEP = 80 * 1024;
        private bool _isDisposed;
        private ulong? _size;
        private ICodingProgressReportable _progressReporter;
        private IBasicOutputByteStream _destinationStream;
        private ulong _position;
        private bool _isEndOfWriting;
        private UInt64 _progressCount;

        public ZipContentOutputStream(IBasicOutputByteStream baseStream, ulong? size)
            : this(baseStream, size, null)
        {
        }

        public ZipContentOutputStream(IBasicOutputByteStream baseStream, ulong? size, ICodingProgressReportable progressReporter)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));

            _isDisposed = false;
            _size = size;
            _progressReporter = progressReporter;
            _destinationStream = null;
            _position = 0;
            _isEndOfWriting = false;
            _progressCount = 0;
        }

        public ulong Position => !_isDisposed ? _position : throw new ObjectDisposedException(GetType().FullName);

        public int Write(IReadOnlyArray<byte> buffer, int offset, int count)
        {
            if (_destinationStream == null)
                throw new Exception("'OutputStream.SetDestinationStream' has not been called yet.");
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
            if (count > 0 && _isEndOfWriting)
                throw new InvalidOperationException("Can not write any more.");

            var written = WriteToDestinationStream(_destinationStream, buffer, offset, count);
            if (written > 0)
            {
                _position += (ulong)written;
                _progressCount += (UInt64)written;
                if (_progressCount >= _PROGRESS_STEP)
                    ReportProgress();
            }
            if (_size.HasValue && _position >= _size.Value)
            {
                FlushDestinationStream(_destinationStream, true);
                _isEndOfWriting = true;
            }
            return written;
        }

        public void Flush()
        {
            if (_destinationStream == null)
                throw new Exception("'OutputStream.SetDestinationStream' has not been called yet.");

            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            try
            {
                FlushDestinationStream(_destinationStream, false);
            }
            catch (IOException)
            {
                throw;
            }
            catch (ObjectDisposedException ex)
            {
                throw new IOException("Stream is closed.", ex);
            }
            catch (OperationCanceledException ex)
            {
                throw new IOException("Stream is closed.", ex);
            }
            catch (Exception ex)
            {
                throw new IOException("Can not flush stream.", ex);
            }
        }

        public void Close()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _destinationStream.Close();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_destinationStream != null)
                    {
                        try
                        {
                            FlushDestinationStream(_destinationStream, true);
                            _isEndOfWriting = true;
                        }
                        catch (Exception)
                        {
                        }
                        ReportProgress();
                        _destinationStream.Dispose();
                        _destinationStream = null;
                    }
                }
                _isDisposed = true;

            }
        }

        protected void SetDestinationStream(IBasicOutputByteStream destinationStream)
        {
            if (destinationStream == null)
                throw new ArgumentNullException(nameof(destinationStream));
            _destinationStream = destinationStream;
        }

        protected virtual int WriteToDestinationStream(IBasicOutputByteStream destinationStream, IReadOnlyArray<byte> buffer, int offset, int count)
        {
            return destinationStream.Write(buffer, offset, count);
        }

        protected virtual void FlushDestinationStream(IBasicOutputByteStream destinationStream, bool isEndOfData)
        {
            if (_isEndOfWriting == false)
                destinationStream.Flush();
        }

        protected void CloseDestinationStream()
        {
            _destinationStream.Close();
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
using System;
using System.IO;

namespace Utility.IO
{
    public abstract class OutputStream
        : Stream
    {
        private bool _isDisposed;
        private long? _size;
        private bool _leaveOpen;
        private Stream _destinationStream;
        private long _totalCount;
        private bool _isEndOfWriting;

        public OutputStream(Stream baseStream, long? offset, long? size, bool leaveOpen)
        {
            if (baseStream == null)
                throw new ArgumentNullException("baseStream");
            if (baseStream.CanWrite == false)
                throw new ArgumentException("'baseStream' is not suppot 'Write'.", "baseStream");
            if (offset.HasValue && offset.Value < 0)
                throw new ArgumentException("'offset' must not be negative.", "offset");
            if (size.HasValue && size.Value < 0)
                throw new ArgumentException("'size' must not be negative.", "size");

            _isDisposed = false;
            _size = size;
            _leaveOpen = leaveOpen;
            _destinationStream = null;
            _totalCount = 0;
            _isEndOfWriting = false;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_destinationStream == null)
                throw new Exception("'OutputStream.SetDestinationStream' has not been called yet.");
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentException("'offset' must not be negative.", "offset");
            if (count < 0)
                throw new ArgumentException("'count' must not be negative.", "count");
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException("'offset + count' is greater than 'buffer.Length'.");
            if (_size.HasValue && _totalCount + count > _size.Value)
                throw new InvalidOperationException("Can not write any more.");
            if (count > 0 && _isEndOfWriting)
                throw new InvalidOperationException("Can not write any more.");

            WriteToDestinationStream(_destinationStream, buffer, offset, count);
            _totalCount += count;
            if (_size.HasValue && _totalCount >= _size.Value)
            {
                FlushDestinationStream(_destinationStream, true);
                _isEndOfWriting = true;
            }
        }

        public override void Flush()
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

        protected override void Dispose(bool disposing)
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
                        if (_leaveOpen == false)
                            _destinationStream.Dispose();
                        _destinationStream = null;
                    }
                }
                _isDisposed = true;
                base.Dispose(disposing);

            }
        }

        // 本来は CanSeek == false なら Position がされた場合は NotSupportedException 例外を発行すべきだが、
        // たまに CanSeek の値を確認せずに Position を参照するユーザが存在するので、 get だけは実装する。
        public override long Position { get => _totalCount; set => throw new NotSupportedException(); }
        public override long Length => _totalCount;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected void SetDestinationStream(Stream destinationStream)
        {
            if (destinationStream == null)
                throw new ArgumentNullException("destinationStream");
            if (destinationStream.CanWrite == false)
                throw new ArgumentException("'destinationStream' is not suppot 'Write'.", "destinationStream");
            _destinationStream = destinationStream;
        }

        protected virtual void WriteToDestinationStream(Stream destinationStream, byte[] buffer, int offset, int count)
        {
            destinationStream.Write(buffer, offset, count);
        }

        protected virtual void FlushDestinationStream(Stream destinationStream, bool isEndOfData)
        {
            if (_isEndOfWriting == false)
                destinationStream.Flush();
        }

        protected void CloseDestinationStream()
        {
            _destinationStream.Close();
        }
    }
}
using System;
using System.IO;
using Utility;
using Utility.IO;

namespace ZipUtility.IO
{
    public abstract class ZipContentOutputStream
        : IOutputByteStream<UInt64>
    {
        private bool _isDisposed;
        private ulong? _size;
        private IOutputByteStream<UInt64> _destinationStream;
        private ulong _position;
        private bool _isEndOfWriting;

        public ZipContentOutputStream(IOutputByteStream<UInt64> baseStream, ulong? size)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));

            _isDisposed = false;
            _size = size;
            _destinationStream = null;
            _position = 0;
            _isEndOfWriting = false;
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
            _position += (ulong)written;
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
                        _destinationStream.Dispose();
                        _destinationStream = null;
                    }
                }
                _isDisposed = true;

            }
        }

        protected void SetDestinationStream(IOutputByteStream<UInt64> destinationStream)
        {
            if (destinationStream == null)
                throw new ArgumentNullException(nameof(destinationStream));
            _destinationStream = destinationStream;
        }

        protected virtual int WriteToDestinationStream(IOutputByteStream<UInt64> destinationStream, IReadOnlyArray<byte> buffer, int offset, int count)
        {
            return destinationStream.Write(buffer, offset, count);
        }

        protected virtual void FlushDestinationStream(IOutputByteStream<UInt64> destinationStream, bool isEndOfData)
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
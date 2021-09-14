using SevenZip.Compression.Lzma;
using System;
using System.IO;
using System.Threading.Tasks;
using Utility;
using Utility.IO;

namespace ZipUtility.Compression.Lzma.DataStream
{
    public class LzmaInputStream
        : Stream
    {
        private bool _isDisposed;
        private Stream _baseStream;
        private long _size;
        private Stream _inputStream;
        private Stream _outputStream;
        private long _totalCount;
        private bool _isCorruptedStream;
        private Task _backgroundDecorder;

        public LzmaInputStream(Stream baseStream, bool useEndOfStreamMarker, long? offset, long packedSize, long size, bool leaveOpen = false)
        {
            if (offset.HasValue && offset.Value < 0)
                throw new ArgumentException();
            if (packedSize < 0)
                throw new ArgumentException();
            if (size < 0)
                throw new ArgumentException();

            _isDisposed = false;
            _baseStream = new BufferedInputStream(new PartialInputStream(baseStream, offset, packedSize, leaveOpen));
            _size = size;
            var fifo = new FifoBuffer();
            _inputStream = fifo.GetInputStream();
            _outputStream = fifo.GetOutputStream(false);
            _totalCount = 0;
            _isCorruptedStream = false;
            var decoder = new LzmaDecoder();
            _backgroundDecorder = Task.Run(() =>
            {
                try
                {
                    try
                    {
                        var error = false;
                        var header = _baseStream.ReadBytes(9);
                        if (header.Length != 9)
                        {
                            lock (this)
                            {
                                error = true;
                                _isCorruptedStream = true;
                            }
                        }
                        if (error == false)
                        {
                            var majorVersion = header[0];
                            var minorVersion = header[1];
                            var propertyLength = header.ToUInt16LE(2);
                            if (propertyLength != 5)
                            {
                                lock (this)
                                {
                                    error = true;
                                    _isCorruptedStream = true;
                                }
                            }
                        }
                        if (error == false)
                        {
                            var properties = new byte[5];
                            header.CopyTo(4, properties, 0, 5);
                            decoder.SetDecoderProperties(properties);
                            decoder.Code(_baseStream, _outputStream, packedSize, size, null);
                        }
                    }
                    finally
                    {
                        lock (this)
                        {
                            if (_baseStream != null)
                            {
                                _baseStream.Dispose();
                                _baseStream = null;
                            }
                            if (_outputStream != null)
                            {
                                _outputStream.Dispose();
                                _outputStream = null;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // foreground task からの一方的な Dispose などに備え、例外はすべて無視する
                }
            });
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            lock (this)
            {
                if (_isCorruptedStream)
                    throw new IOException("The stream is corrupted.");
            }
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException();

            var length = _inputStream.Read(buffer, offset, count);
            if (length > 0)
            {
                // 何らかのデータが読み込めた場合
                _totalCount += length;
            }
            else if (_totalCount != _size)
            {
                //  ストリームの終端に達しているが、これまでに読み込んだデータの長さが期待された値と一致しない場合
                throw new IOException("Size not match");
            }
            else
            {
                // ストリームの終端に達していて、かつこれまでに読み込んだデータの長さが期待された値と一致している場合

                // background task 終了を待機する
                _backgroundDecorder.Wait();
                if (_backgroundDecorder.Exception != null)
                {
                    lock (this)
                    {
                        _isCorruptedStream = true;
                    }
                }
            }
            return length;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            lock (this)
            {
                if (_isCorruptedStream)
                    throw new IOException("The stream is corrupted.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    lock (this)
                    {
                        if (_inputStream != null)
                        {
                            _inputStream.Dispose();
                            _inputStream = null;
                        }
                        if (_outputStream != null)
                        {
                            _outputStream.Dispose();
                            _outputStream = null;
                        }
                        if (_baseStream != null)
                        {
                            _baseStream.Dispose();
                            _baseStream = null;
                        }
                    }
                }
                _isDisposed = true;
                base.Dispose(disposing);
            }
        }

        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}

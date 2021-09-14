using SevenZip.Compression.Lzma;
using System;
using System.IO;
using System.Threading.Tasks;
using Utility;
using Utility.IO;

namespace ZipUtility.Compression.Lzma.DataStream
{
    public class LzmaInputStream
        : InputStream
    {
        private bool _isDisposed;
        private Stream _baseStream;
        private bool _isCorruptedStream;
        private Task _backgroundDecorder;

        public LzmaInputStream(Stream baseStream, bool useEndOfStreamMarker, long? offset, long packedSize, long size, bool leaveOpen = false)
            : base(baseStream, offset, packedSize, size, false)
        {
            _isDisposed = false;
            _baseStream = new BufferedInputStream(new PartialInputStream(baseStream, offset, packedSize, leaveOpen));
            var fifo = new FifoBuffer();
            _isCorruptedStream = false;
            SetSourceStream(fifo.GetInputStream());
            _backgroundDecorder = Task.Run(() =>
            {
                try
                {
                    var outputStream = fifo.GetOutputStream(false);
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
                            var decoder = new LzmaDecoder();
                            decoder.SetDecoderProperties(properties);
                            decoder.Code(_baseStream, outputStream, packedSize, size, null);
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
                        }
                        if (outputStream != null)
                        {
                            outputStream.Dispose();
                            outputStream = null;
                        }
                    }
                }
                catch (Exception)
                {
                    // foreground task からの一方的な Dispose などに備え、例外はすべて無視する
                }
            });
        }

        protected override int ReadFromSourceStream(Stream sourceStream, byte[] buffer, int offset, int count)
        {
            lock (this)
            {
                if (_isCorruptedStream)
                    throw new IOException("The stream is corrupted.");
            }
            return sourceStream.Read(buffer, offset, count);
        }

        protected override void OnEndOfStream()
        {
            // background task の終了を待機する
            _backgroundDecorder.Wait();
            if (_backgroundDecorder.Exception != null)
            {
                lock (this)
                {
                    _isCorruptedStream = true;
                }
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
    }
}

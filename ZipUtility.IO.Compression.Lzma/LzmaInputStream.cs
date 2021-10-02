using System;
using System.IO;
using System.Threading.Tasks;
using Utility;
using Utility.IO;
using Utility.IO.Compression.Lzma;

namespace ZipUtility.IO.Compression.Lzma
{
    public class LzmaInputStream
        : ZipContentInputStream
    {
        private bool _isDisposed;
        private IInputByteStream<UInt64> _baseStream;
        private bool _isCorruptedStream;
        private Task _backgroundDecorder;

        public LzmaInputStream(IInputByteStream<UInt64> baseStream, ulong size)
            : base(baseStream, size)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                _isDisposed = false;
                _baseStream = baseStream.WithCache();
                var fifo = new FifoBuffer();
                _isCorruptedStream = false;
                SetSourceStream(fifo.GetInputByteStream());
                _backgroundDecorder = Task.Run(() =>
                {
                    try
                    {
                        var outputStream = fifo.GetOutputByteStream(false);
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
                                decoder.SetDecoderProperties(properties.AsReadOnly());
                                decoder.Code(_baseStream, outputStream, size, null);
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
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }

        protected override int ReadFromSourceStream(IInputByteStream<UInt64> sourceStream, byte[] buffer, int offset, int count)
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

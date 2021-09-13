using SevenZip;
using SevenZip.Compression.Lzma;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utility.IO;

namespace ZipUtility.Compression.Lzma.DataStream
{
    public class LzmaOutputStream
        : Stream
    {
        private const uint _MAXIMUM_DICTIONAEY_SIZE = 1 << 30; // == 1GB
        private const uint _DEFAULT_DICTIONAEY_SIZE = 1 << 24; // == 16MB
        private const uint _MINIMUM_DICTIONAEY_SIZE = 1 << 12; // == 4KB
        private const byte _LZMA_COMPRESSION_MAJOR_VERSION = 21;
        private const byte _LZMA_COMPRESSION_MINOR_VERSION = 3;
        private const ushort _LZMA_PROPERT_SIZE = 5;
        private bool _isDisposed;
        private Stream _baseStream;
        private long? _size;
        private Stream _inputStream;
        private Stream _outputStream;
        private CancellationTokenSource _cancellationTokenSource;
        private long _totalCount;
        private Task _backgroundEncoder;

        public LzmaOutputStream(Stream baseStream, bool useEndOfStreamMarker, long? offset, long? size, bool leaveOpen)
            : this(baseStream, null, useEndOfStreamMarker, offset, size, leaveOpen)
        {
        }

        public LzmaOutputStream(Stream baseStream, uint dictionarySize, bool useEndOfStreamMarker, long? offset, long? size, bool leaveOpen)
            : this(baseStream, (uint?)dictionarySize, useEndOfStreamMarker, offset, size, leaveOpen)
        {
        }

        private LzmaOutputStream(Stream baseStream, uint? dictionarySize, bool useEndOfStreamMarker, long? offset, long? size, bool leaveOpen)
        {
            if (offset.HasValue && offset.Value < 0)
                throw new ArgumentException();
            if (size.HasValue && size.Value < 0)
                throw new ArgumentException();

            _isDisposed = false;
            _baseStream = new PartialOutputStream(baseStream, offset, null, leaveOpen);
            _size = size;
            var fifo = new FIFOBuffer();
            _inputStream = fifo.GetInputStream();
            _outputStream = fifo.GetOutputStream(false);
            _cancellationTokenSource = new CancellationTokenSource();
            _totalCount = 0;

            var encoder = new LzmaEncoder();
            encoder.SetCoderProperties(new CoderProperties
            {
                DictionarySize = DecideDictionarySize(dictionarySize, size),
                PosStateBits = 2,
                LitContextBits = 3,
                LitPosBits = 0,
                Algorithm = 1,
                NumFastBytes = 128,
                MatchFinder = "bt4",
                EndMarker = useEndOfStreamMarker,
            });

            _backgroundEncoder = Task.Run(() =>
            {
                try
                {
                    var header = new byte[4];
                    header[0] = _LZMA_COMPRESSION_MAJOR_VERSION;
                    header[1] = _LZMA_COMPRESSION_MINOR_VERSION;
                    header[2] = (byte)(_LZMA_PROPERT_SIZE >> 0);
                    header[3] = (byte)(_LZMA_PROPERT_SIZE >> 8);
                    _baseStream.Write(header, 0, header.Length);
                    encoder.WriteCoderProperties(_baseStream);
                    encoder.Code(_inputStream, _baseStream, -1, -1, null);
                }
                catch (Exception)
                {
                    // foreground task からの一方的な Dispose などに備え、例外はすべて無視する
                }
                finally
                {
                    lock (this)
                    {
                        if (_inputStream != null)
                        {
                            _inputStream.Dispose();
                            _inputStream = null;
                        }
                        if (_baseStream != null)
                        {
                            _baseStream.Dispose();
                            _baseStream = null;
                        }
                    }
                }
            });
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > buffer.Length)
                throw new ArgumentException();
            if (_size.HasValue && _totalCount + count > _size.Value)
                throw new InvalidOperationException("Can not write any more.");

            _outputStream.Write(buffer, offset, count);
            _totalCount += count;
            InternalFlush();
        }

        public override void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _outputStream?.Flush();
            InternalFlush();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // コンストラクタで書き込みデータのサイズ指定がない場合、
                    // または意図しないタイミングでのストリームの close の場合にここで待ち合わせが発生する。
                    // 
                    // Dispose の中で待ち合わせというのはかなり行儀が悪いが他に待ち合わせる場所がない。
                    // background task の LZMA エンコーダの動作としては、入力ストリームの close (つまり_outputStream.Dispose)を検出してから
                    // EOS書き込みなどの後処理を行ってその後でエンコードを終了する。
                    // ここで待ち合わせを行わないと、background task で LZMA エンコーダが後処理のためにファイルアクセスをしている最中に
                    // foreground task が同じファイルをアクセスに(おそらくは書き込みに)行ってしまうので、ファイルがクラッシュする可能性がある。
                    // それを避けるために、(やむを得ず)ここで待ち合わせる。
                    try
                    {
                        CloseSourceStreamAndWaitForBackgroundTaskCompletion();
                    }
                    catch (Exception)
                    {
                    }

                    lock (this)
                    {
                        if (_cancellationTokenSource != null)
                        {
                            _cancellationTokenSource.Dispose();
                            _cancellationTokenSource = null;
                        }
                        if (_inputStream != null)
                        {
                            _inputStream.Dispose();
                            _inputStream = null;
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

        private static UInt32 DecideDictionarySize(UInt32? dictionarySize, long? fileSize)
        {
            if (dictionarySize.HasValue)
            {
                if (dictionarySize.Value > _MAXIMUM_DICTIONAEY_SIZE)
                    throw new ArgumentException("Too large dictionary size");
                if (dictionarySize.Value < _MINIMUM_DICTIONAEY_SIZE)
                    return _MINIMUM_DICTIONAEY_SIZE;
                return dictionarySize.Value;
            }
            else if (fileSize.HasValue)
            {
                var shiftCount = 16;
                while (shiftCount < 24 && (1U << shiftCount) < fileSize.Value)
                {
                    ++shiftCount;
                }
                return 1U << shiftCount;
            }
            else
                return _DEFAULT_DICTIONAEY_SIZE;
        }

        private void InternalFlush()
        {
            try
            {
                if (_size.HasValue && _totalCount >= _size.Value && _outputStream != null)
                {
                    // 書き込むデータのサイズ指定があり、かつこれまでに書き込んだデータの長さが与えられたサイズに達していた場合
                    // ストリームを close して、 background task の終了を待ち合わせる
                    CloseSourceStreamAndWaitForBackgroundTaskCompletion();
                }
            }
            catch (NullReferenceException ex)
            {
                throw new IOException("Stream is closed.", ex);
            }
            catch (ObjectDisposedException ex)
            {
                throw new IOException("Stream is closed.", ex);
            }
            catch (OperationCanceledException ex)
            {
                throw new IOException("Stream is closed.", ex);
            }
        }

        private void CloseSourceStreamAndWaitForBackgroundTaskCompletion()
        {
            // _outputStream を Dispose して、 background task が終了処理を始めるのを期待する。
            if (_outputStream != null)
            {
                _outputStream.Dispose();
                _outputStream = null;
            }

            // background task が終了するのを待ち合わせる。
            _backgroundEncoder.Wait();
        }
    }
}
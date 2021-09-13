﻿using System;
using System.IO;
using Utility.IO;

namespace ZipUtility.Compression.Deflate.DataStream
{
    public class InflateStream
        : Stream
    {
        private bool _isDisposed;
        private Stream _baseStream;
        private long? _size;
        private long _totalCount;

        public InflateStream(Stream baseStream, int level, long? offset, long? size, bool leaveOpen)
        {
            if (offset.HasValue && offset.Value < 0)
                throw new ArgumentException();
            if (size.HasValue && size.Value < 0)
                throw new ArgumentException();

            _isDisposed = false;
            _baseStream =
                new Ionic.Zlib.DeflateStream(
                    new PartialOutputStream(baseStream, offset, null, leaveOpen),
                    Ionic.Zlib.CompressionMode.Compress,
                    (Ionic.Zlib.CompressionLevel)level,
                    false);
            _size = size;
            _totalCount = 0;
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

            _baseStream.Write(buffer, offset, count);
            _totalCount += count;
        }

        public override void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            _baseStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_baseStream != null)
                    {
                        _baseStream.Dispose();
                        _baseStream = null;
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
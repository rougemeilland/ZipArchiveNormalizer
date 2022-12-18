using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utility.IO;

namespace ZipUtility
{
    class SingleVolumeZipInputStream
        : IZipInputStream, IVirtualZipFile
    {
        private readonly IRandomInputByteStream<UInt64> _baseStream;

        private bool _isDisposed;

        public SingleVolumeZipInputStream(FileInfo file)
        {
            _isDisposed = false;
            var success = false;
            var stream = file.OpenRead().AsInputByteStream();
            try
            {
                if (stream is not IRandomInputByteStream<UInt64> randomAccessStream)
                    throw new NotSupportedException();
                _baseStream = randomAccessStream;

                success = true;
            }
            finally
            {
                if (!success)
                    stream.Dispose();
            }
        }

        public UInt64 Length
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return _baseStream.Length;
            }

            set
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                _baseStream.Length = value;
            }
        }

        public ZipStreamPosition Position
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return new ZipStreamPosition(0, _baseStream.Position, this);
            }
        }

        public void Seek(ZipStreamPosition offset)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            var rawOffset = (IZipStreamPositionValue)offset;
            if (rawOffset.DiskNumber > 0)
                throw new BadZipFileFormatException("Invalid disk number");
            _baseStream.Seek(rawOffset.OffsetOnTheDisk);
        }

        public Int32 Read(Span<byte> buffer)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return _baseStream.Read(buffer);
        }

        public Task<Int32> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return _baseStream.ReadAsync(buffer, cancellationToken);
        }

        public bool IsMultiVolumeZipStream => false;

        public ZipStreamPosition GetPosition(UInt32 diskNumber, UInt64 offset)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return new ZipStreamPosition(diskNumber, offset, this);
        }

        public ZipStreamPosition LastDiskStartPosition
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return new ZipStreamPosition(0, 0, this);
            }
        }

        public UInt64 LastDiskSize
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return _baseStream.Length;
            }
        }

        ZipStreamPosition? IVirtualZipFile.Add(ZipStreamPosition position, UInt64 offset)
        {
            var rawPosition = (IZipStreamPositionValue)position;
            if (rawPosition.DiskNumber > 0)
                throw new BadZipFileFormatException("Invalid disk number");

            checked
            {
                var newPosition = rawPosition.OffsetOnTheDisk + offset;
                if (newPosition > _baseStream.Length)
                    throw new IOException("Position is out of ZIP file.");
                return new ZipStreamPosition(0, newPosition, this);
            }
        }

        ZipStreamPosition? IVirtualZipFile.Subtract(ZipStreamPosition position, UInt64 offset)
        {
            var rawPosition = (IZipStreamPositionValue)position;
            if (rawPosition.DiskNumber > 0)
                throw new BadZipFileFormatException("Invalid disk number");

            checked
            {
                return new ZipStreamPosition(0, rawPosition.OffsetOnTheDisk - offset, this);
            }
        }

        UInt64 IVirtualZipFile.Subtract(ZipStreamPosition position1, ZipStreamPosition position2)
        {
            var rawPosition1 = (IZipStreamPositionValue)position1;
            if (rawPosition1.DiskNumber > 0)
                throw new BadZipFileFormatException("Invalid disk number");
            var rawPosition2 = (IZipStreamPositionValue)position2;
            if (rawPosition2.DiskNumber > 0)
                throw new BadZipFileFormatException("Invalid disk number");

            checked
            {
                return rawPosition1.OffsetOnTheDisk - rawPosition2.OffsetOnTheDisk;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                    _baseStream.Dispose();
                _isDisposed = true;
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!_isDisposed)
            {
                await _baseStream.DisposeAsync().ConfigureAwait(false);
                _isDisposed = true;
            }
        }
    }
}

using System;
using System.IO;
using Utility.IO;

namespace ZipUtility
{
    class SingleVolumeZipInputStream
        : IZipInputStream, IVirtualZipFile
    {
        private bool _isDisposed;
        private IRandomInputByteStream<UInt64> _baseStream;

        public SingleVolumeZipInputStream(FileInfo file)
        {
            _isDisposed = false;
            var success = false;
            var stream = file.OpenRead().AsInputByteStream();
            try
            {
                _baseStream = stream as IRandomInputByteStream<UInt64>;
                if (_baseStream == null)
                    throw new NotSupportedException();

                success = true;
            }
            finally
            {
                if (!success)
                    stream?.Dispose();
            }
        }

        public ulong Length
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

            var rawOffset = offset as IZipStreamPositionValue;
            if (rawOffset == null)
                throw new Exception();
            if (rawOffset.DiskNumber > 0)
                throw new BadZipFileFormatException("Invalid disk number");
            _baseStream.Seek(rawOffset.OffsetOnTheDisk);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return _baseStream.Read(buffer, offset, count);
        }

        public bool IsMultiVolumeZipStream => false;

        public ZipStreamPosition GetPosition(uint diskNumber, ulong offset)
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

        public ulong LastDiskSize
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return _baseStream.Length;
            }
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
                    if (_baseStream != null)
                    {
                        _baseStream.Dispose();
                        _baseStream = null;
                    }
                }
                _isDisposed = true;
            }
        }

        ZipStreamPosition? IVirtualZipFile.Add(ZipStreamPosition position, ulong offset)
        {
            var rawPosition = position as IZipStreamPositionValue;
            if (rawPosition == null)
                throw new Exception();
            if (rawPosition.DiskNumber > 0)
                throw new BadZipFileFormatException("Invalid disk number");
#if DEBUG
            checked
#endif
            {
                var newPosition = rawPosition.OffsetOnTheDisk + offset;
                if (newPosition > _baseStream.Length)
                    throw new IOException("Position is out of ZIP file.");
                return new ZipStreamPosition(0, newPosition, this);
            }
        }

        ZipStreamPosition? IVirtualZipFile.Subtract(ZipStreamPosition position, ulong offset)
        {
            var rawPosition = position as IZipStreamPositionValue;
            if (rawPosition == null)
                throw new Exception();
            if (rawPosition.DiskNumber > 0)
                throw new BadZipFileFormatException("Invalid disk number");
#if DEBUG
            checked
#endif
            {
                return new ZipStreamPosition(0, rawPosition.OffsetOnTheDisk - offset, this);
            }
        }

        ulong IVirtualZipFile.Subtract(ZipStreamPosition position1, ZipStreamPosition position2)
        {
            var rawPosition1 = position1 as IZipStreamPositionValue;
            if (rawPosition1 == null)
                throw new Exception();
            if (rawPosition1.DiskNumber > 0)
                throw new BadZipFileFormatException("Invalid disk number");
            var rawPosition2 = position2 as IZipStreamPositionValue;
            if (rawPosition2 == null)
                throw new Exception();
            if (rawPosition2.DiskNumber > 0)
                throw new BadZipFileFormatException("Invalid disk number");
#if DEBUG
            checked
#endif
            {
                return rawPosition1.OffsetOnTheDisk - rawPosition2.OffsetOnTheDisk;
            }
        }
    }
}

using System;
using Utility;
using Utility.IO;

namespace ZipUtility
{
    static class StreamExtensions
    {
        private class PartialInputStreamForZipInputStream
            : PartialInputStream<UInt64, ZipStreamPosition>
        {
            public PartialInputStreamForZipInputStream(IInputByteStream<ZipStreamPosition> baseStream, bool leaveOpen = false)
                : base(baseStream, leaveOpen)
            {
            }

            public PartialInputStreamForZipInputStream(IInputByteStream<ZipStreamPosition> baseStream, UInt64 size, bool leaveOpen = false)
                : base(baseStream, size, leaveOpen)
            {
            }

            public PartialInputStreamForZipInputStream(IInputByteStream<ZipStreamPosition> baseStream, UInt64? size, bool leaveOpen = false)
                : base(baseStream, size, leaveOpen)
            {
            }

            protected override UInt64 ZeroPositionValue => 0;

            protected override UInt64 AddPosition(UInt64 x, UInt64 y)
            {
#if DEBUG
                checked
#endif
                {
                    return x + y;
                }
            }
        }

        private class PartialRandomInputStreamForZipInputStream
            : PartialRandomInputStream<UInt64, ZipStreamPosition>
        {
            public PartialRandomInputStreamForZipInputStream(IZipInputStream baseStream, bool leaveOpen = false)
                : base(baseStream, leaveOpen)
            {
            }

            public PartialRandomInputStreamForZipInputStream(IZipInputStream baseStream, UInt64 size, bool leaveOpen = false)
                : base(baseStream, size, leaveOpen)
            {
            }

            public PartialRandomInputStreamForZipInputStream(IZipInputStream baseStream, ZipStreamPosition offset, UInt64 size, bool leaveOpen = false)
                : base(baseStream, offset, size, leaveOpen)
            {
            }

            public PartialRandomInputStreamForZipInputStream(IZipInputStream baseStream, ZipStreamPosition? offset, UInt64? size, bool leaveOpen = false)
                : base(baseStream, offset, size, leaveOpen)
            {
            }

            protected IZipInputStream SourceStream => (BaseStream as IZipInputStream) ?? throw new InternalLogicalErrorException();

            protected override UInt64 ZeroPositionValue => 0;

            protected override ZipStreamPosition EndBasePositionValue => SourceStream.LastDiskStartPosition + SourceStream.LastDiskSize;

            protected override (bool Success, ZipStreamPosition Position) AddBasePosition(ZipStreamPosition x, UInt64 y)
            {
                try
                {
                    checked
                    {
                        return (true, x + y);
                    }
                }
                catch (OverflowException)
                {
                    return (false, EndBasePositionValue);
                }
            }

            protected override (bool Success, UInt64 Position) AddPosition(UInt64 x, UInt64 y)
            {
                try
                {
                    checked
                    {
                        return (true, x + y);
                    }
                }
                catch (OverflowException)
                {
                    return (false, 0);
                }
            }

            protected override (bool Success, UInt64 Distance) GetDistanceBetweenBasePositions(ZipStreamPosition x, ZipStreamPosition y)
            {
                return x >= y ? (true, x - y) : (false, 0);
            }

            protected override (bool Success, UInt64 Distance) GetDistanceBetweenPositions(UInt64 x, UInt64 y)
            {
                return x >= y ? (true, x - y) : (false, 0);
            }
        }

        public static IInputByteStream<UInt64> AsPartial(this IZipInputStream baseStream, ZipStreamPosition offset, UInt64 size)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                baseStream.Seek(offset);
                return new PartialInputStreamForZipInputStream(baseStream, size, true);
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }

        public static IRandomInputByteStream<UInt64> AsRandomPartial(this IZipInputStream baseStream, ZipStreamPosition offset, UInt64 size)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new PartialRandomInputStreamForZipInputStream(baseStream, offset, size, true);
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }
    }
}

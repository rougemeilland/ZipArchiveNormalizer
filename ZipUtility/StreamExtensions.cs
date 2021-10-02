using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            public PartialInputStreamForZipInputStream(IInputByteStream<ZipStreamPosition> baseStream, ulong size, bool leaveOpen = false)
                : base(baseStream, size, leaveOpen)
            {
            }

            public PartialInputStreamForZipInputStream(IInputByteStream<ZipStreamPosition> baseStream, ulong? size, bool leaveOpen = false)
                : base(baseStream, size, leaveOpen)
            {
            }

            protected override ulong ZeroPositionValue => 0;

            protected override ulong AddPosition(ulong x, ulong y)
            {
                checked
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

            public PartialRandomInputStreamForZipInputStream(IZipInputStream baseStream, ulong size, bool leaveOpen = false)
                : base(baseStream, size, leaveOpen)
            {
            }

            public PartialRandomInputStreamForZipInputStream(IZipInputStream baseStream, ZipStreamPosition offset, ulong size, bool leaveOpen = false)
                : base(baseStream, offset, size, leaveOpen)
            {
            }

            public PartialRandomInputStreamForZipInputStream(IZipInputStream baseStream, ZipStreamPosition? offset, ulong? size, bool leaveOpen = false)
                : base(baseStream, offset, size, leaveOpen)
            {
            }

            protected IZipInputStream SourceStream => BaseStream as IZipInputStream;

            protected override ulong ZeroPositionValue => 0;

            protected override ZipStreamPosition EndBasePositionValue => SourceStream.LastDiskStartPosition + SourceStream.LastDiskSize;

            protected override ZipStreamPosition AddBasePosition(ZipStreamPosition x, ulong y)
            {
                return x + y;
            }

            protected override ulong AddPosition(ulong x, ulong y)
            {
                checked
                {
                    return x + y;
                }
            }

            protected override ulong GetDistanceBetweenBasePositions(ZipStreamPosition x, ZipStreamPosition y)
            {
                return x - y;
            }

            protected override ulong GetDistanceBetweenPositions(ulong x, ulong y)
            {
                checked
                {
                    return x - y;
                }
            }
        }

        public static IInputByteStream<UInt64> AsPartial(this IZipInputStream baseStream, ZipStreamPosition offset, UInt64 size)
        {
            try
            {
                if (baseStream == null)
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
                if (baseStream == null)
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

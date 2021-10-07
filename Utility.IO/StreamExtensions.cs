using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Utility;

namespace Utility.IO
{
    public static class StreamExtensions
    {
        private class ReverseByteSequenceByByteStream
            : ReverseByteSequenceByByteStreamEnumerable<UInt64>
        {
            public ReverseByteSequenceByByteStream(IRandomInputByteStream<UInt64> inputStream, UInt64 offset, UInt64 count, bool leaveOpen, Action<int> progressAction)
                : base(inputStream, offset, count, leaveOpen, progressAction)
            {
            }

            protected override UInt64 AddPositionAndDistance(UInt64 position, UInt64 distance)
            {
#if DEBUG
                checked
#endif
                {
                    return position + distance;
                }
            }

            protected override int GetDistanceBetweenPositions(UInt64 position1, UInt64 distance2)
            {
#if DEBUG
                checked
#endif
                {
                    return (int)(position1 - distance2);
                }
            }

            protected override ulong SubtractBufferSizeFromPosition(ulong position, uint distance)
            {
#if DEBUG
                checked
#endif
                {
                    return position - distance;
                }
            }
        }

        private class BufferedInputStreamUInt64
            : BufferedInputStream<UInt64>
        {
            public BufferedInputStreamUInt64(IInputByteStream<ulong> baseStream, bool leaveOpen)
                : base(baseStream, leaveOpen)
            {
            }

            public BufferedInputStreamUInt64(IInputByteStream<ulong> baseStream, int bufferSize, bool leaveOpen)
                : base(baseStream, bufferSize, leaveOpen)
            {
            }

            protected override ulong ZeroPositionValue => 0;

            protected override ulong AddPosition(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x + y;
                }
            }
        }

        private class BufferedOutputStreamUInt64
            : BufferedOutputStream<UInt64>
        {
            public BufferedOutputStreamUInt64(IOutputByteStream<ulong> baseStream, bool leaveOpen)
                : base(baseStream, leaveOpen)
            {
            }

            public BufferedOutputStreamUInt64(IOutputByteStream<ulong> baseStream, int bufferSize, bool leaveOpen)
                : base(baseStream, bufferSize, leaveOpen)
            {
            }

            protected override ulong ZeroPositionValue => 0;

            protected override ulong AddPosition(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x + y;
                }
            }
        }

        private class BufferedRandomInputStreamUInt64
            : BufferedRandomInputStream<ulong>
        {

            public BufferedRandomInputStreamUInt64(IRandomInputByteStream<ulong> baseStream, bool leaveOpen)
                : base(baseStream, leaveOpen)
            {
            }

            public BufferedRandomInputStreamUInt64(IRandomInputByteStream<ulong> baseStream, int bufferSize, bool leaveOpen)
                : base(baseStream, bufferSize, leaveOpen)
            {
            }

            protected override ulong AddPosition(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x + y;
                }
            }
        }

        private class BufferedRandomOutputStreamUint64
            : BufferedRandomOutputStream<UInt64>
        {
            public BufferedRandomOutputStreamUint64(IRandomOutputByteStream<UInt64> baseStream, bool leaveOpen)
                : base(baseStream, leaveOpen)
            {
            }

            public BufferedRandomOutputStreamUint64(IRandomOutputByteStream<UInt64> baseStream, int bufferSize, bool leaveOpen)
                : base(baseStream, bufferSize, leaveOpen)
            {
            }

            protected override ulong ZeroPositionValue => 0;

            protected override ulong AddPosition(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x + y;
                }
            }

            protected override ulong GetDistanceBetweenPositions(ulong x, ulong y)
            {
                return x - y;
            }
        }

        private class PartialInputStreamUint64
            : PartialInputStream<UInt64, UInt64>
        {
            public PartialInputStreamUint64(IInputByteStream<ulong> baseStream, bool leaveOpen)
                : base(baseStream, leaveOpen)
            {
            }

            public PartialInputStreamUint64(IInputByteStream<ulong> baseStream, ulong size, bool leaveOpen)
                : base(baseStream, size, leaveOpen)
            {
            }

            public PartialInputStreamUint64(IInputByteStream<ulong> baseStream, ulong? size, bool leaveOpen)
                : base(baseStream, size, leaveOpen)
            {
            }

            protected override ulong ZeroPositionValue => 0;

            protected override ulong AddPosition(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x + y;
                }
            }
        }

        private class PartialOutputStreamUint64
            : PartialOutputStream<UInt64, UInt64>
        {
            public PartialOutputStreamUint64(IOutputByteStream<ulong> baseStream, bool leaveOpen)
                : base(baseStream, leaveOpen)
            {
            }

            public PartialOutputStreamUint64(IOutputByteStream<ulong> baseStream, ulong size, bool leaveOpen)
                : base(baseStream, size, leaveOpen)
            {
            }

            public PartialOutputStreamUint64(IOutputByteStream<ulong> baseStream, ulong? size, bool leaveOpen)
                : base(baseStream, size, leaveOpen)
            {
            }

            protected override ulong ZeroPositionValue => 0;

            protected override ulong AddPosition(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x + y;
                }
            }
        }

        private class PartialRandomInputStreamUint64
            : PartialRandomInputStream<UInt64, UInt64>
        {
            private IRandomInputByteStream<ulong> _baseStream;

            public PartialRandomInputStreamUint64(IRandomInputByteStream<ulong> baseStream, bool leaveOpen = false)
                : base(baseStream, leaveOpen)
            {
                _baseStream = baseStream;
            }

            public PartialRandomInputStreamUint64(IRandomInputByteStream<ulong> baseStream, ulong size, bool leaveOpen = false)
                : base(baseStream, size, leaveOpen)
            {
                _baseStream = baseStream;
            }

            public PartialRandomInputStreamUint64(IRandomInputByteStream<ulong> baseStream, ulong offset, ulong size, bool leaveOpen = false)
                : base(baseStream, offset, size, leaveOpen)
            {
                _baseStream = baseStream;
            }

            public PartialRandomInputStreamUint64(IRandomInputByteStream<ulong> baseStream, ulong? offset, ulong? size, bool leaveOpen = false)
                : base(baseStream, offset, size, leaveOpen)
            {
                _baseStream = baseStream;
            }

            protected override ulong ZeroPositionValue => 0;

            protected override ulong EndBasePositionValue => _baseStream.Length;

            protected override ulong AddBasePosition(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x + y;
                }
            }

            protected override ulong AddPosition(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x + y;
                }
            }

            protected override ulong GetDistanceBetweenBasePositions(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x - y;
                }
            }

            protected override ulong GetDistanceBetweenPositions(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x - y;
                }
            }
        }

        private class PartialRandomOutputStreamUint64
            : PartialRandomOutputStream<UInt64, UInt64>
        {
            public PartialRandomOutputStreamUint64(IRandomOutputByteStream<ulong> baseStream, bool leaveOpen)
                : base(baseStream, leaveOpen)
            {
            }

            public PartialRandomOutputStreamUint64(IRandomOutputByteStream<ulong> baseStream, ulong size, bool leaveOpen)
                : base(baseStream, size, leaveOpen)
            {
            }

            public PartialRandomOutputStreamUint64(IRandomOutputByteStream<ulong> baseStream, ulong offset, ulong size, bool leaveOpen)
                : base(baseStream, offset, size, leaveOpen)
            {
            }

            public PartialRandomOutputStreamUint64(IRandomOutputByteStream<ulong> baseStream, ulong? offset, ulong? size, bool leaveOpen)
                : base(baseStream, offset, size, leaveOpen)
            {
            }

            protected override ulong ZeroPositionValue => 0;

            protected override ulong ZeroBasePositionValue => 0;

            protected override ulong AddBasePosition(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x + y;
                }
            }

            protected override ulong AddPosition(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x + y;
                }
            }

            protected override ulong GetDistanceBetweenBasePositions(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x - y;
                }
            }

            protected override ulong GetDistanceBetweenPositions(ulong x, ulong y)
            {
#if DEBUG
                checked
#endif
                {
                    return x - y;
                }
            }
        }

        private const int _COPY_TO_DEFAULT_BUFFER_SIZE = 81920;
        private const int _WRITE_BYTE_SEQUENCE_DEFAULT_BUFFER_SIZE = 81920;

        public static IInputByteStream<ulong> AsInputByteStream(this Stream baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream.CanSeek)
                    return new RandomInputByteStreamByStream(baseStream, leaveOpen);
                else
                    return new SequentialInputByteStreamByStream(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<ulong> AsOutputByteStream(this Stream baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream.CanSeek)
                    return new RandomOutputByteStreamByStream(baseStream, leaveOpen);
                else
                    return new SequentialOutputByteStreamByStream(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static Stream AsStream(this IInputByteStream<ulong> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new StreamByInputByteStream(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static Stream AsStream(this IOutputByteStream<ulong> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new StreamByOutputByteStream(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputByteStream<ulong> AsByteStream(this IEnumerable<byte> baseSequence)
        {
            return new SequentialInputByteStreamBySequence(baseSequence);
        }

        public static IInputByteStream<ulong> AsByteStream(this IInputBitStream baseStream, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new SequentialInputByteStreamByBitStream(baseStream, packingDirection, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<ulong> AsByteStream(this IOutputBitStream baseStream, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new SequentialOutputByteStreamByBitStream(baseStream, packingDirection, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputBitStream AsBitStream(this IInputByteStream<ulong> baseStream, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new SequentialInputBitStreamByByteStream(baseStream, packingDirection, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputBitStream AsBitStream(this IEnumerable<byte> baseSequence, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new SequentialInputBitStreamBySequence(baseSequence, packingDirection);
        }

        public static IOutputBitStream AsBitStream(this IOutputByteStream<ulong> baseStream, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new SequentialOutputBitStreamByByteStream(baseStream, packingDirection, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputByteStream<ulong> WithPartial(this IInputByteStream<ulong> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomInputByteStream<ulong>;
                if (baseByteStream != null)
                    return new PartialRandomInputStreamUint64(baseByteStream, leaveOpen);
                else
                    return new PartialInputStreamUint64(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }

        }

        public static IInputByteStream<ulong> WithPartial(this IInputByteStream<ulong> baseStream, ulong size, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomInputByteStream<ulong>;
                if (baseByteStream != null)
                    return new PartialRandomInputStreamUint64(baseByteStream, size, leaveOpen);
                else
                    return new PartialInputStreamUint64(baseStream, size, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputByteStream<ulong> WithPartial(this IInputByteStream<ulong> baseStream, ulong offset, ulong size, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomInputByteStream<ulong>;
                if (baseByteStream == null)
                    throw new NotSupportedException();
                else
                    return new PartialRandomInputStreamUint64(baseByteStream, offset, size, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputByteStream<ulong> WithPartial(this IInputByteStream<ulong> baseStream, ulong? offset, ulong? size, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomInputByteStream<ulong>;
                if (baseByteStream != null)
                    return new PartialRandomInputStreamUint64(baseByteStream, offset, size, leaveOpen);
                else if (offset.HasValue)
                    throw new NotSupportedException();
                else
                    return new PartialInputStreamUint64(baseStream, size, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<ulong> WithPartial(this IOutputByteStream<ulong> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomOutputByteStream<ulong>;
                if (baseByteStream != null)
                    return new PartialRandomOutputStreamUint64(baseByteStream, leaveOpen);
                else
                    return new PartialOutputStreamUint64(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<ulong> WithPartial(this IOutputByteStream<ulong> baseStream, ulong size, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomOutputByteStream<ulong>;
                if (baseByteStream != null)
                    return new PartialRandomOutputStreamUint64(baseByteStream, size, leaveOpen);
                else
                    return new PartialOutputStreamUint64(baseStream, size, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<ulong> WithPartial(this IOutputByteStream<ulong> baseStream, ulong offset, ulong size, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomOutputByteStream<ulong>;
                if (baseByteStream == null)
                    throw new NotSupportedException();
                else
                    return new PartialRandomOutputStreamUint64(baseByteStream, offset, size, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<ulong> WithPartial(this IOutputByteStream<ulong> baseStream, ulong? offset, ulong? size, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomOutputByteStream<ulong>;
                if (baseByteStream != null)
                    return new PartialRandomOutputStreamUint64(baseByteStream, offset, size, leaveOpen);
                else if (offset.HasValue)
                    throw new NotSupportedException();
                else
                    return new PartialOutputStreamUint64(baseStream, size, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputByteStream<ulong> WithCache(this IInputByteStream<ulong> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomInputByteStream<ulong>;
                if (baseByteStream != null)
                    return new BufferedRandomInputStreamUInt64(baseByteStream, leaveOpen);
                else
                    return new BufferedInputStreamUInt64(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputByteStream<ulong> WithCache(this IInputByteStream<ulong> baseStream, int cacheSize, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomInputByteStream<ulong>;
                if (baseByteStream != null)
                    return new BufferedRandomInputStreamUInt64(baseByteStream, cacheSize, leaveOpen);
                else
                    return new BufferedInputStreamUInt64(baseStream, cacheSize, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<ulong> WithCache(this IOutputByteStream<ulong> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomOutputByteStream<ulong>;
                if (baseByteStream != null)
                    return new BufferedRandomOutputStreamUint64(baseByteStream, leaveOpen);
                else
                    return new BufferedOutputStreamUInt64(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<ulong> WithCache(this IOutputByteStream<ulong> baseStream, int cacheSize, bool leaveOpen = false)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomOutputByteStream<ulong>;
                if (baseByteStream != null)
                    return new BufferedRandomOutputStreamUint64(baseByteStream, cacheSize, leaveOpen);
                else
                    return new BufferedOutputStreamUInt64(baseStream, cacheSize, leaveOpen);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this Stream baseStream, bool leaveOpen = false, Action<int> progressAction = null)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetByteSequence(progressAction: progressAction);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this IInputByteStream<ulong> baseStream, bool leaveOpen = false, Action<int> progressAction = null)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new ByteSequenceByByteStreamEnumerable<UInt64>(baseStream, null, null, leaveOpen, progressAction);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this Stream baseStream, ulong offset, bool leaveOpen = false, Action<int> progressAction = null)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetByteSequence(offset, progressAction: progressAction);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this IInputByteStream<ulong> baseStream, ulong offset, bool leaveOpen = false, Action<int> progressAction = null)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));
                var byteSteram = baseStream as IRandomInputByteStream<ulong>;
                if (byteSteram == null)
                    throw new NotSupportedException();
                if (offset < 0)
                    throw new ArgumentException();
                if (offset > byteSteram.Length)
                    throw new ArgumentException();

                return new ByteSequenceByByteStreamEnumerable<UInt64>(byteSteram, offset, byteSteram.Length - offset, leaveOpen, progressAction);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this Stream baseStream, ulong offset, ulong count, bool leaveOpen = false, Action<int> progressAction = null)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetByteSequence(offset, count, progressAction: progressAction);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this IInputByteStream<ulong> baseStream, ulong offset, ulong count, bool leaveOpen = false, Action<int> progressAction = null)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));
                var byteSteram = baseStream as IRandomInputByteStream<ulong>;
                if (byteSteram == null)
                    throw new NotSupportedException();
                if (offset < 0)
                    throw new ArgumentException();
                if (count < 0)
                    throw new ArgumentException();
                if (offset + count > byteSteram.Length)
                    throw new ArgumentException();

                return new ByteSequenceByByteStreamEnumerable<UInt64>(byteSteram, offset, count, leaveOpen, progressAction);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this Stream baseStream, bool leaveOpen = false, Action<int> progressAction = null)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetReverseByteSequence(progressAction: progressAction);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this IInputByteStream<ulong> baseStream, bool leaveOpen = false, Action<int> progressAction = null)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));
                var byteSteram = baseStream as IRandomInputByteStream<ulong>;
                if (byteSteram == null)
                    throw new NotSupportedException();

                return new ReverseByteSequenceByByteStream(byteSteram, 0, byteSteram.Length, leaveOpen, progressAction);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this Stream baseStream, ulong offset, bool leaveOpen = false, Action<int> progressAction = null)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetReverseByteSequence(offset, progressAction: progressAction);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this IInputByteStream<ulong> baseStream, ulong offset, bool leaveOpen = false, Action<int> progressAction = null)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));
                var byteSteram = baseStream as IRandomInputByteStream<ulong>;
                if (byteSteram == null)
                    throw new NotSupportedException();
                if (offset < 0)
                    throw new ArgumentException();
                if (offset > byteSteram.Length)
                    throw new ArgumentException();

                return new ReverseByteSequenceByByteStream(byteSteram, offset, byteSteram.Length - offset, leaveOpen, progressAction);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this Stream baseStream, ulong offset, ulong count, bool leaveOpen = false, Action<int> progressAction = null)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetReverseByteSequence(offset, count, progressAction: progressAction);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this IInputByteStream<ulong> baseStream, ulong offset, ulong count, bool leaveOpen = false, Action<int> progressAction = null)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));
                var byteSteram = baseStream as IRandomInputByteStream<ulong>;
                if (byteSteram == null)
                    throw new NotSupportedException();
                if (offset < 0)
                    throw new ArgumentException();
                if (count < 0)
                    throw new ArgumentException();
                if (offset + count > byteSteram.Length)
                    throw new ArgumentException();

                return new ReverseByteSequenceByByteStream(byteSteram, offset, count, leaveOpen, progressAction);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static bool StreamBytesEqual(this Stream stream1, Stream stream2, bool leaveOpen = false, Action<int> progressNotification = null)
        {
            using (var byteStream1 = stream1.AsInputByteStream(true))
            using (var byteStream2 = stream2.AsInputByteStream(true))
            {
                return byteStream1.StreamBytesEqual(byteStream2, leaveOpen, progressNotification);
            }
        }

        public static bool StreamBytesEqual(this IBasicInputByteStream stream1, IBasicInputByteStream stream2, bool leaveOpen = false, Action<int> progressNotification = null)
        {
            const int bufferSize = 81920;
#if DEBUG
            if (bufferSize % sizeof(UInt64) != 0)
                throw new Exception();
#endif
            try
            {
                if (stream1 == null)
                    throw new ArgumentNullException();
                if (stream2 == null)
                    throw new ArgumentNullException();
                var buffer1 = new byte[bufferSize];
                var buffer2 = new byte[bufferSize];
                while (true)
                {
                    // まず両方のストリームから bufferSize バイトだけ読み込みを試みる
                    var bufferCount1 = stream1.ReadBytes(buffer1, 0, buffer1.Length);
                    var bufferCount2 = stream2.ReadBytes(buffer2, 0, buffer2.Length);
                    if (bufferCount1 != bufferCount2)
                    {
                        // 実際に読み込めたサイズが異なっている場合はどちらかだけがEOFに達したということなので、ストリームの内容が異なると判断しfalseを返す。
                        return false;
                    }

                    // この時点で bufferCount1 == bufferCount2 (どちらのストリームも読み込めたサイズは同じ)

                    if (buffer1.ByteArrayEqual(0, buffer2, 0, bufferCount1) == false)
                    {
                        // バッファの内容が一致しなかった場合は false を返す。
                        return false;
                    }

                    if (bufferCount1 != buffer1.Length)
                    {
                        // どちらのストリームも同時にEOFに達したがそれまでに読み込めたデータはすべて一致していた場合
                        // 全てのデータが一致したと判断して true を返す。
                        return true;
                    }
                    if (progressNotification != null)
                    {
                        try
                        {
                            progressNotification(bufferCount1);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            finally
            {
                if (leaveOpen == false)
                {
                    stream1.Dispose();
                    stream2.Dispose();
                }
            }
        }

        public static void CopyTo(this Stream source, Stream destination, Action<int> progressNotification)
        {
            using (var sourceByteStream = source.AsInputByteStream(true))
            using (var destinationByteStream = destination.AsOutputByteStream(true))
            {
                sourceByteStream.CopyTo(destinationByteStream, _COPY_TO_DEFAULT_BUFFER_SIZE, progressNotification);
            }
        }

        public static void CopyTo(this IBasicInputByteStream source, IBasicOutputByteStream destination, Action<int> progressNotification = null)
        {
            source.CopyTo(destination, _COPY_TO_DEFAULT_BUFFER_SIZE, progressNotification);
        }

        public static void CopyTo(this Stream source, Stream destination, int bufferSize, Action<int> progressNotification)
        {
            using (var sourceByteStream = source.AsInputByteStream(true))
            using (var destinationByteStream = destination.AsOutputByteStream(true))
            {
                sourceByteStream.CopyTo(destinationByteStream, bufferSize, progressNotification);
            }
        }

        public static void CopyTo(this IBasicInputByteStream source, IBasicOutputByteStream destination, int bufferSize, Action<int> progressNotification = null)
        {
            if (source == null)
                throw new ArgumentNullException();
            if (destination == null)
                throw new ArgumentNullException();
            if (bufferSize < 1)
                throw new ArgumentException();
            var buffer = new byte[bufferSize];
            var progressCount = 0;
            while (true)
            {
                var length = source.Read(buffer, 0, buffer.Length);
                if (length <= 0)
                    break;
                destination.WriteBytes(buffer.AsReadOnly(), 0, length);
                progressCount += length;
                if (progressCount >= bufferSize)
                {
                    try
                    {
                        if (progressNotification != null)
                            progressNotification(bufferSize);
                    }
                    catch (Exception)
                    {
                    }
                    progressCount -= bufferSize;
                }
            }
            destination.Flush();
            if (progressCount > 0)
            {
                try
                {
                    if (progressNotification != null)
                        progressNotification(progressCount);
                }
                catch (Exception)
                {
                }
            }
        }

        public static byte? ReadByteOrNull(this IBasicInputByteStream inputStream)
        {
            var buffer = new byte[1];
            var length = inputStream.Read(buffer, 0, 1);
            if (length <= 0)
                return null;
            return buffer[0];
        }

        public static byte ReadByte(this IBasicInputByteStream inputStream)
        {
            return inputStream.ReadByteOrNull() ?? throw new UnexpectedEndOfStreamException();
        }

        public static IReadOnlyArray<byte> ReadBytes(this IBasicInputByteStream inputStream, ushort count) => inputStream.ReadBytes((int)count);

        public static IReadOnlyArray<byte> ReadBytes(this Stream sourceStream, int count)
        {
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (count < 0)
                throw new ArgumentException();

            return
                ReadBytes(
                    (uint)count,
                    (_buffer, _offset, _count) => sourceStream.Read(_buffer, _offset, _count));
        }

        public static IReadOnlyArray<byte> ReadBytes(this IBasicInputByteStream sourceByteStream, int count)
        {
            if (sourceByteStream == null)
                throw new ArgumentNullException(nameof(sourceByteStream));
            if (count < 0)
                throw new ArgumentException();

            return
                ReadBytes(
                    (uint)count,
                    (_buffer, _offset, _count) => sourceByteStream.Read(_buffer, _offset, _count));
        }

        public static IReadOnlyArray<byte> ReadBytes(this Stream sourceStream, uint count)
        {
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            return
                ReadBytes(
                    count,
                    (_buffer, _offset, _count) => sourceStream.Read(_buffer, _offset, _count));
        }

        public static IReadOnlyArray<byte> ReadBytes(this IBasicInputByteStream sourceByteStream, uint count)
        {
            if (sourceByteStream == null)
                throw new ArgumentNullException(nameof(sourceByteStream));

            return
                ReadBytes(
                    count,
                    (_buffer, _offset, _count) => sourceByteStream.Read(_buffer, _offset, _count));
        }

        public static IEnumerable<byte> ReadBytes(this Stream sourceStream, long count)
        {
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (count < 0)
                throw new ArgumentException();

            return
                ReadBytes(
                    (ulong)count,
                    _count => sourceStream.ReadBytes(_count));
        }

        public static IEnumerable<byte> ReadBytes(this IBasicInputByteStream sourceByteStream, long count)
        {
            if (sourceByteStream == null)
                throw new ArgumentNullException(nameof(sourceByteStream));
            if (count < 0)
                throw new ArgumentException();

            return
                ReadBytes(
                    (ulong)count,
                    _count => sourceByteStream.ReadBytes(_count));
        }

        public static IEnumerable<byte> ReadBytes(this Stream sourceStream, ulong count)
        {
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            return
                ReadBytes(
                    count,
                    _count => sourceStream.ReadBytes(_count));
        }

        public static IEnumerable<byte> ReadBytes(this IBasicInputByteStream sourceByteStream, ulong count)
        {
            if (sourceByteStream == null)
                throw new ArgumentNullException(nameof(sourceByteStream));

            return
                ReadBytes(
                    count,
                    _count => sourceByteStream.ReadBytes(_count));
        }

        public static int ReadBytes(this Stream sourceStream, byte[] buffer, int offset, int count)
        {
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            return
                ReadBytes(
                    buffer,
                    offset,
                    count,
                    (_buffer, _offset, _count) => sourceStream.Read(_buffer, _offset, _count));
        }

        public static int ReadBytes(this IBasicInputByteStream sourceByteStream, byte[] buffer, int offset, int count)
        {
            if (sourceByteStream == null)
                throw new ArgumentNullException(nameof(sourceByteStream));

            return
                ReadBytes(
                    buffer,
                    offset,
                    count,
                    (_buffer, _offset, _count) => sourceByteStream.Read(_buffer, _offset, _count));
        }

        public static IReadOnlyArray<byte> ReadAllBytes(this Stream sourceStream)
        {
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            return ReadAllBytes((_buffer, _offset, _count) => sourceStream.Read(_buffer, _offset, _count));
        }

        public static IReadOnlyArray<byte> ReadAllBytes(this IBasicInputByteStream sourceByteStream)
        {
            if (sourceByteStream == null)
                throw new ArgumentNullException(nameof(sourceByteStream));

            return ReadAllBytes((_buffer, _offset, _count) => sourceByteStream.Read(_buffer, _offset, _count));
        }

        public static Int16 ReadInt16LE(this IBasicInputByteStream sourceByteStream)
        {
            var data = sourceByteStream.ReadBytes(sizeof(Int16));
            if (data.Length != sizeof(Int16))
                throw new UnexpectedEndOfStreamException();
            return data.ToInt16LE(0);
        }

        public static Int16 ReadInt16BE(this IBasicInputByteStream sourceByteStream)
        {
            var data = sourceByteStream.ReadBytes(sizeof(Int16));
            if (data.Length != sizeof(Int16))
                throw new UnexpectedEndOfStreamException();
            return data.ToInt16BE(0);
        }

        public static UInt16 ReadUInt16LE(this IBasicInputByteStream sourceByteStream)
        {
            var data = sourceByteStream.ReadBytes(sizeof(UInt16));
            if (data.Length != sizeof(UInt16))
                throw new UnexpectedEndOfStreamException();
            return data.ToUInt16LE(0);
        }

        public static UInt16 ReadUInt16BE(this IBasicInputByteStream sourceByteStream)
        {
            var data = sourceByteStream.ReadBytes(sizeof(UInt16));
            if (data.Length != sizeof(UInt16))
                throw new UnexpectedEndOfStreamException();
            return data.ToUInt16BE(0);
        }

        public static Int32 ReadInt32LE(this IBasicInputByteStream sourceByteStream)
        {
            var data = sourceByteStream.ReadBytes(sizeof(Int32));
            if (data.Length != sizeof(Int32))
                throw new UnexpectedEndOfStreamException();
            return data.ToInt32LE(0);
        }

        public static Int32 ReadInt32BE(this IBasicInputByteStream sourceByteStream)
        {
            var data = sourceByteStream.ReadBytes(sizeof(Int32));
            if (data.Length != sizeof(Int32))
                throw new UnexpectedEndOfStreamException();
            return data.ToInt32BE(0);
        }

        public static UInt32 ReadUInt32LE(this IBasicInputByteStream sourceByteStream)
        {
            var data = sourceByteStream.ReadBytes(sizeof(UInt32));
            if (data.Length != sizeof(UInt32))
                throw new UnexpectedEndOfStreamException();
            return data.ToUInt32LE(0);
        }

        public static UInt32 ReadUInt32BE(this IBasicInputByteStream sourceByteStream)
        {
            var data = sourceByteStream.ReadBytes(sizeof(UInt32));
            if (data.Length != sizeof(UInt32))
                throw new UnexpectedEndOfStreamException();
            return data.ToUInt32BE(0);
        }

        public static Int64 ReadInt64LE(this IBasicInputByteStream sourceByteStream)
        {
            var data = sourceByteStream.ReadBytes(sizeof(Int64));
            if (data.Length != sizeof(Int64))
                throw new UnexpectedEndOfStreamException();
            return data.ToInt64LE(0);
        }

        public static Int64 ReadInt64BE(this IBasicInputByteStream sourceByteStream)
        {
            var data = sourceByteStream.ReadBytes(sizeof(Int64));
            if (data.Length != sizeof(Int64))
                throw new UnexpectedEndOfStreamException();
            return data.ToInt64BE(0);
        }

        public static UInt64 ReadUInt64LE(this IBasicInputByteStream sourceByteStream)
        {
            var data = sourceByteStream.ReadBytes(sizeof(UInt64));
            if (data.Length != sizeof(UInt64))
                throw new UnexpectedEndOfStreamException();
            return data.ToUInt64LE(0);
        }

        public static UInt64 ReadUInt64BE(this IBasicInputByteStream sourceByteStream)
        {
            var data = sourceByteStream.ReadBytes(sizeof(UInt64));
            if (data.Length != sizeof(UInt64))
                throw new UnexpectedEndOfStreamException();
            return data.ToUInt64BE(0);
        }

        public static void Write(this Stream stream, byte[] buffer)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            stream.Write(buffer, 0, buffer.Length);
        }

        public static void Write(this Stream stream, IReadOnlyArray<byte> buffer)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            stream.Write(buffer, 0, buffer.Length);
        }

        public static void Write(this Stream stream, IReadOnlyArray<byte> buffer, int offset, int count)
        {
            stream.Write(buffer.GetRawArray(), offset, count);
        }

        public static void Write(this Stream stream, IEnumerable<byte> sequence)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));

            foreach (var buffer in sequence.ToChunkOfArray(_WRITE_BYTE_SEQUENCE_DEFAULT_BUFFER_SIZE))
                stream.Write(buffer, 0, buffer.Length);
        }

        public static void WriteByte(this IBasicOutputByteStream stream, byte value)
        {
            stream.WriteBytes(new[] { value });
        }

        public static void WriteBytes(this IBasicOutputByteStream stream, byte[] buffer)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            stream.WriteBytes(buffer.AsReadOnly());
        }

        public static void WriteBytes(this IBasicOutputByteStream stream, IReadOnlyArray<byte> buffer)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            stream.WriteBytes(buffer, 0, buffer.Length);
        }

        public static void WriteBytes(this IBasicOutputByteStream stream, IReadOnlyArray<byte> buffer, int offset, int count)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentException("'offset' must not be negative.", nameof(offset));
            if (count < 0)
                throw new ArgumentException("'count' must not be negative.", nameof(count));
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException("'offset + count' is greater than 'buffer.Length'.");

            while (count > 0)
            {
                var length = stream.Write(buffer, offset, count);
                offset += length;
                count -= length;
            }
        }

        public static void WriteBytes(this IBasicOutputByteStream stream, IEnumerable<byte> sequence)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));

            foreach (var buffer in sequence.ToChunkOfReadOnlyArray(_WRITE_BYTE_SEQUENCE_DEFAULT_BUFFER_SIZE))
                stream.WriteBytes(buffer, 0, buffer.Length);
        }

        public static void WriteInt16LE(this IBasicOutputByteStream stream, Int16 value)
        {
            stream.WriteBytes(value.GetBytesLE());
        }

        public static void WriteInt16BE(this IBasicOutputByteStream stream, Int16 value)
        {
            stream.WriteBytes(value.GetBytesBE());
        }

        public static void WriteUInt16LE(this IBasicOutputByteStream stream, UInt16 value)
        {
            stream.WriteBytes(value.GetBytesLE());
        }

        public static void WriteUInt16BE(this IBasicOutputByteStream stream, UInt16 value)
        {
            stream.WriteBytes(value.GetBytesBE());
        }

        public static void WriteInt32LE(this IBasicOutputByteStream stream, Int32 value)
        {
            stream.WriteBytes(value.GetBytesLE());
        }

        public static void WriteInt32BE(this IBasicOutputByteStream stream, Int32 value)
        {
            stream.WriteBytes(value.GetBytesBE());
        }

        public static void WriteUInt32LE(this IBasicOutputByteStream stream, UInt32 value)
        {
            stream.WriteBytes(value.GetBytesLE());
        }

        public static void WriteUInt32BE(this IBasicOutputByteStream stream, UInt32 value)
        {
            stream.WriteBytes(value.GetBytesBE());
        }

        public static void WriteInt64LE(this IBasicOutputByteStream stream, Int64 value)
        {
            stream.WriteBytes(value.GetBytesLE());
        }

        public static void WriteInt64BE(this IBasicOutputByteStream stream, Int64 value)
        {
            stream.WriteBytes(value.GetBytesBE());
        }

        public static void WriteUInt64LE(this IBasicOutputByteStream stream, UInt64 value)
        {
            stream.WriteBytes(value.GetBytesLE());
        }

        public static void WriteUInt64BE(this IBasicOutputByteStream stream, UInt64 value)
        {
            stream.WriteBytes(value.GetBytesBE());
        }

        private static IReadOnlyArray<byte> ReadBytes(uint count, Func<byte[], int, int, int> reader)
        {
            if (count < 0)
                throw new ArgumentException("count");

            var buffer = new byte[count];
            var index = 0;
            while (index < buffer.Length)
            {
                var length = reader(buffer, index, buffer.Length - index);
                if (length <= 0)
                    break;
                index += length;
            }
            if (index < buffer.Length)
                Array.Resize(ref buffer, index);
            return buffer.AsReadOnly();
        }

        private static IEnumerable<byte> ReadBytes(ulong count, Func<int, IReadOnlyArray<byte>> reader)
        {
            if (count < 0)
                throw new ArgumentException("count");

            var byteArrayChain = new byte[0].AsEnumerable();
            while (count > 0)
            {
                var length = (int)Math.Min(count, int.MaxValue);
                var data = reader(length);
                byteArrayChain = byteArrayChain.Concat(data);
                count -= (uint)length;
            }
            return byteArrayChain;
        }

        private static int ReadBytes(byte[] buffer, int offset, int count, Func<byte[], int, int, int> reader)
        {
            if (count < 0)
                throw new ArgumentException("count");
            var index = offset;
            while (index < offset + count)
            {
                var length = reader(buffer, index, offset + count - index);
                if (length <= 0)
                    break;
                index += length;
            }
            return index - offset;
        }

        private static IReadOnlyArray<byte> ReadAllBytes(Func<byte[], int, int, int> reader)
        {
            const int BUFFER_SIZE = 80 * 2024;
            using (var outputStream = new MemoryStream())
            {
                var buffer = new byte[BUFFER_SIZE];
                while (true)
                {
                    var length = reader(buffer, 0, buffer.Length);
                    if (length <= 0)
                        break;
                    outputStream.Write(buffer, 0, length);
                }
                return outputStream.ToArray().AsReadOnly();
            }
        }

#if DEBUG
        public static void SelfTest()
        {
            var testData1 = Encoding.UTF8.GetBytes("");
            using (var inputStream = new MemoryStream(testData1))
            {
                if (!inputStream.GetByteSequence(true).ToArray().SequenceEqual(testData1))
                    throw new Exception();
            }
            var testData2 = Encoding.UTF8.GetBytes("Hello");
            using (var inputStream = new MemoryStream(testData2))
            {
                if (!inputStream.GetByteSequence(true).ToArray().SequenceEqual(testData2))
                    throw new Exception();
            }
            var testData3 = Encoding.UTF8.GetBytes("1995年夏、人々は融けかかったアスファルトに己が足跡を刻印しつつ歩いていた。酷く暑い.");
            using (var inputStream = new MemoryStream(testData3))
            {
                if (!inputStream.GetByteSequence(true).ToArray().SequenceEqual(testData3))
                    throw new Exception();
            }
            var testData4 = Encoding.UTF8.GetBytes("1995年夏、人々は溶けかかったアスファルトに己が足跡を刻印しつつ歩いていた。酷く暑い!");
            var testData5 = Encoding.UTF8.GetBytes("1995年夏、人々は溶けかかったアスファルトに己が足跡を刻印しつつ歩いていた。酷く暑い!!");
            var testData6 = Encoding.UTF8.GetBytes("2995年夏、人々は融けかかったアスファルトに己が足跡を刻印しつつ歩いていた。酷く暑い.");

            var testDataPair = new[]
            {
                new { data1 = testData1, data2 = testData1, expected = true},
                new { data1 = testData1, data2 = testData3, expected = false},
                new { data1 = testData1, data2 = testData4, expected = false},
                new { data1 = testData1, data2 = testData5, expected = false},
                new { data1 = testData1, data2 = testData6, expected = false},
                new { data1 = testData3, data2 = testData1, expected = false},
                new { data1 = testData3, data2 = testData3, expected = true},
                new { data1 = testData3, data2 = testData4, expected = false},
                new { data1 = testData3, data2 = testData5, expected = false},
                new { data1 = testData3, data2 = testData6, expected = false},
                new { data1 = testData4, data2 = testData1, expected = false},
                new { data1 = testData4, data2 = testData3, expected = false},
                new { data1 = testData4, data2 = testData4, expected = true},
                new { data1 = testData4, data2 = testData5, expected = false},
                new { data1 = testData4, data2 = testData6, expected = false},
                new { data1 = testData5, data2 = testData1, expected = false},
                new { data1 = testData5, data2 = testData3, expected = false},
                new { data1 = testData5, data2 = testData4, expected = false},
                new { data1 = testData5, data2 = testData5, expected = true},
                new { data1 = testData5, data2 = testData6, expected = false},
                new { data1 = testData6, data2 = testData1, expected = false},
                new { data1 = testData6, data2 = testData3, expected = false},
                new { data1 = testData6, data2 = testData4, expected = false},
                new { data1 = testData6, data2 = testData5, expected = false},
                new { data1 = testData6, data2 = testData6, expected = true},
            };
            foreach (var item in testDataPair)
            {
                using (var inputStream1 = new MemoryStream(item.data1))
                using (var inputStream2 = new MemoryStream(item.data2))
                {
                    if (inputStream1.StreamBytesEqual(inputStream2, true) != item.expected)
                        throw new Exception();
                }
            }
        }
#endif
    }
}
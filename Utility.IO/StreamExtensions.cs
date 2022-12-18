using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utility;

namespace Utility.IO
{
    public static class StreamExtensions
    {
        #region private class

        private class ReverseByteSequenceByByteStream
            : ReverseByteSequenceByByteStreamEnumerable<UInt64>
        {
            public ReverseByteSequenceByByteStream(IRandomInputByteStream<UInt64> inputStream, UInt64 offset, UInt64 count, IProgress<UInt64>? progress, bool leaveOpen)
                : base(inputStream, offset, count, progress, leaveOpen)
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

            protected override Int32 GetDistanceBetweenPositions(UInt64 position1, UInt64 distance2)
            {
#if DEBUG
                checked
#endif
                {
                    return (Int32)(position1 - distance2);
                }
            }

            protected override UInt64 SubtractBufferSizeFromPosition(UInt64 position, UInt32 distance)
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
            public BufferedInputStreamUInt64(IInputByteStream<UInt64> baseStream, bool leaveOpen)
                : base(baseStream, leaveOpen)
            {
            }

            public BufferedInputStreamUInt64(IInputByteStream<UInt64> baseStream, Int32 bufferSize, bool leaveOpen)
                : base(baseStream, bufferSize, leaveOpen)
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

        private class BufferedOutputStreamUInt64
            : BufferedOutputStream<UInt64>
        {
            public BufferedOutputStreamUInt64(IOutputByteStream<UInt64> baseStream, bool leaveOpen)
                : base(baseStream, leaveOpen)
            {
            }

            public BufferedOutputStreamUInt64(IOutputByteStream<UInt64> baseStream, Int32 bufferSize, bool leaveOpen)
                : base(baseStream, bufferSize, leaveOpen)
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

        private class BufferedRandomInputStreamUInt64
            : BufferedRandomInputStream<UInt64>
        {

            public BufferedRandomInputStreamUInt64(IRandomInputByteStream<UInt64> baseStream, bool leaveOpen)
                : base(baseStream, leaveOpen)
            {
            }

            public BufferedRandomInputStreamUInt64(IRandomInputByteStream<UInt64> baseStream, Int32 bufferSize, bool leaveOpen)
                : base(baseStream, bufferSize, leaveOpen)
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

            protected override UInt64 GetDistanceBetweenPositions(UInt64 x, UInt64 y)
            {
#if DEBUG
                checked
#endif
                {
                    return x - y;
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

            public BufferedRandomOutputStreamUint64(IRandomOutputByteStream<UInt64> baseStream, Int32 bufferSize, bool leaveOpen)
                : base(baseStream, bufferSize, leaveOpen)
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

            protected override UInt64 GetDistanceBetweenPositions(UInt64 x, UInt64 y)
            {
                return x - y;
            }
        }

        private class PartialInputStreamUint64
            : PartialInputStream<UInt64, UInt64>
        {
            public PartialInputStreamUint64(IInputByteStream<UInt64> baseStream, bool leaveOpen)
                : base(baseStream, leaveOpen)
            {
            }

            public PartialInputStreamUint64(IInputByteStream<UInt64> baseStream, UInt64 size, bool leaveOpen)
                : base(baseStream, size, leaveOpen)
            {
            }

            public PartialInputStreamUint64(IInputByteStream<UInt64> baseStream, UInt64? size, bool leaveOpen)
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

        private class PartialOutputStreamUint64
            : PartialOutputStream<UInt64, UInt64>
        {
            public PartialOutputStreamUint64(IOutputByteStream<UInt64> baseStream, bool leaveOpen)
                : base(baseStream, leaveOpen)
            {
            }

            public PartialOutputStreamUint64(IOutputByteStream<UInt64> baseStream, UInt64 size, bool leaveOpen)
                : base(baseStream, size, leaveOpen)
            {
            }

            public PartialOutputStreamUint64(IOutputByteStream<UInt64> baseStream, UInt64? size, bool leaveOpen)
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

        private class PartialRandomInputStreamUint64
            : PartialRandomInputStream<UInt64, UInt64>
        {
            private readonly IRandomInputByteStream<UInt64> _baseStream;

            public PartialRandomInputStreamUint64(IRandomInputByteStream<UInt64> baseStream, bool leaveOpen = false)
                : base(baseStream, leaveOpen)
            {
                _baseStream = baseStream;
            }

            public PartialRandomInputStreamUint64(IRandomInputByteStream<UInt64> baseStream, UInt64 size, bool leaveOpen = false)
                : base(baseStream, size, leaveOpen)
            {
                _baseStream = baseStream;
            }

            public PartialRandomInputStreamUint64(IRandomInputByteStream<UInt64> baseStream, UInt64 offset, UInt64 size, bool leaveOpen = false)
                : base(baseStream, offset, size, leaveOpen)
            {
                _baseStream = baseStream;
            }

            public PartialRandomInputStreamUint64(IRandomInputByteStream<UInt64> baseStream, UInt64? offset, UInt64? size, bool leaveOpen = false)
                : base(baseStream, offset, size, leaveOpen)
            {
                _baseStream = baseStream;
            }

            protected override UInt64 ZeroPositionValue => 0;

            protected override UInt64 EndBasePositionValue => _baseStream.Length;

            protected override (bool Success, UInt64 Position) AddBasePosition(UInt64 x, UInt64 y)
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

            protected override (bool Success, UInt64 Distance) GetDistanceBetweenBasePositions(UInt64 x, UInt64 y)
            {
                return x >= y ? (true, x - y) : (false, 0);
            }

            protected override (bool Success, UInt64 Distance) GetDistanceBetweenPositions(UInt64 x, UInt64 y)
            {
                return x >= y ? (true, x - y) : (false, 0);
            }
        }

        private class PartialRandomOutputStreamUint64
            : PartialRandomOutputStream<UInt64, UInt64>
        {
            public PartialRandomOutputStreamUint64(IRandomOutputByteStream<UInt64> baseStream, bool leaveOpen)
                : base(baseStream, leaveOpen)
            {
            }

            public PartialRandomOutputStreamUint64(IRandomOutputByteStream<UInt64> baseStream, UInt64 size, bool leaveOpen)
                : base(baseStream, size, leaveOpen)
            {
            }

            public PartialRandomOutputStreamUint64(IRandomOutputByteStream<UInt64> baseStream, UInt64 offset, UInt64 size, bool leaveOpen)
                : base(baseStream, offset, size, leaveOpen)
            {
            }

            public PartialRandomOutputStreamUint64(IRandomOutputByteStream<UInt64> baseStream, UInt64? offset, UInt64? size, bool leaveOpen)
                : base(baseStream, offset, size, leaveOpen)
            {
            }

            protected override UInt64 ZeroPositionValue => 0;

            protected override UInt64 ZeroBasePositionValue => 0;

            protected override (bool Success, UInt64 Position) AddBasePosition(UInt64 x, UInt64 y)
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

            protected override (bool Success, UInt64 Distance) GetDistanceBetweenBasePositions(UInt64 x, UInt64 y)
            {
                return x >= y ? (true, x - y) : (false, 0);
            }

            protected override (bool Success, UInt64 Distance) GetDistanceBetweenPositions(UInt64 x, UInt64 y)
            {
                return x >= y ? (true, x - y) : (false, 0);
            }
        }

        private class Crc32CalculationInputStream
            : IInputByteStream<UInt64>
        {
            private readonly IInputByteStream<UInt64> _baseStream;
            private readonly ICrcCalculationState<UInt32, UInt64> _session;
            private readonly ValueHolder<(UInt32 Crc, UInt64 Length)> _valueHolder;
            private readonly bool _leaveOpen;

            private bool _isDisposed;
            private UInt64 _position;

            public Crc32CalculationInputStream(IInputByteStream<UInt64> baseStream, ICrcCalculationState<UInt32, UInt64> session, ValueHolder<(UInt32 Crc, UInt64 Length)> valueHolder, bool leaveOpen)
            {
                _baseStream = baseStream;
                _session = session;
                _valueHolder = valueHolder;
                _leaveOpen = leaveOpen;
            }

            public UInt64 Position
            {
                get
                {
                    if (_isDisposed)
                        throw new ObjectDisposedException(GetType().FullName);

                    return _position;
                }
            }

            public Int32 Read(Span<byte> buffer)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                var length = _baseStream.Read(buffer);
                if (length > 0)
                {
                    _position += (UInt32)length;
                    _session.Put(buffer);
                }
                return length;
            }

            public async Task<Int32> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                var length = await _baseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (length > 0)
                {
                    _position += (UInt32)length;
                    _session.Put(buffer.Span);
                }
                return length;
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
                    {
                        _valueHolder.Value = _session.GetResult();
                        if (!_leaveOpen)
                            _baseStream.Dispose();
                    }
                    _isDisposed = true;
                }
            }

            protected virtual async ValueTask DisposeAsyncCore()
            {
                if (!_isDisposed)
                {
                    _valueHolder.Value = _session.GetResult();
                    if (!_leaveOpen)
                        await _baseStream.DisposeAsync().ConfigureAwait(false);
                    _isDisposed = true;
                }
            }
        }

        private class Crc32CalculationOutputStream
            : IOutputByteStream<UInt64>
        {
            private readonly IOutputByteStream<UInt64> _baseStream;
            private readonly ICrcCalculationState<UInt32, UInt64> _session;
            private readonly ValueHolder<(UInt32 Crc, UInt64 Length)> _valueHolder;
            private readonly bool _leaveOpen;

            private bool _isDisposed;
            private UInt64 _position;

            public Crc32CalculationOutputStream(IOutputByteStream<UInt64> baseStream, ICrcCalculationState<UInt32, UInt64> session, ValueHolder<(UInt32 Crc, UInt64 Length)> valueHolder, bool leaveOpen)
            {
                _baseStream = baseStream;
                _session = session;
                _valueHolder = valueHolder;
                _leaveOpen = leaveOpen;
            }

            public UInt64 Position
            {
                get
                {
                    if (_isDisposed)
                        throw new ObjectDisposedException(GetType().FullName);

                    return _position;
                }
            }

            public Int32 Write(ReadOnlySpan<byte> buffer)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                var length = _baseStream.Write(buffer);
                if (length > 0)
                {
                    _position += (UInt32)length;
                    _session.Put(buffer);
                }
                return length;
            }

            public async Task<Int32> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                var length = await _baseStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (length > 0)
                {
                    _position += (UInt32)length;
                    _session.Put(buffer.Span);
                }
                return length;
            }

            public void Flush()
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

            }

            public Task FlushAsync(CancellationToken cancellationToken = default)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return Task.CompletedTask;
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
                    {
                        _valueHolder.Value = _session.GetResult();
                        if (!_leaveOpen)
                            _baseStream.Dispose();
                    }
                    _isDisposed = true;
                }
            }

            protected virtual async ValueTask DisposeAsyncCore()
            {
                if (!_isDisposed)
                {
                    _valueHolder.Value = _session.GetResult();
                    if (!_leaveOpen)
                        await _baseStream.DisposeAsync().ConfigureAwait(false);
                    _isDisposed = true;
                }
            }
        }

        #endregion

        private const Int32 _COPY_TO_DEFAULT_BUFFER_SIZE = 81920;
        private const Int32 _WRITE_BYTE_SEQUENCE_DEFAULT_BUFFER_SIZE = 81920;

        #region AsInputByteStream

        public static IInputByteStream<UInt64> AsInputByteStream(this Stream baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream.CanSeek)
                    return new RandomInputByteStreamByStream(baseStream, leaveOpen);
                else
                    return new SequentialInputByteStreamByStream(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        #endregion

        #region AsOutputByteStream

        public static IOutputByteStream<UInt64> AsOutputByteStream<BASE_STREAM_T>(this BASE_STREAM_T baseStream, bool leaveOpen = false)
            where BASE_STREAM_T : Stream
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream.CanSeek)
                    return new RandomOutputByteStreamByStream<BASE_STREAM_T>(baseStream, _ => { }, leaveOpen);
                else
                    return new SequentialOutputByteStreamByStream<BASE_STREAM_T>(baseStream, _ => { }, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<UInt64> AsOutputByteStream<BASE_STREAM_T>(this BASE_STREAM_T baseStream, Action<BASE_STREAM_T> finishAction, bool leaveOpen = false)
            where BASE_STREAM_T : Stream
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream.CanSeek)
                    return new RandomOutputByteStreamByStream<BASE_STREAM_T>(baseStream, finishAction, leaveOpen);
                else
                    return new SequentialOutputByteStreamByStream<BASE_STREAM_T>(baseStream, finishAction, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        #endregion

        #region AsStream

        public static Stream AsStream(this IInputByteStream<UInt64> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new StreamByInputByteStream(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static Stream AsStream<BASE_STREAM_T>(this BASE_STREAM_T baseStream, bool leaveOpen = false)
            where BASE_STREAM_T : IOutputByteStream<UInt64>
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new StreamByOutputByteStream<BASE_STREAM_T>(baseStream, _ => { }, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static Stream AsStream<BASE_STREAM_T>(this BASE_STREAM_T baseStream, Action<BASE_STREAM_T> finishAction, bool leaveOpen = false)
            where BASE_STREAM_T : IOutputByteStream<UInt64>
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new StreamByOutputByteStream<BASE_STREAM_T>(baseStream, finishAction, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        #endregion

        #region AsByteStream

        public static IInputByteStream<UInt64> AsByteStream(this IEnumerable<byte> baseSequence)
        {
            if (baseSequence is null)
                throw new ArgumentNullException(nameof(baseSequence));

            return new SequentialInputByteStreamBySequence(baseSequence);
        }

        public static IInputByteStream<UInt64> AsByteStream(this IInputBitStream baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new SequentialInputByteStreamByBitStream(baseStream, BitPackingDirection.MsbToLsb, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputByteStream<UInt64> AsByteStream(this IInputBitStream baseStream, BitPackingDirection bitPackingDirection, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new SequentialInputByteStreamByBitStream(baseStream, bitPackingDirection, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<UInt64> AsByteStream(this IOutputBitStream baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new SequentialOutputByteStreamByBitStream(baseStream, BitPackingDirection.MsbToLsb, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<UInt64> AsByteStream(this IOutputBitStream baseStream, BitPackingDirection bitPackingDirection, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new SequentialOutputByteStreamByBitStream(baseStream, bitPackingDirection, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        #endregion

        #region AsBitStream

        public static IInputBitStream AsBitStream(this IInputByteStream<UInt64> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new SequentialInputBitStreamByByteStream(baseStream, BitPackingDirection.Default, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputBitStream AsBitStream(this IInputByteStream<UInt64> baseStream, BitPackingDirection bitPackingDirection, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new SequentialInputBitStreamByByteStream(baseStream, bitPackingDirection, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputBitStream AsBitStream(this IEnumerable<byte> baseSequence, BitPackingDirection bitPackingDirection = BitPackingDirection.Default)
        {
            if (baseSequence is null)
                throw new ArgumentNullException(nameof(baseSequence));

            return new SequentialInputBitStreamBySequence(baseSequence, bitPackingDirection);
        }

        public static IOutputBitStream AsBitStream(this IOutputByteStream<UInt64> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new SequentialOutputBitStreamByByteStream(baseStream, BitPackingDirection.Default, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputBitStream AsBitStream(this IOutputByteStream<UInt64> baseStream, BitPackingDirection bitPackingDirection, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return new SequentialOutputBitStreamByByteStream(baseStream, bitPackingDirection, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        #endregion

        #region WithPartial

        public static IInputByteStream<UInt64> WithPartial(this IInputByteStream<UInt64> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomInputByteStream<UInt64>;
                if (baseByteStream is not null)
                    return new PartialRandomInputStreamUint64(baseByteStream, leaveOpen);
                else
                    return new PartialInputStreamUint64(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }

        }

        public static IInputByteStream<UInt64> WithPartial(this IInputByteStream<UInt64> baseStream, UInt64 size, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomInputByteStream<UInt64>;
                if (baseByteStream is not null)
                    return new PartialRandomInputStreamUint64(baseByteStream, size, leaveOpen);
                else
                    return new PartialInputStreamUint64(baseStream, size, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputByteStream<UInt64> WithPartial(this IInputByteStream<UInt64> baseStream, UInt64 offset, UInt64 size, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                if (baseStream is not IRandomInputByteStream<UInt64> baseByteStream)
                    throw new NotSupportedException();
                else
                    return new PartialRandomInputStreamUint64(baseByteStream, offset, size, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputByteStream<UInt64> WithPartial(this IInputByteStream<UInt64> baseStream, UInt64? offset, UInt64? size, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomInputByteStream<UInt64>;
                if (baseByteStream is not null)
                    return new PartialRandomInputStreamUint64(baseByteStream, offset, size, leaveOpen);
                else if (offset is not null)
                    throw new NotSupportedException();
                else
                    return new PartialInputStreamUint64(baseStream, size, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<UInt64> WithPartial(this IOutputByteStream<UInt64> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomOutputByteStream<UInt64>;
                if (baseByteStream is not null)
                    return new PartialRandomOutputStreamUint64(baseByteStream, leaveOpen);
                else
                    return new PartialOutputStreamUint64(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<UInt64> WithPartial(this IOutputByteStream<UInt64> baseStream, UInt64 size, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomOutputByteStream<UInt64>;
                if (baseByteStream is not null)
                    return new PartialRandomOutputStreamUint64(baseByteStream, size, leaveOpen);
                else
                    return new PartialOutputStreamUint64(baseStream, size, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<UInt64> WithPartial(this IOutputByteStream<UInt64> baseStream, UInt64 offset, UInt64 size, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomOutputByteStream<UInt64> baseByteStream)
                    throw new NotSupportedException();

                return new PartialRandomOutputStreamUint64(baseByteStream, offset, size, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<UInt64> WithPartial(this IOutputByteStream<UInt64> baseStream, UInt64? offset, UInt64? size, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomOutputByteStream<UInt64>;
                if (baseByteStream is not null)
                    return new PartialRandomOutputStreamUint64(baseByteStream, offset, size, leaveOpen);
                else if (offset is not null)
                    throw new NotSupportedException();
                else
                    return new PartialOutputStreamUint64(baseStream, size, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        #endregion

        #region WithCache

        public static IInputByteStream<UInt64> WithCache(this IInputByteStream<UInt64> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomInputByteStream<UInt64>;
                if (baseByteStream is not null)
                    return new BufferedRandomInputStreamUInt64(baseByteStream, leaveOpen);
                else
                    return new BufferedInputStreamUInt64(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IInputByteStream<UInt64> WithCache(this IInputByteStream<UInt64> baseStream, Int32 cacheSize, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomInputByteStream<UInt64>;
                if (baseByteStream is not null)
                    return new BufferedRandomInputStreamUInt64(baseByteStream, cacheSize, leaveOpen);
                else
                    return new BufferedInputStreamUInt64(baseStream, cacheSize, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<UInt64> WithCache(this IOutputByteStream<UInt64> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomOutputByteStream<UInt64>;
                if (baseByteStream is not null)
                    return new BufferedRandomOutputStreamUint64(baseByteStream, leaveOpen);
                else
                    return new BufferedOutputStreamUInt64(baseStream, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<UInt64> WithCache(this IOutputByteStream<UInt64> baseStream, Int32 cacheSize, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                var baseByteStream = baseStream as IRandomOutputByteStream<UInt64>;
                if (baseByteStream is not null)
                    return new BufferedRandomOutputStreamUint64(baseByteStream, cacheSize, leaveOpen);
                else
                    return new BufferedOutputStreamUInt64(baseStream, cacheSize, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        #endregion

        #region WithCrc32Calculation

        public static IInputByteStream<UInt64> WithCrc32Calculation(this IInputByteStream<UInt64> baseStream, ValueHolder<(UInt32 Crc, UInt64 Length)> value, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (value is null)
                    throw new ArgumentNullException(nameof(value));

                return new Crc32CalculationInputStream(baseStream, Crc32.CreateCalculationState(), value, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IOutputByteStream<UInt64> WithCrc32Calculation(this IOutputByteStream<UInt64> baseStream, ValueHolder<(UInt32 Crc, UInt64 Length)> value, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (value is null)
                    throw new ArgumentNullException(nameof(value));

                return new Crc32CalculationOutputStream(baseStream, Crc32.CreateCalculationState(), value, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        #endregion

        #region GetByteSequence

        public static IEnumerable<byte> GetByteSequence(this Stream baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetByteSequence();
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this Stream baseStream, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetByteSequence(progress);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this Stream baseStream, UInt64 offset, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetByteSequence(offset);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this Stream baseStream, UInt64 offset, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetByteSequence(offset, progress);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this Stream baseStream, UInt64 offset, UInt64 count, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetByteSequence(offset, count);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this Stream baseStream, UInt64 offset, UInt64 count, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetByteSequence(offset, count, progress: progress);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this IInputByteStream<UInt64> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return InternalGetByteSequence(baseStream, null, null, null, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this IInputByteStream<UInt64> baseStream, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return InternalGetByteSequence(baseStream, null, null, progress, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteStream)
                    throw new NotSupportedException();
                if (offset > byteStream.Length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                return InternalGetByteSequence(byteStream, offset, byteStream.Length - offset, null, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteStream)
                    throw new NotSupportedException();
                if (offset > byteStream.Length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                return InternalGetByteSequence(byteStream, offset, byteStream.Length - offset, progress, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, UInt64 count, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();
                checked
                {
                    if (offset + count > byteSteram.Length)
                        throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(baseStream)}.");
                }

                return InternalGetByteSequence(byteSteram, offset, count, null, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, UInt64 count, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();
                checked
                {
                    if (offset + count > byteSteram.Length)
                        throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(baseStream)}.");
                }

                return InternalGetByteSequence(byteSteram, offset, count, progress, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        #endregion

        #region GetReverseByteSequence

        public static IEnumerable<byte> GetReverseByteSequence(this Stream baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetReverseByteSequence();
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this Stream baseStream, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetReverseByteSequence(progress: progress);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this Stream baseStream, UInt64 offset, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetReverseByteSequence(offset);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this Stream baseStream, UInt64 offset, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetReverseByteSequence(offset, progress);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this Stream baseStream, UInt64 offset, UInt64 count, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetReverseByteSequence(offset, count);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this Stream baseStream, UInt64 offset, UInt64 count, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetReverseByteSequence(offset, count, progress);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this IInputByteStream<UInt64> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();

                return new ReverseByteSequenceByByteStream(byteSteram, 0, byteSteram.Length, null, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this IInputByteStream<UInt64> baseStream, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();

                return new ReverseByteSequenceByByteStream(byteSteram, 0, byteSteram.Length, progress, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();
                if (offset < 0)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                if (offset > byteSteram.Length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                return new ReverseByteSequenceByByteStream(byteSteram, offset, byteSteram.Length - offset, null, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();
                if (offset < 0)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                if (offset > byteSteram.Length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                return new ReverseByteSequenceByByteStream(byteSteram, offset, byteSteram.Length - offset, progress, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, UInt64 count, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();
                if (offset < 0)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                if (count < 0)
                    throw new ArgumentOutOfRangeException(nameof(count));
                checked
                {
                    if (offset + count > byteSteram.Length)
                        throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(baseStream)}.");
                }

                return new ReverseByteSequenceByByteStream(byteSteram, offset, count, null, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IEnumerable<byte> GetReverseByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, UInt64 count, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();
                if (offset < 0)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                if (count < 0)
                    throw new ArgumentOutOfRangeException(nameof(count));
                checked
                {
                    if (offset + count > byteSteram.Length)
                        throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(baseStream)}.");
                }

                return new ReverseByteSequenceByByteStream(byteSteram, offset, count, progress, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        #endregion

        #region StreamBytesEqual

        public static bool StreamBytesEqual(this Stream stream1, Stream stream2, bool leaveOpen = false)
        {
            if (stream1 is null)
                throw new ArgumentNullException(nameof(stream1));
            if (stream2 is null)
                throw new ArgumentNullException(nameof(stream2));

            try
            {
                using var byteStream1 = stream1.AsInputByteStream(true);
                using var byteStream2 = stream2.AsInputByteStream(true);
                return byteStream1.StreamBytesEqual(byteStream2, null, leaveOpen);
            }
            finally
            {
                if (!leaveOpen)
                {
                    stream1?.Dispose();
                    stream2?.Dispose();
                }
            }
        }

        public static bool StreamBytesEqual(this Stream stream1, Stream stream2, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            if (stream1 is null)
                throw new ArgumentNullException(nameof(stream1));
            if (stream2 is null)
                throw new ArgumentNullException(nameof(stream2));

            try
            {
                using var byteStream1 = stream1.AsInputByteStream(true);
                using var byteStream2 = stream2.AsInputByteStream(true);
                return byteStream1.StreamBytesEqual(byteStream2, progress, leaveOpen);
            }
            finally
            {
                if (!leaveOpen)
                {
                    stream1?.Dispose();
                    stream2?.Dispose();
                }
            }
        }

        public static bool StreamBytesEqual(this IBasicInputByteStream stream1, IBasicInputByteStream stream2, bool leaveOpen = false)
        {
            try
            {
                if (stream1 is null)
                    throw new ArgumentNullException(nameof(stream1));
                if (stream2 is null)
                    throw new ArgumentNullException(nameof(stream2));

                return InternalStreamBytesEqual(stream1, stream2, null);
            }
            finally
            {
                if (!leaveOpen)
                {
                    stream1?.Dispose();
                    stream2?.Dispose();
                }
            }
        }

        public static bool StreamBytesEqual(this IBasicInputByteStream stream1, IBasicInputByteStream stream2, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (stream1 is null)
                    throw new ArgumentNullException(nameof(stream1));
                if (stream2 is null)
                    throw new ArgumentNullException(nameof(stream2));

                return InternalStreamBytesEqual(stream1, stream2, progress);
            }
            finally
            {
                if (!leaveOpen)
                {
                    stream1?.Dispose();
                    stream2?.Dispose();
                }
            }
        }

        #endregion

        #region CopyTo

        public static void CopyTo(this Stream source, Stream destination, IProgress<UInt64>? progress = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            using var sourceByteStream = source.AsInputByteStream(true);
            using var destinationByteStream = destination.AsOutputByteStream(true);
            sourceByteStream.InternalCopyTo(destinationByteStream, _COPY_TO_DEFAULT_BUFFER_SIZE, progress);
        }

        public static void CopyTo(this IBasicInputByteStream source, IBasicOutputByteStream destination, IProgress<UInt64>? progress = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            source.InternalCopyTo(destination, _COPY_TO_DEFAULT_BUFFER_SIZE, progress);
        }

        public static void CopyTo(this Stream source, Stream destination, Int32 bufferSize, IProgress<UInt64>? progress = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            using var sourceByteStream = source.AsInputByteStream(true);
            using var destinationByteStream = destination.AsOutputByteStream(true);
            sourceByteStream.InternalCopyTo(destinationByteStream, bufferSize, progress);
        }

        public static void CopyTo(this IBasicInputByteStream source, IBasicOutputByteStream destination, Int32 bufferSize, IProgress<UInt64>? progress = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            source.InternalCopyTo(destination, bufferSize, progress);
        }

        #endregion

        #region Read

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Read(this Stream stream, byte[] buffer)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            return stream.Read(buffer, 0, buffer.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Read(this Stream stream, byte[] buffer, Int32 offset)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return
                stream.Read(
                    buffer,
                    offset,
                    buffer.Length - offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 Read(this Stream stream, byte[] buffer, UInt32 offset)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset > (UInt32)buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
#if DEBUG
            if (offset > Int32.MaxValue)
                throw new Exception();
#endif
            return
                (UInt32)stream.Read(
                    buffer,
                    (Int32)offset,
                    buffer.Length - (Int32)offset)
                .Maximum(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Read(this Stream stream, byte[] buffer, Range range)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            var (isOk, offset, count) = buffer.GetOffsetAndLength(range);
            if (!isOk)
                throw new ArgumentOutOfRangeException(nameof(range));

            return stream.Read(buffer, offset, count);
        }

#if false
        public static Int32 Read(this Stream stream, byte[] buffer, Int32 offset, Int32 count)
        {
            throw new NotImplementedException(); // defined in System.IO.Stream.Read(byte[], Int32, Int32)
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 Read(this Stream stream, byte[] buffer, UInt32 offset, UInt32 count)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            checked
            {
                if (offset + count > (UInt32)buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }

            return
                (UInt32)stream.Read(
                    buffer,
                    (Int32)offset,
                    (Int32)count)
                .Maximum(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Read(this Stream stream, Memory<byte> buffer)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            return stream.Read(buffer.Span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Read(this IBasicInputByteStream stream, byte[] buffer)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            return stream.Read(buffer.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Read(this IBasicInputByteStream stream, byte[] buffer, Int32 offset)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return stream.Read(buffer.AsSpan(offset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 Read(this IBasicInputByteStream stream, byte[] buffer, UInt32 offset)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return
                (UInt32)stream.Read(buffer.AsSpan(offset))
                .Maximum(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Read(this IBasicInputByteStream stream, byte[] buffer, Range range)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            var (isOk, offset, count) = buffer.GetOffsetAndLength(range);
            if (!isOk)
                throw new ArgumentOutOfRangeException(nameof(range));

            return stream.Read(buffer.AsSpan(offset, count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Read(this IBasicInputByteStream stream, byte[] buffer, Int32 offset, Int32 count)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            checked
            {
                if (offset + count > buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }

            return stream.Read(buffer.AsSpan(offset, count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 Read(this IBasicInputByteStream stream, byte[] buffer, UInt32 offset, UInt32 count)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            checked
            {
                if (offset + count > buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }

            return checked((UInt32)stream.Read(buffer.AsSpan(offset, count)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Read(this IBasicInputByteStream stream, Memory<byte> buffer)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            return stream.Read(buffer.Span);
        }

#if false
        public static Int32 Read(this IBasicInputByteStream stream, Span<byte> buffer)
        {
            throw new NotImplementedException(); // defined in IBasicInputByteStream.Read(Span<byte>)
        }
#endif

        #endregion

        #region ReadByteOrNull

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte? ReadByteOrNull(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<byte> buffer = stackalloc byte[1];
            var length = sourceStream.Read(buffer);
            if (length <= 0)
                return null;
            return buffer[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte? ReadByteOrNull(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<byte> buffer = stackalloc byte[1];
            var length = sourceStream.Read(buffer);
            if (length <= 0)
                return null;
            return buffer[0];
        }

        #endregion

        #region ReadByte

#if false
        public static byte ReadByte(this Stream sourceStream)
        {
            throw new NotImplementedException(); // defined in System.IO.Stream.ReadByte()
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByte(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            Span<byte> buffer = stackalloc byte[1];
            var length = sourceStream.Read(buffer);
            if (length <= 0)
                throw new UnexpectedEndOfStreamException();
            return buffer[0];
        }

        #endregion

        #region ReadBytes

        public static ReadOnlyMemory<byte> ReadBytes(this Stream sourceStream, Int32 count)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var buffer = new byte[count];
            var length =
                InternalReadBytes(
                    0,
                    buffer.Length,
                    (_offset, _count) => sourceStream.Read(buffer, _offset, _count));
            if (length < buffer.Length)
                Array.Resize(ref buffer, length);
            return buffer;
        }

        public static ReadOnlyMemory<byte> ReadBytes(this Stream sourceStream, UInt32 count)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new byte[count];
            var length =
                InternalReadBytes(
                    0,
                    buffer.Length,
                    (_offset, _count) => sourceStream.Read(buffer, _offset, _count));
            if (length < buffer.Length)
                Array.Resize(ref buffer, length);
            return buffer;
        }

        public static Int32 ReadBytes(this Stream sourceStream, byte[] buffer)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            return
                InternalReadBytes(
                    0,
                    buffer.Length,
                    (_offset, _count) => sourceStream.Read(buffer, _offset, _count));
        }

        public static Int32 ReadBytes(this Stream sourceStream, byte[] buffer, Int32 offset)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return
                InternalReadBytes(
                    offset,
                    buffer.Length - offset,
                    (_offset, _count) => sourceStream.Read(buffer, _offset, _count));
        }

        public static UInt32 ReadBytes(this Stream sourceStream, byte[] buffer, UInt32 offset)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset > (UInt32)buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
#if DEBUG
            if (offset > Int32.MaxValue)
                throw new Exception();
#endif
            return
                (UInt32)InternalReadBytes(
                    (Int32)offset,
                    buffer.Length - (Int32)offset,
                    (_offset, _count) => sourceStream.Read(buffer, _offset, _count))
                .Maximum(0);
        }

        public static Int32 ReadBytes(this Stream sourceStream, byte[] buffer, Range range)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            var (isOk, offset, count) = buffer.GetOffsetAndLength(range);
            if (!isOk)
                throw new ArgumentOutOfRangeException(nameof(range));

            return
                InternalReadBytes(
                    offset,
                    count,
                    (_offset, _count) => sourceStream.Read(buffer, _offset, _count));
        }

        public static Int32 ReadBytes(this Stream sourceStream, byte[] buffer, Int32 offset, Int32 count)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            checked
            {
                if (offset + count > buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }

            return
                InternalReadBytes(
                    offset,
                    count,
                    (_offset, _count) => sourceStream.Read(buffer, _offset, _count));
        }

        public static UInt32 ReadBytes(this Stream sourceStream, byte[] buffer, UInt32 offset, UInt32 count)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            checked
            {
                if (offset + count > buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }
#if DEBUG
            if (offset > Int32.MaxValue)
                throw new Exception();
#endif
            return
                (UInt32)InternalReadBytes(
                    (Int32)offset,
                    (Int32)count,
                    (_offset, _count) => sourceStream.Read(buffer, _offset, _count))
                .Maximum(0);
        }

        public static Int32 ReadBytes(this Stream sourceStream, Memory<byte> buffer)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            return
                InternalReadBytes(
                    buffer,
                    _buffer => sourceStream.Read(_buffer.Span));
        }

        public static Int32 ReadBytes(this Stream sourceStream, Span<byte> buffer)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var processedCount = 0;
            while (!buffer.IsEmpty)
            {
                var length = sourceStream.Read(buffer);
                if (length <= 0)
                    break;
                buffer = buffer[length..];
                processedCount += length;
            }
            return processedCount; ;
        }

        public static ReadOnlyMemory<byte> ReadBytes(this IBasicInputByteStream sourceStream, Int32 count)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var buffer = new byte[count];
            var length =
                InternalReadBytes(
                    0,
                    buffer.Length,
                    (_offset, _count) => sourceStream.Read(buffer, _offset, _count));
            if (length < buffer.Length)
                Array.Resize(ref buffer, length);
            return buffer;
        }

        public static ReadOnlyMemory<byte> ReadBytes(this IBasicInputByteStream sourceStream, UInt32 count)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new byte[count];
            var length =
                InternalReadBytes(
                    0,
                    buffer.Length,
                    (_offset, _count) => sourceStream.Read(buffer.AsSpan(_offset, _count)));
            if (length < buffer.Length)
                Array.Resize(ref buffer, length);
            return buffer;
        }

        public static Int32 ReadBytes(this IBasicInputByteStream sourceStream, byte[] buffer)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            return
                InternalReadBytes(
                    0,
                    buffer.Length,
                    (_offset, _count) => sourceStream.Read(buffer.AsSpan(_offset, _count)));
        }

        public static Int32 ReadBytes(this IBasicInputByteStream sourceStream, byte[] buffer, Int32 offset)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return
                InternalReadBytes(
                    offset,
                    buffer.Length - offset,
                    (_offset, _count) => sourceStream.Read(buffer.AsSpan(_offset, _count)));
        }

        public static UInt32 ReadBytes(this IBasicInputByteStream sourceStream, byte[] buffer, UInt32 offset)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset > (UInt32)buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
#if DEBUG
            if (offset > Int32.MaxValue)
                throw new Exception();
#endif
            return
                (UInt32)InternalReadBytes(
                    (Int32)offset,
                    buffer.Length - (Int32)offset,
                    (_offset, _count) => sourceStream.Read(buffer.AsSpan(_offset, _count)))
                .Maximum(0);
        }

        public static Int32 ReadBytes(this IBasicInputByteStream sourceStream, byte[] buffer, Range range)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            var (isOk, offset, count) = buffer.GetOffsetAndLength(range);
            if (!isOk)
                throw new ArgumentOutOfRangeException(nameof(range));

            return
                InternalReadBytes(
                    offset,
                    count,
                    (_offset, _count) => sourceStream.Read(buffer.AsSpan(_offset, _count)));
        }

        public static Int32 ReadBytes(this IBasicInputByteStream sourceStream, byte[] buffer, Int32 offset, Int32 count)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            checked
            {
                if (offset + count > buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }

            return
                InternalReadBytes(
                    offset,
                    count,
                    (_offset, _count) => sourceStream.Read(buffer.AsSpan(_offset, _count)));
        }

        public static UInt32 ReadBytes(this IBasicInputByteStream sourceStream, byte[] buffer, UInt32 offset, UInt32 count)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            checked
            {
                if (offset + count > buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }
#if DEBUG
            if (offset > Int32.MaxValue)
                throw new Exception();
#endif
            return
                (UInt32)InternalReadBytes(
                    (Int32)offset,
                    (Int32)count,
                    (_offset, _count) => sourceStream.Read(buffer.AsSpan(_offset, _count)))
                .Maximum(0);
        }

        public static Int32 ReadBytes(this IBasicInputByteStream sourceStream, Memory<byte> buffer)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            return
                InternalReadBytes(
                    buffer,
                    _buffer => sourceStream.Read(_buffer.Span));
        }

        public static Int32 ReadBytes(this IBasicInputByteStream sourceStream, Span<byte> buffer)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var processedCount = 0;
            while (!buffer.IsEmpty)
            {
                var length = sourceStream.Read(buffer);
                if (length <= 0)
                    break;
                buffer = buffer[length..];
                processedCount += length;
            }
            return processedCount; ;
        }

        #endregion

        #region ReadByteSequence

        public static IEnumerable<byte> ReadByteSequence(this Stream sourceStream, Int64 count)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            return
                InternalReadByteSequence(
                    (UInt64)count,
                    _count =>
                    {
                        var buffer = new byte[_count];
                        var length = sourceStream.Read(buffer, 0, buffer.Length);
                        if (length <= 0)
                            return null;
                        if (length < buffer.Length)
                            Array.Resize(ref buffer, length);
                        return buffer;
                    });
        }

        public static IEnumerable<byte> ReadByteSequence(this Stream sourceStream, UInt64 count)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            return
                InternalReadByteSequence(
                    count,
                    _count =>
                    {
                        var buffer = new byte[_count];
                        var length = sourceStream.Read(buffer, 0, buffer.Length);
                        if (length <= 0)
                            return null;
                        if (length < buffer.Length)
                            Array.Resize(ref buffer, length);
                        return buffer;
                    });
        }

        public static IEnumerable<byte> ReadByteSequence(this IBasicInputByteStream sourceStream, Int64 count)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            return
                InternalReadByteSequence(
                    (UInt64)count,
                    _count =>
                    {
                        var buffer = new byte[_count];
                        var length = sourceStream.Read(buffer);
                        if (length <= 0)
                            return null;
                        if (length < buffer.Length)
                            Array.Resize(ref buffer, length);
                        return buffer;
                    });
        }

        public static IEnumerable<byte> ReadByteSequence(this IBasicInputByteStream sourceStream, UInt64 count)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            return
                InternalReadByteSequence(
                    count,
                    _count =>
                    {
                        var buffer = new byte[_count];
                        var length = sourceStream.Read(buffer);
                        if (length <= 0)
                            return null;
                        if (length < buffer.Length)
                            Array.Resize(ref buffer, length);
                        return buffer;
                    });
        }

        #endregion

        #region ReadAllBytes

        public static ReadOnlyMemory<byte> ReadAllBytes(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            return InternalReadAllBytes((_buffer, _offset, _count) => sourceStream.Read(_buffer, _offset, _count));
        }

        public static ReadOnlyMemory<byte> ReadAllBytes(this IBasicInputByteStream sourceByteStream)
        {
            if (sourceByteStream is null)
                throw new ArgumentNullException(nameof(sourceByteStream));

            return InternalReadAllBytes((_buffer, _offset, _count) => sourceByteStream.Read(_buffer.AsSpan(_offset, _count)));
        }

        #endregion

        #region ReadInt16LE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16 ReadInt16LE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int16)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt16LE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16 ReadInt16LE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int16)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt16LE();
        }

        #endregion

        #region ReadUInt16LE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16 ReadUInt16LE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt16)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt16LE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16 ReadUInt16LE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt16)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt16LE();
        }

        #endregion

        #region ReadInt32LE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 ReadInt32LE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int32)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt32LE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 ReadInt32LE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int32)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt32LE();
        }

        #endregion

        #region ReadUInt32LE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 ReadUInt32LE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt32)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt32LE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 ReadUInt32LE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt32)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt32LE();
        }

        #endregion

        #region ReadInt64LE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64 ReadInt64LE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int64)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt64LE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64 ReadInt64LE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int64)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt64LE();
        }

        #endregion

        #region ReadUInt64LE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64 ReadUInt64LE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt64)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt64LE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64 ReadUInt64LE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt64)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt64LE();
        }

        #endregion

        #region ReadSingleLE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single ReadSingleLE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Single)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToSingleLE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single ReadSingleLE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Single)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToSingleLE();
        }

        #endregion

        #region ReadDoubleLE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double ReadDoubleLE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Double)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDoubleLE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double ReadDoubleLE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Double)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDoubleLE();
        }

        #endregion

        #region ReadDecimalLE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal ReadDecimalLE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Decimal)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDecimalLE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal ReadDecimalLE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Decimal)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDecimalLE();
        }

        #endregion

        #region ReadInt16BE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16 ReadInt16BE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int16)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt16BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16 ReadInt16BE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int16)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt16BE();
        }

        #endregion

        #region ReadUInt16BE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16 ReadUInt16BE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt16)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt16BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16 ReadUInt16BE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt16)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt16BE();
        }

        #endregion

        #region ReadInt32BE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 ReadInt32BE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int32)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt32BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 ReadInt32BE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int32)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt32BE();
        }

        #endregion

        #region ReadUInt32BE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 ReadUInt32BE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt32)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt32BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 ReadUInt32BE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt32)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt32BE();
        }

        #endregion

        #region ReadInt64BE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64 ReadInt64BE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int64)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt64BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64 ReadInt64BE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int64)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt64BE();
        }

        #endregion

        #region ReadUInt64BE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64 ReadUInt64BE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt64)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt64BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64 ReadUInt64BE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt64)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt64BE();
        }

        #endregion

        #region ReadSingleBE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single ReadSingleBE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Single)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToSingleBE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single ReadSingleBE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Single)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToSingleBE();
        }

        #endregion

        #region ReadDoubleBE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double ReadDoubleBE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Double)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDoubleBE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double ReadDoubleBE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Double)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDoubleBE();
        }

        #endregion

        #region ReadDecimalBE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal ReadDecimalBE(this Stream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Decimal)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDecimalBE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal ReadDecimalBE(this IBasicInputByteStream sourceStream)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Decimal)];
            if (sourceStream.ReadBytes(buffer) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDecimalBE();
        }

        #endregion

        #region Write

#if false
        public static Int32 Write(this Stream stream, byte[] buffer)
        {
            throw new NotImplementedException(); // equivalent to System.IO.Stream.Write(ReadOnlyMemory<Byte>)
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Write(this Stream stream, byte[] buffer, Int32 offset)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            var count = buffer.Length - offset;
            stream.Write(buffer, offset, count);
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 Write(this Stream stream, byte[] buffer, UInt32 offset)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
#if DEBUG
            if (offset > Int32.MaxValue)
                throw new Exception();
#endif

            var count = buffer.Length - (Int32)offset;
            stream.Write(buffer, (Int32)offset, count);
            return (UInt32)count;
        }

#if false

        public static Int32 Write(this Stream stream, byte[] buffer, Int32 offset, Int32 count)
        {
            throw new NotImplementedException(); // defined in System.IO.Stream.Write(byte[], Int32, Int32)
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 Write(this Stream stream, byte[] buffer, UInt32 offset, UInt32 count)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            checked
            {
                if (offset + count > (UInt32)buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }
#if DEBUG
            if (offset > Int32.MaxValue)
                throw new Exception();
            if (count > Int32.MaxValue)
                throw new Exception();
#endif

            stream.Write(buffer, (Int32)offset, (Int32)count);
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Write(this Stream stream, ReadOnlyMemory<byte> buffer)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            stream.Write(buffer.Span);
            return buffer.Length;
        }

#if false

        public static Int32 Write(this Stream stream, ReadOnlySpan<byte> buffer)
        {
            throw new NotImplementedException(); // defined in System.IO.Stream.Write(ReadOnlySpan<byte>)
        }
#endif

#if false
        public static Int32 Write(this IBasicOutputByteStream stream, byte[] buffer)
        {
            throw new NotImplementedException(); // equivalent to IBasicOutputByteStream.Write(ReadOnlySpan<Byte>)
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Write(this IBasicOutputByteStream stream, byte[] buffer, Int32 offset)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return stream.Write(buffer.AsSpan(offset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 Write(this IBasicOutputByteStream stream, byte[] buffer, UInt32 offset)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
#if DEBUG
            if (offset > Int32.MaxValue)
                throw new Exception();
#endif

            return (UInt32)stream.Write(buffer.AsSpan((Int32)offset)).Maximum(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Write(this IBasicOutputByteStream stream, byte[] buffer, Int32 offset, Int32 count)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            checked
            {
                if (offset + count > buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }

            return stream.Write(buffer.AsSpan(offset, count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 Write(this IBasicOutputByteStream stream, byte[] buffer, UInt32 offset, UInt32 count)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            checked
            {
                if (offset + count > (UInt32)buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }
#if DEBUG
            if (offset > Int32.MaxValue)
                throw new Exception();
            if (count > Int32.MaxValue)
                throw new Exception();
#endif

            return (UInt32)stream.Write(buffer.AsSpan(offset, count)).Maximum(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Write(this IBasicOutputByteStream stream, ReadOnlyMemory<byte> buffer)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            return stream.Write(buffer.Span);
        }

#if false

        public static Int32 Write(this IBasicOutputByteStream stream, ReadOnlySpan<byte> buffer)
        {
            throw new NotImplementedException(); // defined in IBasicOutputByteStream.Write(ReadOnlySpan<byte>)
        }
#endif

        #endregion

        #region WriteByte

#if false
        public static void WriteByte(this Stream stream, byte value)
        {
            throw new NotImplementedException(); // defined in System.IO.Stream.WriteByte(Byte)
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteByte(this IBasicOutputByteStream stream, byte value)
        {
            Span<byte> buffer = stackalloc byte[1];
            buffer[0] = value;
            var length = stream.Write(buffer);
            if (length <= 0)
                throw new InternalLogicalErrorException();
        }

        #endregion

        #region WriteBytes

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this Stream destinationStream, byte[] buffer)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            destinationStream.Write(buffer, 0, buffer.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this Stream destinationStream, byte[] buffer, Int32 offset)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            destinationStream.Write(buffer, offset, buffer.Length - offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this Stream destinationStream, byte[] buffer, UInt32 offset)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
#if DEBUG
            if (offset > Int32.MaxValue)
                throw new Exception();
#endif

            destinationStream.Write(buffer, (Int32)offset, buffer.Length - (Int32)offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this Stream destinationStream, byte[] buffer, Range range)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            var (isOk, offset, count) = buffer.GetOffsetAndLength(range);
            if (!isOk)
                throw new ArgumentOutOfRangeException(nameof(range));

            destinationStream.Write(buffer, offset, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this Stream destinationStream, byte[] buffer, Int32 offset, Int32 count)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            checked
            {
                if (offset + count > buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }

            destinationStream.Write(buffer, offset, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this Stream destinationStream, byte[] buffer, UInt32 offset, UInt32 count)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            checked
            {
                if (offset + count > (UInt32)buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }
#if DEBUG
            if (offset > Int32.MaxValue)
                throw new Exception();
            if (count > Int32.MaxValue)
                throw new Exception();
#endif

            destinationStream.Write(buffer, (Int32)offset, (Int32)count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this Stream destinationStream, ReadOnlyMemory<byte> buffer)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            destinationStream.Write(buffer.Span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this Stream destinationStream, ReadOnlySpan<byte> buffer)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            destinationStream.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this IBasicOutputByteStream destinationStream, byte[] buffer)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            InternalWriteBytes(0, buffer.Length, (_offset, _count) => destinationStream.Write(buffer.AsSpan(_offset, _count)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this IBasicOutputByteStream destinationStream, byte[] buffer, Int32 offset)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            InternalWriteBytes(0, buffer.Length, (_offset, _count) => destinationStream.Write(buffer.AsSpan(_offset, _count)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this IBasicOutputByteStream destinationStream, byte[] buffer, UInt32 offset)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset > (UInt32)buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
#if DEBUG
            if (offset > Int32.MaxValue)
                throw new Exception();
#endif

            InternalWriteBytes((Int32)offset, buffer.Length - (Int32)offset, (_offset, _count) => destinationStream.Write(buffer.AsSpan(_offset, _count)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this IBasicOutputByteStream destinationStream, byte[] buffer, Range range)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            var (isOk, offset, count) = buffer.GetOffsetAndLength(range);
            if (!isOk)
                throw new ArgumentOutOfRangeException(nameof(range));

            InternalWriteBytes(offset, count, (_offset, _count) => destinationStream.Write(buffer.AsSpan(_offset, _count)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this IBasicOutputByteStream destinationStream, byte[] buffer, Int32 offset, Int32 count)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            checked
            {
                if (offset + count > buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }

            InternalWriteBytes(offset, count, (_offset, _count) => destinationStream.Write(buffer.AsSpan(_offset, _count)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this IBasicOutputByteStream destinationStream, byte[] buffer, UInt32 offset, UInt32 count)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            checked
            {
                if (offset + count > (UInt32)buffer.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(buffer)}.");
            }
#if DEBUG
            if (offset > Int32.MaxValue)
                throw new Exception();
            if (count > Int32.MaxValue)
                throw new Exception();
#endif

            InternalWriteBytes((Int32)offset, (Int32)count, (_offset, _count) => destinationStream.Write(buffer.AsSpan(_offset, _count)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this IBasicOutputByteStream destinationStream, ReadOnlyMemory<byte> buffer)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var spanOfBuffer = buffer.Span;
            while (!spanOfBuffer.IsEmpty)
            {
                var length = destinationStream.Write(spanOfBuffer);
                if (length <= 0)
                    throw new IOException("Can not write any more");
                spanOfBuffer = spanOfBuffer[length..];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this IBasicOutputByteStream destinationStream, ReadOnlySpan<byte> buffer)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            while (!buffer.IsEmpty)
            {
                var length = destinationStream.Write(buffer);
                if (length <= 0)
                    throw new IOException("Can not write any more");
                buffer = buffer[length..];
            }
        }

        #endregion

        #region WriteByteSequence

        public static void WriteByteSequence(this Stream destinationStream, IEnumerable<byte> sequence)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (sequence is null)
                throw new ArgumentNullException(nameof(sequence));

            using var enumerator = sequence.GetEnumerator();
            var buffer = new Byte[_WRITE_BYTE_SEQUENCE_DEFAULT_BUFFER_SIZE];
            var isEndOfSequence = false;
            while (!isEndOfSequence)
            {
                var index = 0;
                while (index < buffer.Length)
                {
                    if (!enumerator.MoveNext())
                    {
                        isEndOfSequence = true;
                        break;
                    }
                    buffer[index++] = enumerator.Current;
                }
                if (index > 0)
                    destinationStream.Write(buffer, 0, index);
            }
        }

        public static void WriteByteSequence(this IBasicOutputByteStream destinationStream, IEnumerable<byte> sequence)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (sequence is null)
                throw new ArgumentNullException(nameof(sequence));

            using var enumerator = sequence.GetEnumerator();
            var buffer = new Byte[_WRITE_BYTE_SEQUENCE_DEFAULT_BUFFER_SIZE];
            var isEndOfSequence = false;
            while (!isEndOfSequence)
            {
                var index = 0;
                while (index < buffer.Length)
                {
                    if (!enumerator.MoveNext())
                    {
                        isEndOfSequence = true;
                        break;
                    }
                    buffer[index++] = enumerator.Current;
                }
                if (index > 0)
                    destinationStream.Write(buffer.AsSpan(0, index));
            }
        }

        #endregion

        #region WriteInt16LE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16LE(this Stream destinationStream, Int16 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int16)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16LE(this IBasicOutputByteStream destinationStream, Int16 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int16)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteUInt16LE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16LE(this Stream destinationStream, UInt16 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt16)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16LE(this IBasicOutputByteStream destinationStream, UInt16 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt16)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteInt32LE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32LE(this Stream destinationStream, Int32 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int32)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32LE(this IBasicOutputByteStream destinationStream, Int32 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int32)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteUInt32LE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32LE(this Stream destinationStream, UInt32 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt32)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32LE(this IBasicOutputByteStream destinationStream, UInt32 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt32)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteInt64LE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64LE(this Stream destinationStream, Int64 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int64)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64LE(this IBasicOutputByteStream destinationStream, Int64 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int64)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteUInt64LE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64LE(this Stream destinationStream, UInt64 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt64)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64LE(this IBasicOutputByteStream destinationStream, UInt64 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt64)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteSingleLE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingleLE(this Stream destinationStream, Single value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Single)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingleLE(this IBasicOutputByteStream destinationStream, Single value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Single)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteDoubleLE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDoubleLE(this Stream destinationStream, Double value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Double)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDoubleLE(this IBasicOutputByteStream destinationStream, Double value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Double)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteDecimalLE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDecimalLE(this Stream destinationStream, Decimal value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Decimal)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDecimalLE(this IBasicOutputByteStream destinationStream, Decimal value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Decimal)];
            buffer.CopyValueLE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteInt16BE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16BE(this Stream destinationStream, Int16 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int16)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16BE(this IBasicOutputByteStream destinationStream, Int16 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int16)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteUInt16BE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16BE(this Stream destinationStream, UInt16 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt16)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16BE(this IBasicOutputByteStream destinationStream, UInt16 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt16)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteInt32BE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32BE(this Stream destinationStream, Int32 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int32)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32BE(this IBasicOutputByteStream destinationStream, Int32 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int32)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteUInt32BE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32BE(this Stream destinationStream, UInt32 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt32)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32BE(this IBasicOutputByteStream destinationStream, UInt32 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt32)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteInt64BE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64BE(this Stream destinationStream, Int64 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int64)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64BE(this IBasicOutputByteStream destinationStream, Int64 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Int64)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteUInt64BE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64BE(this Stream destinationStream, UInt64 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt64)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64BE(this IBasicOutputByteStream destinationStream, UInt64 value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(UInt64)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteSingleBE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingleBE(this Stream destinationStream, Single value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Single)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingleBE(this IBasicOutputByteStream destinationStream, Single value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Single)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteDoubleBE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDoubleBE(this Stream destinationStream, Double value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Double)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDoubleBE(this IBasicOutputByteStream destinationStream, Double value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Double)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region WriteDecimalBE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDecimalBE(this Stream destinationStream, Decimal value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Decimal)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDecimalBE(this IBasicOutputByteStream destinationStream, Decimal value)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            Span<Byte> buffer = stackalloc Byte[sizeof(Decimal)];
            buffer.CopyValueBE(value);
            destinationStream.WriteBytes(buffer);
        }

        #endregion

        #region CalculateCrc24

        public static (UInt32 Crc, UInt64 Length) CalculateCrc24(this Stream inputStream, bool leaveOpen = false)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.AsInputByteStream(true).InternalCalculateCrc24(MAX_BUFFER_SIZE, null);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc24(this Stream inputStream, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.AsInputByteStream(true).InternalCalculateCrc24(MAX_BUFFER_SIZE, progress);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc24(this Stream inputStream, Int32 bufferSize, bool leaveOpen = false)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.AsInputByteStream(true).InternalCalculateCrc24(bufferSize, null);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc24(this Stream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.AsInputByteStream(true).InternalCalculateCrc24(bufferSize, progress);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc24(this IBasicInputByteStream inputStream, bool leaveOpen = false)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.InternalCalculateCrc24(MAX_BUFFER_SIZE, null);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc24(this IBasicInputByteStream inputStream, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.InternalCalculateCrc24(MAX_BUFFER_SIZE, progress);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc24(this IBasicInputByteStream inputStream, Int32 bufferSize, bool leaveOpen = false)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.InternalCalculateCrc24(bufferSize, null);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc24(this IBasicInputByteStream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.InternalCalculateCrc24(bufferSize, progress);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        #endregion

        #region CalculateCrc32

        public static (UInt32 Crc, UInt64 Length) CalculateCrc32(this Stream inputStream, bool leaveOpen = false)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.AsInputByteStream(true).InternalCalculateCrc32(MAX_BUFFER_SIZE, null);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc32(this Stream inputStream, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.AsInputByteStream(true).InternalCalculateCrc32(MAX_BUFFER_SIZE, progress);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc32(this Stream inputStream, Int32 bufferSize, bool leaveOpen = false)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.AsInputByteStream(true).InternalCalculateCrc32(bufferSize, null);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc32(this Stream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.AsInputByteStream(true).InternalCalculateCrc32(bufferSize, progress);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc32(this IBasicInputByteStream inputStream, bool leaveOpen = false)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.InternalCalculateCrc32(MAX_BUFFER_SIZE, null);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc32(this IBasicInputByteStream inputStream, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.InternalCalculateCrc32(MAX_BUFFER_SIZE, progress);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc32(this IBasicInputByteStream inputStream, Int32 bufferSize, bool leaveOpen = false)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.InternalCalculateCrc32(bufferSize, null);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        public static (UInt32 Crc, UInt64 Length) CalculateCrc32(this IBasicInputByteStream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return inputStream.InternalCalculateCrc32(bufferSize, progress);
            }
            finally
            {
                if (!leaveOpen)
                    inputStream?.Dispose();
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<byte> InternalGetByteSequence<POSITION_T>(IInputByteStream<POSITION_T> baseStream, POSITION_T? offset, UInt64? count, IProgress<UInt64>? progress, bool leaveOpen)
            where POSITION_T : struct
        {
            const Int32 BUFFER_SIZE = 80 * 1024;

            try
            {
                var randomAccessStream = baseStream as IRandomInputByteStream<POSITION_T>;
                if (randomAccessStream is not null)
                {
                    if (offset.HasValue)
                        randomAccessStream.Seek(offset.Value);
                }
                else
                {
                    if (offset.HasValue)
                        throw new ArgumentException($"{nameof(offset)} must be null if {nameof(baseStream)} is sequential.", nameof(offset));
                }

                var buffer = new Byte[BUFFER_SIZE];
                var processedCount = 0UL;
                while (true)
                {
                    var readCount = buffer.Length;
                    if (count.HasValue)
                        readCount = (Int32)((UInt64)readCount).Minimum(count.Value - processedCount);
                    if (readCount <= 0)
                        break;
                    var length = baseStream.Read(buffer, 0, readCount);
                    if (length <= 0)
                        break;
                    for (var index = 0; index < length; ++index)
                        yield return buffer[index];
                    processedCount += (UInt32)length;
                    try
                    {
                        progress?.Report(processedCount);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            finally
            {
                if (!leaveOpen)
                    baseStream.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool InternalStreamBytesEqual(this IBasicInputByteStream stream1, IBasicInputByteStream stream2, IProgress<UInt64>? progress)
        {
            const Int32 bufferSize = 81920;

            if (bufferSize % sizeof(UInt64) != 0)
                throw new InternalLogicalErrorException();

            var buffer1 = new byte[bufferSize];
            var buffer2 = new byte[bufferSize];
            var processedCount = 0UL;
            while (true)
            {
                // まず両方のストリームから bufferSize バイトだけ読み込みを試みる
                var bufferCount1 = stream1.ReadBytes(buffer1);
                var bufferCount2 = stream2.ReadBytes(buffer2);
                processedCount += (UInt32)bufferCount1;
                try
                {
                    progress?.Report(processedCount);
                }
                catch (Exception)
                {
                }

                if (bufferCount1 != bufferCount2)
                {
                    // 実際に読み込めたサイズが異なっている場合はどちらかだけがEOFに達したということなので、ストリームの内容が異なると判断しfalseを返す。
                    return false;
                }

                // この時点で bufferCount1 == bufferCount2 (どちらのストリームも読み込めたサイズは同じ)

                if (!buffer1.SequenceEqual(0, buffer2, 0, bufferCount1))
                {
                    // バッファの内容が一致しなかった場合は false を返す。
                    return false;
                }

                if (bufferCount1 < buffer1.Length)
                {
                    // どちらのストリームも同時にEOFに達したがそれまでに読み込めたデータはすべて一致していた場合
                    // 全てのデータが一致したと判断して true を返す。
                    return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InternalCopyTo(this IBasicInputByteStream source, IBasicOutputByteStream destination, Int32 bufferSize, IProgress<UInt64>? progress)
        {
            var buffer = new byte[bufferSize];
            var processedCount = 0UL;
            while (true)
            {
                var length = source.ReadBytes(buffer);
                if (length <= 0)
                    break;
                destination.WriteBytes(buffer.AsSpan(0, length).AsReadOnly());
                processedCount += (UInt32)length;
                try
                {
                    progress?.Report(processedCount);
                }
                catch (Exception)
                {
                }
            }
            destination.Flush();
        }

        #region InternalReadBytes

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Int32 InternalReadBytes(Int32 offset, Int32 count, Func<Int32, Int32, Int32> reader)
        {
            var index = 0;
            while (count > 0)
            {
                var length = reader(offset + index, count);
                if (length <= 0)
                    break;
                index += length;
                count -= length;
            }
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Int32 InternalReadBytes(Memory<byte> buffer, Func<Memory<byte>, Int32> reader)
        {
            var index = 0;
            while (!buffer.IsEmpty)
            {
                var length = reader(buffer);
                if (length <= 0)
                    break;
                buffer = buffer[length..];
                index += length;
            }
            return index;
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<byte> InternalReadByteSequence(UInt64 count, Func<Int32, IEnumerable<byte>?> reader)
        {
            var byteArrayChain = Array.Empty<byte>().AsEnumerable();
            while (count > 0)
            {
                var length = (Int32)count.Minimum((UInt32)Int32.MaxValue);
                var data = reader(length);
                if (data is null)
                    break;
                byteArrayChain = byteArrayChain.Concat(data);
                count -= (UInt32)length;
            }
            return byteArrayChain;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlyMemory<byte> InternalReadAllBytes(Func<byte[], Int32, Int32, Int32> reader)
        {
            const Int32 BUFFER_SIZE = 80 * 2024;
            var buffers = new Queue<Byte[]>();
            var dataLength = 0;
            while (true)
            {
                var partialBuffer = new byte[BUFFER_SIZE];
                var length = reader(partialBuffer, 0, partialBuffer.Length);
                if (length <= 0)
                    break;
                if (length < partialBuffer.Length)
                    Array.Resize(ref partialBuffer, length);
                buffers.Enqueue(partialBuffer);
                dataLength += length;
            }
            if (buffers.Count <= 0)
                return Array.Empty<byte>();
            else if (buffers.Count == 1)
                return buffers.Dequeue();
            else
            {
                var buffer = new byte[dataLength];
                var destinationWindow = buffer.AsMemory();
                while (buffers.Count > 0)
                {
                    var partialBuffer = buffers.Dequeue();
                    partialBuffer.CopyTo(destinationWindow);
                    destinationWindow = destinationWindow[partialBuffer.Length..];
                }
#if DEBUG
                if (!destinationWindow.IsEmpty)
                    throw new Exception();
#endif
                return buffer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InternalWriteBytes(Int32 offset, Int32 count, Func<Int32, Int32, Int32> writer)
        {
            var index = 0;
            while (count > 0)
            {
                var length = writer(offset + index, count);
                if (length <= 0)
                    break;
                index += length;
                count -= length;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (UInt32 Crc, UInt64 Length) InternalCalculateCrc24(this IBasicInputByteStream inputStream, Int32 bufferSize, IProgress<UInt64>? progress) =>
            InternalCalculateCrc(inputStream, bufferSize, progress, Crc24.CreateCalculationState());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (UInt32 Crc, UInt64 Length) InternalCalculateCrc32(this IBasicInputByteStream inputStream, Int32 bufferSize, IProgress<UInt64>? progress) =>
            InternalCalculateCrc(inputStream, bufferSize, progress, Crc32.CreateCalculationState());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (UInt32 Crc, UInt64 Length) InternalCalculateCrc(IBasicInputByteStream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, ICrcCalculationState<UInt32, UInt64> session)
        {
            var buffer = new byte[bufferSize];
            var processedCount = 0UL;
            while (true)
            {
                var length = inputStream.Read(buffer);
                if (length <= 0)
                    break;
                session.Put(buffer, 0, length);
                processedCount += (UInt32)length;
                try
                {
                    progress?.Report(processedCount);
                }
                catch (Exception)
                {
                }
            }
            return session.GetResult();
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
                using var inputStream1 = new MemoryStream(item.data1);
                using var inputStream2 = new MemoryStream(item.data2);
                if (inputStream1.StreamBytesEqual(inputStream2, true) != item.expected)
                    throw new Exception();
            }
        }
#endif
    }
}

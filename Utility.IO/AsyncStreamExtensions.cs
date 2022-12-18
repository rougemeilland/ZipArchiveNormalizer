﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.IO
{
    public static class AsyncStreamExtensions
    {
        #region private class

        private class AsyncReverseByteSequenceByByteStream
            : AsyncReverseByteSequenceByByteStreamEnumerable<UInt64>
        {
            public AsyncReverseByteSequenceByByteStream(IRandomInputByteStream<UInt64> inputStream, UInt64 offset, UInt64 count, IProgress<UInt64>? progress, bool leaveOpen)
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

        #endregion

        private const Int32 _COPY_TO_DEFAULT_BUFFER_SIZE = 81920;
        private const Int32 _WRITE_BYTE_SEQUENCE_DEFAULT_BUFFER_SIZE = 81920;

        #region GetAsyncByteSequence

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this Stream baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetAsyncByteSequence();
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this Stream baseStream, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetAsyncByteSequence(progress);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this Stream baseStream, UInt64 offset, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetAsyncByteSequence(offset);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this Stream baseStream, UInt64 offset, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetAsyncByteSequence(offset, progress);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this Stream baseStream, UInt64 offset, UInt64 count, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetAsyncByteSequence(offset, count);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this Stream baseStream, UInt64 offset, UInt64 count, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetAsyncByteSequence(offset, count, progress);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this IInputByteStream<UInt64> baseStream, CancellationToken cancellationToken = default)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return InternalGetByteSequence(baseStream, null, null, null, false, cancellationToken);
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this IInputByteStream<UInt64> baseStream, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return InternalGetByteSequence(baseStream, null, null, null, leaveOpen, cancellationToken);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this IInputByteStream<UInt64> baseStream, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return InternalGetByteSequence(baseStream, null, null, progress, false, cancellationToken);
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this IInputByteStream<UInt64> baseStream, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return InternalGetByteSequence(baseStream, null, null, progress, leaveOpen, cancellationToken);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, CancellationToken cancellationToken = default)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteStream)
                    throw new NotSupportedException();
                if (offset > byteStream.Length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                return InternalGetByteSequence(byteStream, offset, byteStream.Length - offset, null, false, cancellationToken);
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteStream)
                    throw new NotSupportedException();
                if (offset > byteStream.Length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                return InternalGetByteSequence(byteStream, offset, byteStream.Length - offset, null, leaveOpen, cancellationToken);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteStream)
                    throw new NotSupportedException();
                if (offset > byteStream.Length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                return InternalGetByteSequence(byteStream, offset, byteStream.Length - offset, progress, false, cancellationToken);
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteStream)
                    throw new NotSupportedException();
                if (offset > byteStream.Length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                return InternalGetByteSequence(byteStream, offset, byteStream.Length - offset, progress, leaveOpen, cancellationToken);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, UInt64 count, CancellationToken cancellationToken = default)
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

                return InternalGetByteSequence(byteSteram, offset, count, null, false, cancellationToken);
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, UInt64 count, bool leaveOpen, CancellationToken cancellationToken = default)
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

                return InternalGetByteSequence(byteSteram, offset, count, null, leaveOpen, cancellationToken);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, UInt64 count, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
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

                return InternalGetByteSequence(byteSteram, offset, count, progress, false, cancellationToken);
            }
            catch (Exception)
            {
                baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, UInt64 count, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
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

                return InternalGetByteSequence(byteSteram, offset, count, progress, leaveOpen, cancellationToken);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        #endregion

        #region GetAsyncReverseByteSequence

        public static IAsyncEnumerable<byte> GetAsyncReverseByteSequence(this Stream baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetAsyncReverseByteSequence();
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncReverseByteSequence(this Stream baseStream, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetAsyncReverseByteSequence(progress);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncReverseByteSequence(this Stream baseStream, UInt64 offset, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return
                    baseStream.AsInputByteStream(leaveOpen)
                    .GetAsyncReverseByteSequence(offset);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncReverseByteSequence(this Stream baseStream, UInt64 offset, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return
                    baseStream.AsInputByteStream(leaveOpen)
                    .GetAsyncReverseByteSequence(offset, progress);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncReverseByteSequence(this Stream baseStream, UInt64 offset, UInt64 count, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetAsyncReverseByteSequence(offset, count);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncReverseByteSequence(this Stream baseStream, UInt64 offset, UInt64 count, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                return baseStream.AsInputByteStream(leaveOpen).GetAsyncReverseByteSequence(offset, count, progress);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncReverseByteSequence(this IInputByteStream<UInt64> baseStream, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();

                return new AsyncReverseByteSequenceByByteStream(byteSteram, 0, byteSteram.Length, null, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncReverseByteSequence(this IInputByteStream<UInt64> baseStream, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();

                return new AsyncReverseByteSequenceByByteStream(byteSteram, 0, byteSteram.Length, progress, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncReverseByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();

                return new AsyncReverseByteSequenceByByteStream(byteSteram, offset, byteSteram.Length - offset, null, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncReverseByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();

                return new AsyncReverseByteSequenceByByteStream(byteSteram, offset, byteSteram.Length - offset, progress, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncReverseByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, UInt64 count, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();

                return new AsyncReverseByteSequenceByByteStream(byteSteram, offset, count, null, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public static IAsyncEnumerable<byte> GetAsyncReverseByteSequence(this IInputByteStream<UInt64> baseStream, UInt64 offset, UInt64 count, IProgress<UInt64>? progress, bool leaveOpen = false)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (baseStream is not IRandomInputByteStream<UInt64> byteSteram)
                    throw new NotSupportedException();

                return new AsyncReverseByteSequenceByByteStream(byteSteram, offset, count, progress, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        #endregion

        #region StreamBytesEqualAsync

        public static async Task<bool> StreamBytesEqualAsync(this Stream stream1, Stream stream2, CancellationToken cancellationToken = default)
        {
            try
            {
                if (stream1 is null)
                    throw new ArgumentNullException(nameof(stream1));
                if (stream2 is null)
                    throw new ArgumentNullException(nameof(stream2));

                return await InternalStreamBytesEqualAsync(stream1, stream2, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (stream1 is not null)
                    await stream1.DisposeAsync().ConfigureAwait(false);
                if (stream2 is not null)
                    await stream2.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<bool> StreamBytesEqualAsync(this Stream stream1, Stream stream2, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (stream1 is null)
                    throw new ArgumentNullException(nameof(stream1));
                if (stream2 is null)
                    throw new ArgumentNullException(nameof(stream2));

                return await InternalStreamBytesEqualAsync(stream1, stream2, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (stream1 is not null)
                        await stream1.DisposeAsync().ConfigureAwait(false);
                    if (stream2 is not null)
                        await stream2.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<bool> StreamBytesEqualAsync(this Stream stream1, Stream stream2, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            try
            {
                if (stream1 is null)
                    throw new ArgumentNullException(nameof(stream1));
                if (stream2 is null)
                    throw new ArgumentNullException(nameof(stream2));

                return await InternalStreamBytesEqualAsync(stream1, stream2, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (stream1 is not null)
                    await stream1.DisposeAsync().ConfigureAwait(false);
                if (stream2 is not null)
                    await stream2.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<bool> StreamBytesEqualAsync(this Stream stream1, Stream stream2, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (stream1 is null)
                    throw new ArgumentNullException(nameof(stream1));
                if (stream2 is null)
                    throw new ArgumentNullException(nameof(stream2));

                return await InternalStreamBytesEqualAsync(stream1, stream2, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (stream1 is not null)
                        await stream1.DisposeAsync().ConfigureAwait(false);
                    if (stream2 is not null)
                        await stream2.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<bool> StreamBytesEqualAsync(this IBasicInputByteStream stream1, IBasicInputByteStream stream2, CancellationToken cancellationToken = default)
        {
            try
            {
                if (stream1 is null)
                    throw new ArgumentNullException(nameof(stream1));
                if (stream2 is null)
                    throw new ArgumentNullException(nameof(stream2));

                return await stream1.InternalStreamBytesEqualAsync(stream2, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (stream1 is not null)
                    await stream1.DisposeAsync().ConfigureAwait(false);
                if (stream2 is not null)
                    await stream2.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<bool> StreamBytesEqualAsync(this IBasicInputByteStream stream1, IBasicInputByteStream stream2, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (stream1 is null)
                    throw new ArgumentNullException(nameof(stream1));
                if (stream2 is null)
                    throw new ArgumentNullException(nameof(stream2));

                return await stream1.InternalStreamBytesEqualAsync(stream2, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (stream1 is not null)
                        await stream1.DisposeAsync().ConfigureAwait(false);
                    if (stream2 is not null)
                        await stream2.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<bool> StreamBytesEqualAsync(this IBasicInputByteStream stream1, IBasicInputByteStream stream2, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            try
            {
                if (stream1 is null)
                    throw new ArgumentNullException(nameof(stream1));
                if (stream2 is null)
                    throw new ArgumentNullException(nameof(stream2));

                return await stream1.InternalStreamBytesEqualAsync(stream2, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (stream1 is not null)
                    await stream1.DisposeAsync().ConfigureAwait(false);
                if (stream2 is not null)
                    await stream2.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<bool> StreamBytesEqualAsync(this IBasicInputByteStream stream1, IBasicInputByteStream stream2, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (stream1 is null)
                    throw new ArgumentNullException(nameof(stream1));
                if (stream2 is null)
                    throw new ArgumentNullException(nameof(stream2));

                return await stream1.InternalStreamBytesEqualAsync(stream2, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (stream1 is not null)
                        await stream1.DisposeAsync().ConfigureAwait(false);
                    if (stream2 is not null)
                        await stream2.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        #endregion

        #region CopyToAsync

#if false
        public static Task CopyToAsync(this Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException(); // defined in System.IO.Stream.CopyAsync(Stream, CancellationToken)
        }
#endif

#if false
        public static Task CopyToAsync(this Stream source, Stream destination, Int32 bufferSize, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException(); // defined in System.IO.Stream.CopyAsync(Stream, Int32, CancellationToken)
        }
#endif

        public static async Task CopyToAsync(this Stream source, Stream destination, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (source is null)
                    throw new ArgumentNullException(nameof(source));
                if (destination is null)
                    throw new ArgumentNullException(nameof(destination));

                await source.InternalCopyToAsync(destination, _COPY_TO_DEFAULT_BUFFER_SIZE, null, leaveOpen, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (source is not null)
                        await source.DisposeAsync().ConfigureAwait(false);
                    if (destination is not null)
                        await destination.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task CopyToAsync(this Stream source, Stream destination, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            try
            {
                if (source is null)
                    throw new ArgumentNullException(nameof(source));
                if (destination is null)
                    throw new ArgumentNullException(nameof(destination));

                await source.InternalCopyToAsync(destination, _COPY_TO_DEFAULT_BUFFER_SIZE, progress, false, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (source is not null)
                    await source.DisposeAsync().ConfigureAwait(false);
                if (destination is not null)
                    await destination.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task CopyToAsync(this Stream source, Stream destination, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (source is null)
                    throw new ArgumentNullException(nameof(source));
                if (destination is null)
                    throw new ArgumentNullException(nameof(destination));

                await source.InternalCopyToAsync(destination, _COPY_TO_DEFAULT_BUFFER_SIZE, progress, leaveOpen, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (source is not null)
                        await source.DisposeAsync().ConfigureAwait(false);
                    if (destination is not null)
                        await destination.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task CopyToAsync(this Stream source, Stream destination, Int32 bufferSize, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (source is null)
                    throw new ArgumentNullException(nameof(source));
                if (destination is null)
                    throw new ArgumentNullException(nameof(destination));

                await source.InternalCopyToAsync(destination, bufferSize, null, leaveOpen, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (source is not null)
                        await source.DisposeAsync().ConfigureAwait(false);
                    if (destination is not null)
                        await destination.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task CopyToAsync(this Stream source, Stream destination, Int32 bufferSize, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            try
            {
                if (source is null)
                    throw new ArgumentNullException(nameof(source));
                if (destination is null)
                    throw new ArgumentNullException(nameof(destination));

                await source.InternalCopyToAsync(destination, bufferSize, progress, false, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (source is not null)
                    await source.DisposeAsync().ConfigureAwait(false);
                if (destination is not null)
                    await destination.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task CopyToAsync(this Stream source, Stream destination, Int32 bufferSize, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (source is null)
                    throw new ArgumentNullException(nameof(source));
                if (destination is null)
                    throw new ArgumentNullException(nameof(destination));

                await source.InternalCopyToAsync(destination, bufferSize, progress, leaveOpen, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (source is not null)
                        await source.DisposeAsync().ConfigureAwait(false);
                    if (destination is not null)
                        await destination.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static Task CopyToAsync(this IBasicInputByteStream source, IBasicOutputByteStream destination, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            return source.InternalCopyToAsync(destination, _COPY_TO_DEFAULT_BUFFER_SIZE, null, false, cancellationToken);
        }

        public static Task CopyToAsync(this IBasicInputByteStream source, IBasicOutputByteStream destination, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            return source.InternalCopyToAsync(destination, _COPY_TO_DEFAULT_BUFFER_SIZE, null, leaveOpen, cancellationToken);
        }

        public static Task CopyToAsync(this IBasicInputByteStream source, IBasicOutputByteStream destination, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            return source.InternalCopyToAsync(destination, _COPY_TO_DEFAULT_BUFFER_SIZE, progress, false, cancellationToken);
        }

        public static Task CopyToAsync(this IBasicInputByteStream source, IBasicOutputByteStream destination, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            return source.InternalCopyToAsync(destination, _COPY_TO_DEFAULT_BUFFER_SIZE, progress, leaveOpen, cancellationToken);
        }

        public static Task CopyToAsync(this IBasicInputByteStream source, IBasicOutputByteStream destination, Int32 bufferSize, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            return source.InternalCopyToAsync(destination, bufferSize, null, false, cancellationToken);
        }

        public static Task CopyToAsync(this IBasicInputByteStream source, IBasicOutputByteStream destination, Int32 bufferSize, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            return source.InternalCopyToAsync(destination, bufferSize, null, leaveOpen, cancellationToken);
        }

        public static Task CopyToAsync(this IBasicInputByteStream source, IBasicOutputByteStream destination, Int32 bufferSize, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            return source.InternalCopyToAsync(destination, bufferSize, progress, false, cancellationToken);
        }

        public static Task CopyToAsync(this IBasicInputByteStream source, IBasicOutputByteStream destination, Int32 bufferSize, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            return source.InternalCopyToAsync(destination, bufferSize, progress, leaveOpen, cancellationToken);
        }

        #endregion

        #region ReadAsync

#if false
        public static Task<Int32> ReadAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException(); // defined in System.IO.Stream.ReadAsync(Memory<byte>, CancellationToken)
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadAsync(this Stream stream, byte[] buffer, Int32 offset, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return stream.ReadAsync(buffer.Slice(offset), cancellationToken).AsTask();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> ReadAsync(this Stream stream, byte[] buffer, UInt32 offset, CancellationToken cancellationToken = default)
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
                (UInt32)(
                    await stream.ReadAsync(
                        buffer.AsMemory((Int32)offset,
                        buffer.Length - (Int32)offset),
                        cancellationToken)
                    .ConfigureAwait(false)
                ).Maximum(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadAsync(this Stream stream, byte[] buffer, Range range, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            var (isOk, offset, count) = buffer.GetOffsetAndLength(range);
            if (!isOk)
                throw new ArgumentOutOfRangeException(nameof(range));

            return stream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

#if false
        public static async Task<Int32> ReadAsync(this Stream stream, byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException(); // defined in System.IO.Stream.ReadAsync(byte[], Int32, Int32, CancellationToken)
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> ReadAsync(this Stream stream, byte[] buffer, UInt32 offset, UInt32 count, CancellationToken cancellationToken = default)
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
                (UInt32)(
                    await stream.ReadAsync(
                        buffer.AsMemory((Int32)offset, buffer.Length - (Int32)offset),
                        cancellationToken)
                    .ConfigureAwait(false)
                ).Maximum(0);
        }

#if false
        public static Task<Int32> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();  // defined in System.IO.Stream.ReadAsync(Memory<byte>, CancellationToken)
        }
#endif

#if false
        public static Task<Int32> ReadAsync(this IBasicInputByteStream stream, byte[] buffer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException(); // Equivalent to IBasicInputByteStream.ReadAsync(Memory<Byte>, CancellationToken)
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadAsync(this IBasicInputByteStream stream, byte[] buffer, Int32 offset, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return stream.ReadAsync(buffer.Slice(offset), cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> ReadAsync(this IBasicInputByteStream stream, byte[] buffer, UInt32 offset, CancellationToken cancellationToken = default)
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
                (UInt32)(
                    await stream.ReadAsync(buffer.Slice((Int32)offset), cancellationToken).ConfigureAwait(false)
                ).Maximum(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadAsync(this IBasicInputByteStream stream, byte[] buffer, Range range, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            var (isOk, offset, count) = buffer.GetOffsetAndLength(range);
            if (!isOk)
                throw new ArgumentOutOfRangeException(nameof(range));

            return stream.ReadAsync(buffer.Slice(offset, count), cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadAsync(this IBasicInputByteStream stream, byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken = default)
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

            return stream.ReadAsync(buffer.Slice(offset, count), cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> ReadAsync(this IBasicInputByteStream stream, byte[] buffer, UInt32 offset, UInt32 count, CancellationToken cancellationToken = default)
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
                (UInt32)(
                    await stream.ReadAsync(buffer.Slice(offset, count), cancellationToken).ConfigureAwait(false)
                ).Maximum(0);
        }

#if false
        public static Task<Int32> ReadAsync(this IBasicInputByteStream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException(); // defined in IBasicInputByteStream.ReadAsync(Memory<Byte>, CancellationToken)
        }
#endif

        #endregion

        #region ReadByteOrNullAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Byte?> ReadByteOrNullAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new byte[1];
            var length = await sourceStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (length <= 0)
                return null;
            return buffer[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Byte?> ReadByteOrNullAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new byte[1];
            var length = await sourceStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (length <= 0)
                return null;
            return buffer[0];
        }

        #endregion

        #region ReadByteAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Byte> ReadByteAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new byte[1];
            var length = await sourceStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (length <= 0)
                throw new UnexpectedEndOfStreamException();
            return buffer[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Byte> ReadByteAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new byte[1];
            var length = await sourceStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (length <= 0)
                throw new UnexpectedEndOfStreamException();
            return buffer[0];
        }

        #endregion

        #region ReadBytesAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<ReadOnlyMemory<byte>> ReadBytesAsync(this Stream sourceStream, Int32 count, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var buffer = new byte[count];
            var length =
                await InternalReadBytesAsync(
                    0,
                    buffer.Length,
                    (_offset, _count) => sourceStream.ReadAsync(buffer, _offset, _count, cancellationToken))
                .ConfigureAwait(false);
            if (length < buffer.Length)
                Array.Resize(ref buffer, length);
            return buffer;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<ReadOnlyMemory<byte>> ReadBytesAsync(this Stream sourceStream, UInt32 count, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new byte[count];
            var length =
                await InternalReadBytesAsync(
                    0,
                    buffer.Length,
                    (_offset, _count) => sourceStream.ReadAsync(buffer, _offset, _count, cancellationToken))
                .ConfigureAwait(false);
            if (length < buffer.Length)
                Array.Resize(ref buffer, length);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadBytesAsync(this Stream sourceStream, byte[] buffer, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            return
                InternalReadBytesAsync(
                    0,
                    buffer.Length,
                    (_offset, _count) => sourceStream.ReadAsync(buffer, _offset, _count, cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadBytesAsync(this Stream sourceStream, byte[] buffer, Int32 offset, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return
                InternalReadBytesAsync(
                    offset,
                    buffer.Length - offset,
                    (_offset, _count) => sourceStream.ReadAsync(buffer, _offset, _count, cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> ReadBytesAsync(this Stream sourceStream, byte[] buffer, UInt32 offset, CancellationToken cancellationToken = default)
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
                (UInt32)(
                    await InternalReadBytesAsync(
                        (Int32)offset,
                        buffer.Length - (Int32)offset,
                        (_offset, _count) => sourceStream.ReadAsync(buffer, _offset, _count, cancellationToken))
                    .ConfigureAwait(false)
                ).Maximum(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadBytesAsync(this Stream sourceStream, byte[] buffer, Range range, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            var (isOk, offset, count) = buffer.GetOffsetAndLength(range);
            if (!isOk)
                throw new ArgumentOutOfRangeException(nameof(range));

            return
                InternalReadBytesAsync(
                    offset,
                    count,
                    (_offset, _count) => sourceStream.ReadAsync(buffer, _offset, _count, cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadBytesAsync(this Stream sourceStream, byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
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

            return
                InternalReadBytesAsync(
                    offset,
                    count,
                    (_offset, _count) => sourceStream.ReadAsync(buffer, _offset, _count, cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> ReadBytesAsync(this Stream sourceStream, byte[] buffer, UInt32 offset, UInt32 count, CancellationToken cancellationToken = default)
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
                (UInt32)(
                    await InternalReadBytesAsync(
                        (Int32)offset,
                        (Int32)count,
                        (_offset, _count) => sourceStream.ReadAsync(buffer, _offset, _count))
                    .ConfigureAwait(false)
                ).Maximum(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadBytesAsync(this Stream sourceStream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            return
                InternalReadBytesAsync(
                    buffer,
                    _buffer => sourceStream.ReadAsync(_buffer, cancellationToken).AsTask());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<ReadOnlyMemory<byte>> ReadBytesAsync(this IBasicInputByteStream sourceStream, Int32 count, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var buffer = new byte[count];
            var length =
                await InternalReadBytesAsync(
                    0,
                    buffer.Length,
                    (_offset, _count) => sourceStream.ReadAsync(buffer.Slice(_offset, _count), cancellationToken))
                .ConfigureAwait(false);
            if (length < buffer.Length)
                Array.Resize(ref buffer, length);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<ReadOnlyMemory<byte>> ReadBytesAsync(this IBasicInputByteStream sourceStream, UInt32 count, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new byte[count];
            var length =
                await InternalReadBytesAsync(
                    0,
                    buffer.Length,
                    (_offset, _count) => sourceStream.ReadAsync(buffer.Slice(_offset, _count), cancellationToken))
                .ConfigureAwait(false);
            if (length < buffer.Length)
                Array.Resize(ref buffer, length);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadBytesAsync(this IBasicInputByteStream sourceStream, byte[] buffer, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            return
                InternalReadBytesAsync(
                    0,
                    buffer.Length,
                    (_offset, _count) => sourceStream.ReadAsync(buffer.Slice(_offset, _count), cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadBytesAsync(this IBasicInputByteStream sourceStream, byte[] buffer, Int32 offset, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return
                InternalReadBytesAsync(
                    offset,
                    buffer.Length - offset,
                    (_offset, _count) => sourceStream.ReadAsync(buffer.Slice(_offset, _count), cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> ReadBytesAsync(this IBasicInputByteStream sourceStream, byte[] buffer, UInt32 offset, CancellationToken cancellationToken = default)
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
                (UInt32)(
                    await InternalReadBytesAsync(
                        (Int32)offset,
                        buffer.Length - (Int32)offset,
                        (_offset, _count) => sourceStream.ReadAsync(buffer.Slice(_offset, _count), cancellationToken))
                    .ConfigureAwait(false)
                ).Maximum(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadBytesAsync(this IBasicInputByteStream sourceStream, byte[] buffer, Range range, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            var (isOk, offset, count) = buffer.GetOffsetAndLength(range);
            if (!isOk)
                throw new ArgumentOutOfRangeException(nameof(range));

            return
                InternalReadBytesAsync(
                    offset,
                    count,
                    (_offset, _count) => sourceStream.ReadAsync(buffer.Slice(_offset, _count), cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadBytesAsync(this IBasicInputByteStream sourceStream, byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken = default)
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
                InternalReadBytesAsync(
                    offset,
                    count,
                    (_offset, _count) => sourceStream.ReadAsync(buffer.Slice(_offset, _count), cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> ReadBytesAsync(this IBasicInputByteStream sourceStream, byte[] buffer, UInt32 offset, UInt32 count, CancellationToken cancellationToken = default)
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
                (UInt32)(
                    await InternalReadBytesAsync(
                        (Int32)offset,
                        (Int32)count,
                        (_offset, _count) => sourceStream.ReadAsync(buffer.Slice(_offset, _count), cancellationToken))
                    .ConfigureAwait(false)
               ).Maximum(0);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> ReadBytesAsync(this IBasicInputByteStream sourceStream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            return
                InternalReadBytesAsync(
                    buffer,
                    _buffer => sourceStream.ReadAsync(_buffer, cancellationToken));
        }

        #endregion

        #region ReadByteSequenceAsync

        public static Task<IEnumerable<byte>> ReadByteSequenceAsync(this Stream sourceStream, Int64 count, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            return
                InternalReadByteSequenceAsync(
                    (UInt64)count,
                    async _count =>
                    {
                        var buffer = new byte[_count.Minimum(MAX_BUFFER_SIZE)];
                        var length = await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false);
                        if (length <= 0)
                            return null;
                        if (length < buffer.Length)
                            Array.Resize(ref buffer, length);
                        return buffer;
                    });
        }

        public static Task<IEnumerable<byte>> ReadByteSequenceAsync(this Stream sourceStream, UInt64 count, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            return
                InternalReadByteSequenceAsync(
                    count,
                    async _count =>
                    {
                        var buffer = new byte[_count.Minimum(MAX_BUFFER_SIZE)];
                        var length = await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false);
                        if (length <= 0)
                            return null;
                        if (length < buffer.Length)
                            Array.Resize(ref buffer, length);
                        return buffer;
                    });
        }

        public static Task<IEnumerable<byte>> ReadByteSequenceAsync(this IBasicInputByteStream sourceStream, Int64 count, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            return
                InternalReadByteSequenceAsync(
                    (UInt64)count,
                    async _count =>
                    {
                        var buffer = new byte[_count.Minimum(MAX_BUFFER_SIZE)];
                        var length = await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false);
                        if (length <= 0)
                            return null;
                        if (length < buffer.Length)
                            Array.Resize(ref buffer, length);
                        return buffer;
                    });
        }

        public static Task<IEnumerable<byte>> ReadByteSequenceAsync(this IBasicInputByteStream sourceStream, UInt64 count, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            return
                InternalReadByteSequenceAsync(
                    count,
                    async _count =>
                    {
                        var buffer = new byte[_count.Minimum(MAX_BUFFER_SIZE)];
                        var length = await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false);
                        if (length <= 0)
                            return null;
                        if (length < buffer.Length)
                            Array.Resize(ref buffer, length);
                        return buffer;
                    });
        }

        #endregion

        #region ReadAllBytesAsync

        public static Task<ReadOnlyMemory<byte>> ReadAllBytesAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            return
                InternalReadAllBytesAsync((
                _buffer,
                _offset,
                _count) => sourceStream.ReadAsync(_buffer, _offset, _count, cancellationToken));
        }

        public static Task<ReadOnlyMemory<byte>> ReadAllBytesAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            return
                InternalReadAllBytesAsync((
                _buffer,
                _offset,
                _count) => sourceStream.ReadAsync(_buffer.Slice(_offset, _count), cancellationToken));
        }

        #endregion

        #region ReadInt16LEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int16> ReadInt16LEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Int16)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt16LE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int16> ReadInt16LEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Int16)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt16LE();
        }

        #endregion

        #region ReadUInt16LEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt16> ReadUInt16LEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(UInt16)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt16LE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt16> ReadUInt16LEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(UInt16)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt16LE();
        }


        #endregion

        #region ReadInt32LEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int32> ReadInt32LEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Int32)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt32LE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int32> ReadInt32LEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Int32)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt32LE();
        }

        #endregion

        #region ReadUInt32LEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> ReadUInt32LEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(UInt32)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt32LE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> ReadUInt32LEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(UInt32)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt32LE();
        }


        #endregion

        #region ReadInt64LEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int64> ReadInt64LEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Int64)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt64LE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int64> ReadInt64LEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Int64)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt64LE();
        }

        #endregion

        #region ReadUInt64LEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt64> ReadUInt64LEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(UInt64)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt64LE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt64> ReadUInt64LEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(UInt64)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt64LE();
        }


        #endregion

        #region ReadSingleLEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Single> ReadSingleLEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Single)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToSingleLE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Single> ReadSingleLEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Single)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToSingleLE();
        }

        #endregion

        #region ReadDoubleLEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Double> ReadDoubleLEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Double)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDoubleLE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Double> ReadDoubleLEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Double)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDoubleLE();
        }

        #endregion

        #region ReadDecimalLEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Decimal> ReadDecimalLEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Decimal)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDecimalLE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Decimal> ReadDecimalLEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Decimal)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDecimalLE();
        }

        #endregion

        #region ReadInt16BEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int16> ReadInt16BEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Int16)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt16BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int16> ReadInt16BEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Int16)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt16BE();
        }

        #endregion

        #region ReadUInt16BEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt16> ReadUInt16BEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(UInt16)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt16BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt16> ReadUInt16BEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(UInt16)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt16BE();
        }


        #endregion

        #region ReadInt32BEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int32> ReadInt32BEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Int32)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt32BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int32> ReadInt32BEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Int32)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt32BE();
        }

        #endregion

        #region ReadUInt32BEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> ReadUInt32BEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(UInt32)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt32BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> ReadUInt32BEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(UInt32)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt32BE();
        }


        #endregion

        #region ReadInt64BEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int64> ReadInt64BEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Int64)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt64BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int64> ReadInt64BEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Int64)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToInt64BE();
        }

        #endregion

        #region ReadUInt64BEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt64> ReadUInt64BEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(UInt64)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt64BE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt64> ReadUInt64BEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(UInt64)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToUInt64BE();
        }


        #endregion

        #region ReadSingleBEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Single> ReadSingleBEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Single)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToSingleBE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Single> ReadSingleBEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Single)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToSingleBE();
        }

        #endregion

        #region ReadDoubleBEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Double> ReadDoubleBEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Double)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDoubleBE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Double> ReadDoubleBEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Double)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDoubleBE();
        }

        #endregion

        #region ReadDecimalBEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Decimal> ReadDecimalBEAsync(this Stream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Decimal)];
            if (await sourceStream.ReadBytesAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDecimalBE();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Decimal> ReadDecimalBEAsync(this IBasicInputByteStream sourceStream, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));

            var buffer = new Byte[sizeof(Decimal)];
            if (await sourceStream.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false) != buffer.Length)
                throw new UnexpectedEndOfStreamException();
            return buffer.ToDecimalBE();
        }

        #endregion

        #region WriteAsync

#if false
        public static Task<Int32> WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException(); // equivalent to System.IO.Stream.WriteAsync(ReadOnlyMemory<Byte>, CancellationToken)
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int32> WriteAsync(this Stream stream, byte[] buffer, Int32 offset, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            var count = buffer.Length - offset;
            await stream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> WriteAsync(this Stream stream, byte[] buffer, UInt32 offset, CancellationToken cancellationToken = default)
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
            await stream.WriteAsync(buffer.AsMemory((Int32)offset, count), cancellationToken).ConfigureAwait(false);
            return (UInt32)count;
        }

#if false

        public static Task<Int32> WriteAsync(this Stream stream, byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException(); // defined in System.IO.Stream.WriteAsync(byte[], Int32, Int32, CancellationToken)
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> WriteAsync(this Stream stream, byte[] buffer, UInt32 offset, UInt32 count, CancellationToken cancellationToken = default)
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

            await stream.WriteAsync(buffer.AsMemory((Int32)offset, (Int32)count), cancellationToken).ConfigureAwait(false);
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Int32> WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            return buffer.Length;
        }

#if false
        public static Task<UInt32> WriteAsync(this IBasicOutputByteStream stream, byte[] buffer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException(); // equivalent to IBasicOutputByteStream.WriteAsync(ReadOnlyMemory<Byte>, CancellationToken)
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> WriteAsync(this IBasicOutputByteStream stream, byte[] buffer, Int32 offset, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return stream.WriteAsync(buffer.Slice(offset), cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> WriteAsync(this IBasicOutputByteStream stream, byte[] buffer, UInt32 offset, CancellationToken cancellationToken = default)
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

            return (UInt32)(await stream.WriteAsync(buffer.Slice((Int32)offset), cancellationToken).ConfigureAwait(false)).Maximum(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Int32> WriteAsync(this IBasicOutputByteStream stream, byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken = default)
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

            return stream.WriteAsync(buffer.Slice(offset, count), cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<UInt32> WriteAsync(this IBasicOutputByteStream stream, byte[] buffer, UInt32 offset, UInt32 count, CancellationToken cancellationToken = default)
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

            return
                (UInt32)(
                    await stream.WriteAsync(
                        buffer.Slice(offset, count),
                        cancellationToken)
                    .ConfigureAwait(false)
                ).Maximum(0);
        }

#if false
        public static Task<Int32> WriteAsync(this IBasicOutputByteStream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException(); // defined in IBasicOutputByteStream.WriteAsync(ReadOnlyMemory<Byte>, CancellationToken) 
        }
#endif

        #endregion

        #region WriteByteAsync

#if false
        public static Task WriteByteAsync(this Stream stream, byte value, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException(); // defined in System.IO.Stream.WriteByteAsync(Byte, CancellationToken) 
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task WriteByteAsync(this IBasicOutputByteStream stream, byte value, CancellationToken cancellationToken = default)
        {
            var length = await stream.WriteAsync(new[] { value }, cancellationToken).ConfigureAwait(false);
            if (length <= 0)
                throw new InternalLogicalErrorException();
        }

        #endregion

        #region WriteBytesAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteBytesAsync(this Stream destinationStream, byte[] buffer, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            return destinationStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteBytesAsync(this Stream destinationStream, byte[] buffer, Int32 offset, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return destinationStream.WriteAsync(buffer, offset, buffer.Length - offset, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteBytesAsync(this Stream destinationStream, byte[] buffer, UInt32 offset, CancellationToken cancellationToken = default)
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

            return destinationStream.WriteAsync(buffer, (Int32)offset, buffer.Length - (Int32)offset, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteBytesAsync(this Stream destinationStream, byte[] buffer, Range range, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            var (isOk, offset, count) = buffer.GetOffsetAndLength(range);
            if (!isOk)
                throw new ArgumentOutOfRangeException(nameof(range));

            return destinationStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteBytesAsync(this Stream destinationStream, byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken = default)
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

            return destinationStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteBytesAsync(this Stream destinationStream, byte[] buffer, UInt32 offset, UInt32 count, CancellationToken cancellationToken = default)
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

            return destinationStream.WriteAsync(buffer, (Int32)offset, (Int32)count, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteBytesAsync(this Stream destinationStream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            return destinationStream.WriteAsync(buffer, cancellationToken).AsTask();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteBytesAsync(this IBasicOutputByteStream destinationStream, byte[] buffer, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            return
                InternalWriteBytesAsync(
                    0,
                    buffer.Length,
                    (_offset, _count) => destinationStream.WriteAsync(buffer.Slice(_offset, _count), cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteBytesAsync(this IBasicOutputByteStream destinationStream, byte[] buffer, Int32 offset, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (!offset.IsBetween(0, buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return
                InternalWriteBytesAsync(
                    0,
                    buffer.Length,
                    (_offset, _count) => destinationStream.WriteAsync(buffer.Slice(_offset, _count), cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteBytesAsync(this IBasicOutputByteStream destinationStream, byte[] buffer, UInt32 offset, CancellationToken cancellationToken = default)
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

            return
                InternalWriteBytesAsync(
                    (Int32)offset,
                    buffer.Length - (Int32)offset,
                    (_offset, _count) => destinationStream.WriteAsync(buffer.Slice(_offset, _count), cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteBytesAsync(this IBasicOutputByteStream destinationStream, byte[] buffer, Range range, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            var (isOk, offset, count) = buffer.GetOffsetAndLength(range);
            if (!isOk)
                throw new ArgumentOutOfRangeException(nameof(range));

            return
                InternalWriteBytesAsync(
                    offset,
                    count,
                    (_offset, _count) => destinationStream.WriteAsync(buffer.Slice(_offset, _count), cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteBytesAsync(this IBasicOutputByteStream destinationStream, byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken = default)
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

            return
                InternalWriteBytesAsync(
                    offset,
                    count,
                    (_offset, _count) => destinationStream.WriteAsync(buffer.Slice(_offset, _count), cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteBytesAsync(this IBasicOutputByteStream destinationStream, byte[] buffer, UInt32 offset, UInt32 count, CancellationToken cancellationToken = default)
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

            return
                InternalWriteBytesAsync(
                    (Int32)offset,
                    (Int32)count,
                    (_offset, _count) => destinationStream.WriteAsync(buffer.Slice(_offset, _count), cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task WriteBytesAsync(this IBasicOutputByteStream destinationStream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            while (!buffer.IsEmpty)
            {
                var length = await destinationStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (length <= 0)
                    throw new IOException("Can not write any more");
                buffer = buffer[length..];
            }
        }

        #endregion

        #region WriteByteSequenceAsync

        public static async Task WriteByteSequenceAsync(this Stream destinationStream, IEnumerable<byte> sequence, CancellationToken cancellationToken = default)
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
                    await destinationStream.WriteAsync(buffer.AsMemory(0, index), cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task WriteByteSequenceAsync(this Stream destinationStream, IAsyncEnumerable<byte> sequence, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (sequence is null)
                throw new ArgumentNullException(nameof(sequence));

            var enumerator = sequence.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var buffer = new Byte[_WRITE_BYTE_SEQUENCE_DEFAULT_BUFFER_SIZE];
                var isEndOfSequence = false;
                while (!isEndOfSequence)
                {
                    var index = 0;
                    while (index < buffer.Length)
                    {
                        if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            isEndOfSequence = true;
                            break;
                        }
                        buffer[index++] = enumerator.Current;
                    }
                    if (index > 0)
                        await destinationStream.WriteAsync(buffer.AsMemory(0, index), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public static async Task WriteByteSequenceAsync(this IBasicOutputByteStream destinationStream, IEnumerable<byte> sequence, CancellationToken cancellationToken = default)
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
                    await destinationStream.WriteAsync(buffer.Slice(0, index), cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task WriteByteSequenceAsync(this IBasicOutputByteStream destinationStream, IAsyncEnumerable<byte> sequence, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (sequence is null)
                throw new ArgumentNullException(nameof(sequence));

            var enumerator = sequence.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var buffer = new Byte[_WRITE_BYTE_SEQUENCE_DEFAULT_BUFFER_SIZE];
                var isEndOfSequence = false;
                while (!isEndOfSequence)
                {
                    var index = 0;
                    while (index < buffer.Length)
                    {
                        if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            isEndOfSequence = true;
                            break;
                        }
                        buffer[index++] = enumerator.Current;
                    }
                    if (index > 0)
                        await destinationStream.WriteAsync(buffer.Slice(0, index), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        #endregion

        #region WriteInt16LEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteInt16LEAsync(this Stream destinationStream, Int16 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Int16)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteInt16LEAsync(this IBasicOutputByteStream destinationStream, Int16 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Int16)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteUInt16LEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteUInt16LEAsync(this Stream destinationStream, UInt16 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(UInt16)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteUInt16LEAsync(this IBasicOutputByteStream destinationStream, UInt16 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(UInt16)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteInt32LEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteInt32LEAsync(this Stream destinationStream, Int32 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Int32)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteInt32LEAsync(this IBasicOutputByteStream destinationStream, Int32 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Int32)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteUInt32LEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteUInt32LEAsync(this Stream destinationStream, UInt32 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(UInt32)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteUInt32LEAsync(this IBasicOutputByteStream destinationStream, UInt32 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(UInt32)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteInt64LEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteInt64LEAsync(this Stream destinationStream, Int64 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Int64)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteInt64LEAsync(this IBasicOutputByteStream destinationStream, Int64 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Int64)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteUInt64LEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteUInt64LEAsync(this Stream destinationStream, UInt64 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(UInt64)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteUInt64LEAsync(this IBasicOutputByteStream destinationStream, UInt64 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(UInt64)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteSingleLEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteSingleLEAsync(this Stream destinationStream, Single value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Single)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteSingleLEAsync(this IBasicOutputByteStream destinationStream, Single value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Single)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteDoubleLEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteDoubleLEAsync(this Stream destinationStream, Double value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Double)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteDoubleLEAsync(this IBasicOutputByteStream destinationStream, Double value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Double)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteDecimalLEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteDecimalLEAsync(this Stream destinationStream, Decimal value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Decimal)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteDecimalLEAsync(this IBasicOutputByteStream destinationStream, Decimal value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Decimal)];
            buffer.CopyValueLE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteInt16BEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteInt16BEAsync(this Stream destinationStream, Int16 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Int16)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteInt16BEAsync(this IBasicOutputByteStream destinationStream, Int16 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Int16)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteUInt16BEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteUInt16BEAsync(this Stream destinationStream, UInt16 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(UInt16)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteUInt16BEAsync(this IBasicOutputByteStream destinationStream, UInt16 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(UInt16)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteInt32BEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteInt32BEAsync(this Stream destinationStream, Int32 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Int32)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteInt32BEAsync(this IBasicOutputByteStream destinationStream, Int32 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Int32)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteUInt32BEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteUInt32BEAsync(this Stream destinationStream, UInt32 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(UInt32)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteUInt32BEAsync(this IBasicOutputByteStream destinationStream, UInt32 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(UInt32)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteInt64BEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteInt64BEAsync(this Stream destinationStream, Int64 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Int64)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteInt64BEAsync(this IBasicOutputByteStream destinationStream, Int64 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Int64)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteUInt64BEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteUInt64BEAsync(this Stream destinationStream, UInt64 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(UInt64)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteUInt64BEAsync(this IBasicOutputByteStream destinationStream, UInt64 value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(UInt64)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteSingleBEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteSingleBEAsync(this Stream destinationStream, Single value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Single)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteSingleBEAsync(this IBasicOutputByteStream destinationStream, Single value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Single)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteDoubleBEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteDoubleBEAsync(this Stream destinationStream, Double value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Double)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteDoubleBEAsync(this IBasicOutputByteStream destinationStream, Double value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Double)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region WriteDecimalBEAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteDecimalBEAsync(this Stream destinationStream, Decimal value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Decimal)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteDecimalBEAsync(this IBasicOutputByteStream destinationStream, Decimal value, CancellationToken cancellationToken = default)
        {
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));

            var buffer = new Byte[sizeof(Decimal)];
            buffer.CopyValueBE(0, value);
            return destinationStream.WriteBytesAsync(buffer, cancellationToken);
        }

        #endregion

        #region CalculateCrc24Async

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this Stream inputStream, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc24Async(MAX_BUFFER_SIZE, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this Stream inputStream, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc24Async(MAX_BUFFER_SIZE, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this Stream inputStream, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc24Async(MAX_BUFFER_SIZE, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this Stream inputStream, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc24Async(MAX_BUFFER_SIZE, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this Stream inputStream, Int32 bufferSize, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc24Async(bufferSize, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this Stream inputStream, Int32 bufferSize, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc24Async(bufferSize, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this Stream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc24Async(bufferSize, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this Stream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc24Async(bufferSize, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this IBasicInputByteStream inputStream, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc24Async(MAX_BUFFER_SIZE, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this IBasicInputByteStream inputStream, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc24Async(MAX_BUFFER_SIZE, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this IBasicInputByteStream inputStream, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc24Async(MAX_BUFFER_SIZE, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this IBasicInputByteStream inputStream, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc24Async(MAX_BUFFER_SIZE, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this IBasicInputByteStream inputStream, Int32 bufferSize, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc24Async(bufferSize, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this IBasicInputByteStream inputStream, Int32 bufferSize, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc24Async(bufferSize, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this IBasicInputByteStream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc24Async(bufferSize, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this IBasicInputByteStream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc24Async(bufferSize, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        #endregion

        #region CalculateCrc32Async

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this Stream inputStream, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc32Async(MAX_BUFFER_SIZE, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this Stream inputStream, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc32Async(MAX_BUFFER_SIZE, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this Stream inputStream, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc32Async(MAX_BUFFER_SIZE, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this Stream inputStream, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc32Async(MAX_BUFFER_SIZE, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this Stream inputStream, Int32 bufferSize, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc32Async(bufferSize, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this Stream inputStream, Int32 bufferSize, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc32Async(bufferSize, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this Stream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc32Async(bufferSize, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this Stream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.AsInputByteStream(true).InternalCalculateCrc32Async(bufferSize, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this IBasicInputByteStream inputStream, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc32Async(MAX_BUFFER_SIZE, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this IBasicInputByteStream inputStream, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc32Async(MAX_BUFFER_SIZE, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this IBasicInputByteStream inputStream, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc32Async(MAX_BUFFER_SIZE, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this IBasicInputByteStream inputStream, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            const Int32 MAX_BUFFER_SIZE = 80 * 1024;

            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc32Async(MAX_BUFFER_SIZE, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this IBasicInputByteStream inputStream, Int32 bufferSize, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc32Async(bufferSize, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this IBasicInputByteStream inputStream, Int32 bufferSize, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc32Async(bufferSize, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this IBasicInputByteStream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc32Async(bufferSize, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (inputStream is not null)
                    await inputStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public static async Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this IBasicInputByteStream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken = default)
        {
            try
            {
                if (inputStream is null)
                    throw new ArgumentNullException(nameof(inputStream));

                return await inputStream.InternalCalculateCrc32Async(bufferSize, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (!leaveOpen)
                {
                    if (inputStream is not null)
                        await inputStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async IAsyncEnumerable<byte> InternalGetByteSequence<POSITION_T>(IInputByteStream<POSITION_T> baseStream, POSITION_T? offset, UInt64? count, IProgress<UInt64>? progress, bool leaveOpen, [EnumeratorCancellation] CancellationToken cancellationToken)
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
                    var length = await baseStream.ReadAsync(buffer, 0, readCount, cancellationToken).ConfigureAwait(false);
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

        #region InternalStreamBytesEqualAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<bool> InternalStreamBytesEqualAsync(this Stream stream1, Stream stream2, IProgress<UInt64>? progress, CancellationToken cancellationToken)
        {
            var byteStream1 = stream1.AsInputByteStream(true);
            await using (byteStream1.ConfigureAwait(false))
            {
                var byteStream2 = stream2.AsInputByteStream(true);
                await using (byteStream2.ConfigureAwait(false))
                {
                    return await byteStream1.InternalStreamBytesEqualAsync(byteStream2, progress, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<bool> InternalStreamBytesEqualAsync(this IBasicInputByteStream stream1, IBasicInputByteStream stream2, IProgress<UInt64>? progress, CancellationToken cancellationToken)
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
                var bufferCount1 = await stream1.ReadBytesAsync(buffer1, cancellationToken).ConfigureAwait(false);
                var bufferCount2 = await stream2.ReadBytesAsync(buffer2, cancellationToken).ConfigureAwait(false);
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

                if (!buffer1.AsSpan(0, bufferCount1).SequenceEqual(buffer2.AsSpan(0, bufferCount2)))
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

        #endregion

        #region InternalCopyToAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task InternalCopyToAsync(this Stream source, Stream destination, Int32 bufferSize, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken)
        {
            try
            {
                var sourceByteStream = source.AsInputByteStream(leaveOpen);
                await using (sourceByteStream.ConfigureAwait(false))
                {
                    var destinationByteStream = destination.AsOutputByteStream(leaveOpen);
                    await using (destinationByteStream.ConfigureAwait(false))
                    {
                        await sourceByteStream.InternalCopyToAsync(destinationByteStream, bufferSize, progress, leaveOpen, cancellationToken).ConfigureAwait(false);
                    }
                }

            }
            finally
            {
                if (!leaveOpen)
                {
                    if (source is not null)
                        await source.DisposeAsync().ConfigureAwait(false);
                    if (destination is not null)
                        await destination.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task InternalCopyToAsync(this IBasicInputByteStream source, IBasicOutputByteStream destination, Int32 bufferSize, IProgress<UInt64>? progress, bool leaveOpen, CancellationToken cancellationToken)
        {
            try
            {
                var buffer = new byte[bufferSize];
                var processedCount = 0UL;
                while (true)
                {
                    var length = await source.ReadBytesAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (length <= 0)
                        break;
                    await destination.WriteBytesAsync(buffer, cancellationToken).ConfigureAwait(false);
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
            finally
            {
                if (!leaveOpen)
                {
                    if (source is not null)
                        await source.DisposeAsync().ConfigureAwait(false);
                    if (destination is not null)
                        await destination.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        #endregion

        #region InternalReadBytesAsync

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<Int32> InternalReadBytesAsync(Int32 offset, Int32 count, Func<Int32, Int32, Task<Int32>> reader)
        {
            var index = 0;
            while (count > 0)
            {
                var length = await reader(offset + index, count).ConfigureAwait(false);
                if (length <= 0)
                    break;
                index += length;
                count -= length;
            }
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<Int32> InternalReadBytesAsync(Memory<byte> buffer, Func<Memory<byte>, Task<Int32>> reader)
        {
            var index = 0;
            while (!buffer.IsEmpty)
            {
                var length = await reader(buffer).ConfigureAwait(false);
                if (length <= 0)
                    break;
                buffer = buffer[length..];
                index += length;
            }
            return index;
        }

        #endregion

        private static async Task<IEnumerable<byte>> InternalReadByteSequenceAsync(UInt64 count, Func<Int32, Task<IEnumerable<byte>?>> reader)
        {
            var byteArrayChain = Array.Empty<byte>().AsEnumerable();
            while (count > 0)
            {
                var length = (Int32)count.Minimum((UInt32)Int32.MaxValue);
                var data = await reader(length).ConfigureAwait(false);
                if (data is null)
                    break;
                byteArrayChain = byteArrayChain.Concat(data);
                count -= (UInt32)length;
            }
            return byteArrayChain;
        }

        private static async Task<ReadOnlyMemory<byte>> InternalReadAllBytesAsync(Func<byte[], Int32, Int32, Task<Int32>> reader)
        {
            const Int32 BUFFER_SIZE = 80 * 2024;
            var buffers = new Queue<Byte[]>();
            var dataLength = 0;
            while (true)
            {
                var partialBuffer = new byte[BUFFER_SIZE];
                var length = await reader(partialBuffer, 0, partialBuffer.Length).ConfigureAwait(false);
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
        private static async Task InternalWriteBytesAsync(Int32 offset, Int32 count, Func<Int32, Int32, Task<Int32>> writer)
        {
            var index = 0;
            while (count > 0)
            {
                var length = await writer(offset + index, count).ConfigureAwait(false);
                if (length <= 0)
                    break;
                index += length;
                count -= length;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task<(UInt32 Crc, UInt64 Length)> InternalCalculateCrc24Async(this IBasicInputByteStream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, CancellationToken cancellationToken) =>
            InternalCalculateCrcAsync(inputStream, bufferSize, progress, Crc24.CreateCalculationState(), cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task<(UInt32 Crc, UInt64 Length)> InternalCalculateCrc32Async(this IBasicInputByteStream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, CancellationToken cancellationToken) =>
            InternalCalculateCrcAsync(inputStream, bufferSize, progress, Crc32.CreateCalculationState(), cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<(UInt32 Crc, UInt64 Length)> InternalCalculateCrcAsync(IBasicInputByteStream inputStream, Int32 bufferSize, IProgress<UInt64>? progress, ICrcCalculationState<UInt32, UInt64> session, CancellationToken cancellationToken)
        {
            var buffer = new byte[bufferSize];
            var processedCount = 0UL;
            while (true)
            {
                var length = await inputStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
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
    }
}

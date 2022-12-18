using SevenZip.Compression.Lzma;
using System;
using System.Threading;
using System.Threading.Tasks;
using Utility.IO;

namespace ZipUtility.IO.Compression.Lzma
{
    public class LzmaEncoderPlugin
        : LzmaCoderPlugin, ICompressionEncoder
    {
        public void Encode(IInputByteStream<UInt64> sourceStream, IOutputByteStream<UInt64> destinationStream, ICoderOption option, UInt64? sourceSize, IProgress<UInt64>? progress = null)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (option is null)
                throw new ArgumentNullException(nameof(option));
            if (option is not LzmaCompressionOption lzmaOption)
                throw new ArgumentException($"Illegal {nameof(option)} data", nameof(option));

            InternalEncode(sourceStream, destinationStream, sourceSize, progress, lzmaOption);
        }

        public async Task<Exception?> EncodeAsync(IInputByteStream<UInt64> sourceStream, IOutputByteStream<UInt64> destinationStream, ICoderOption option, UInt64? sourceSize, IProgress<UInt64>? progress = null, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (option is null)
                throw new ArgumentNullException(nameof(option));
            if (sourceSize is null)
                throw new ArgumentNullException(nameof(sourceSize));
            if (option is not LzmaCompressionOption lzmaOption)
                throw new ArgumentException($"Illegal {nameof(option)} data", nameof(option));

            return
                await Task.Run(() =>
                {
                    try
                    {
                        InternalEncode(sourceStream, destinationStream, sourceSize, progress, lzmaOption);
                        return null;
                    }
                    catch (Exception ex)
                    {
                        return ex;
                    }
                });
        }

        private static void InternalEncode(IInputByteStream<ulong> sourceStream, IOutputByteStream<ulong> destinationStream, ulong? sourceSize, IProgress<ulong>? progress, LzmaCompressionOption lzmaOption)
        {
            var encoder =
                new LzmaEncoder(
                    new LzmaEncoderProperties { EndMarker = lzmaOption.UseEndOfStreamMarker },
                    (outStream, propertyGetter) =>
                    {
                        Span<Byte> propertyBuffer = stackalloc Byte[LzmaEncoder.MaximumPropertyLength];
                        var propertyLength = propertyGetter.GetEncoderProperties(propertyBuffer);
                        propertyBuffer = propertyBuffer[..propertyLength];
                        destinationStream.WriteByte(Constants.LZMA_COMPRESSION_MAJOR_VERSION);
                        destinationStream.WriteByte(Constants.LZMA_COMPRESSION_MINOR_VERSION);
                        destinationStream.WriteUInt16LE((UInt16)propertyBuffer.Length);
                        destinationStream.WriteBytes(propertyBuffer);
                        return sizeof(Byte) + sizeof(Byte) + sizeof(UInt16) + propertyLength;
                    });
            try
            {
                encoder.Encode(sourceStream, destinationStream, sourceSize, null, progress);

            }
            catch (UnexpectedEndOfStreamException ex)
            {
                throw new DataErrorException("Too short LZMA header", ex);
            }
        }
    }
}

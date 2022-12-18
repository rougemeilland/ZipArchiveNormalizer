using SevenZip;
using SevenZip.Compression.Lzma;
using System;
using System.Threading;
using System.Threading.Tasks;
using Utility.IO;

namespace ZipUtility.IO.Compression.Lzma
{
    public class LzmaDecoderPlugin
        : LzmaCoderPlugin, ICompressionDecoder
    {
        public void Decode(IInputByteStream<UInt64> sourceStream, IOutputByteStream<UInt64> destinationStream, ICoderOption option, UInt64 unpackedSize, UInt64 packedSize, IProgress<UInt64>? progress = null)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (option is null)
                throw new ArgumentNullException(nameof(option));
            if (option is not LzmaCompressionOption)
                throw new ArgumentException($"Illegal {nameof(option)} data", nameof(option));

            InternalDecode(sourceStream, destinationStream, unpackedSize, packedSize, progress);
        }

        public async Task<Exception?> DecodeAsync(IInputByteStream<UInt64> sourceStream, IOutputByteStream<UInt64> destinationStream, ICoderOption option, UInt64 unpackedSize, UInt64 packedSize, IProgress<UInt64>? progress = null, CancellationToken cancellationToken = default)
        {
            if (sourceStream is null)
                throw new ArgumentNullException(nameof(sourceStream));
            if (destinationStream is null)
                throw new ArgumentNullException(nameof(destinationStream));
            if (option is null)
                throw new ArgumentNullException(nameof(option));
            if (option is not LzmaCompressionOption)
                throw new ArgumentException($"Illegal {nameof(option)} data", nameof(option));


            return
                await Task.Run(() =>
                {
                    try
                    {
                        InternalDecode(sourceStream, destinationStream, unpackedSize, packedSize, progress);
                        return null;
                    }
                    catch (Exception ex)
                    {
                        return ex;
                    }
                });
        }

        private static void InternalDecode(IInputByteStream<ulong> sourceStream, IOutputByteStream<ulong> destinationStream, ulong unpackedSize, ulong packedSize, IProgress<ulong>? progress)
        {
            var decoder = CreateDecoder(sourceStream);
            try
            {
                decoder.Decode(sourceStream, destinationStream, unpackedSize, packedSize, progress);
            }
            catch (SevenZipDataErrorException ex)
            {
                throw new DataErrorException("Detected data error", ex);
            }
        }

        private static LzmaDecoder CreateDecoder(IInputByteStream<ulong> sourceStream)
        {
            Span<Byte> propertiesBuffer = stackalloc Byte[LzmaDecoder.MaximumPropertyLength];
            var (headerLength, propertiesLength) = ReadProperties(sourceStream, propertiesBuffer);
            return new LzmaDecoder(propertiesBuffer[..headerLength]);
        }

        private static (Int32 headerLength, Int32 propertiesLength) ReadProperties(IInputByteStream<UInt64> baseStream, Span<Byte> propertiesBuffer)
        {
            try
            {
                var majorVersion = baseStream.ReadByte();
                var minorVersion = baseStream.ReadByte();
                var propertyLength = baseStream.ReadUInt16LE();
                if (propertyLength > propertiesBuffer.Length)
                    throw new DataErrorException();
                propertiesBuffer = propertiesBuffer[..propertyLength];
                var length = baseStream.ReadBytes(propertiesBuffer);
                if (length != propertiesBuffer.Length)
                    throw new UnexpectedEndOfStreamException();
                return (sizeof(Byte) + sizeof(Byte) + sizeof(UInt16) + propertyLength, propertyLength);
            }
            catch (UnexpectedEndOfStreamException ex)
            {
                throw new DataErrorException("Too short LZMA header", ex);
            }
        }

    }
}

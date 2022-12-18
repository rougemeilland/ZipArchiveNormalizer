using System;
using Utility;
using Utility.IO;

namespace ZipUtility.ZipFileHeader
{
    static class StreamExtensions
    {
        public static Int64 FindFirstSigunature(this IRandomInputByteStream<UInt64> inputStream, ReadOnlyMemory<byte> signature, UInt64 offset, UInt64 count)
        {
            return inputStream.FindFirstSigunature(signature.Length, offset, count, (buffer, index) => buffer.Span.SigunatureEqual(index, signature.Span));
        }

        public static Int64 FindFirstSigunature(this IRandomInputByteStream<UInt64> inputStream, Int32 signatureLength, UInt64 offset, UInt64 count, Func<ReadOnlyMemory<byte>, Int32, bool> predicate)
        {
            var buffer = new byte[signatureLength];
            var readOnlyBuffer = buffer.AsReadOnly();
            var index = -(Int64)signatureLength;
            foreach (var data in inputStream.GetByteSequence(offset, count, true))
            {
                if (index >= 0)
                {
                    if (predicate(readOnlyBuffer, 0))
                        return index;
                }
                Array.Copy(buffer, 1, buffer, 0, buffer.Length - 1);
                buffer[^1] = data;
                ++index;
            }
            if (predicate(readOnlyBuffer, 0))
                return index;
            return -1;
        }

        public static UInt64? FindLastSigunature(this IRandomInputByteStream<UInt64> inputStream, ReadOnlyMemory<byte> signature, UInt64 offset, UInt64 count)
        {
            return inputStream.FindLastSigunature((UInt32)signature.Length, offset, count, (buffer, index) => buffer.Span.SigunatureEqual(index, signature.Span));
        }

        public static UInt64? FindLastSigunature(this IRandomInputByteStream<UInt64> inputStream, UInt32 signatureLength, UInt64 offset, UInt64 count, Func<ReadOnlyMemory<byte>, Int32, bool> predicate)
        {
            var buffer = new byte[signatureLength];
            var readOnlyBuffer = buffer.AsReadOnly();
            var index = offset + count;
            foreach (var data in inputStream.GetReverseByteSequence(offset, count, true))
            {
                if (index <= offset + count - signatureLength)
                {
                    if (predicate(readOnlyBuffer, 0))
                        return index;
                }
                Array.Copy(buffer, 0, buffer, 1, buffer.Length - 1);
                buffer[0] = data;
                --index;
            }
            if (predicate(readOnlyBuffer, 0))
                return index;
            return null;
        }

        private static bool SigunatureEqual(this ReadOnlySpan<byte> array, Int32 index, ReadOnlySpan<byte> signature)
        {
            if (signature.Length == 4)
                return array.SigunatureEqual(index, signature[0], signature[1], signature[2], signature[3]);
            else
                throw new InternalLogicalErrorException();
        }

        private static bool SigunatureEqual(this ReadOnlySpan<byte> array, Int32 index, byte signatureByte0, byte signatureByte1, byte signatureByte2, byte signatureByte3)
        {
            return
                array[index] == signatureByte0 &&
                array[index + 1] == signatureByte1 &&
                array[index + 2] == signatureByte2 &&
                array[index + 3] == signatureByte3;
        }
    }
}

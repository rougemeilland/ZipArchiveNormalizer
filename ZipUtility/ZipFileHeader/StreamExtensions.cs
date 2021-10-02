using System;
using System.IO;
using Utility.IO;

namespace ZipUtility.ZipFileHeader
{
    static class StreamExtensions
    {
#if DEBUG
        private const int maximumSignatureBufferSize = 16;
#else
private const int maximumSignatureBufferSize = 1024;
#endif

        public static long FindFirstSigunature(this IRandomInputByteStream<UInt64> inputStream, byte[] signature, ulong offset, ulong count)
        {
            return inputStream.FindFirstSigunature(signature.Length, offset, count, (buffer, index) => buffer.SigunatureEqual(index, signature));
        }

        public static long FindFirstSigunature(this IRandomInputByteStream<UInt64> inputStream, int signatureLength, ulong offset, ulong count, Func<byte[], int, bool> predicate)
        {
            var buffer = new byte[signatureLength];
            var index = (long)-signatureLength;
            foreach (var data in inputStream.GetByteSequence(offset, count, true))
            {
                if (index >= 0)
                {
                    if (predicate(buffer, 0))
                        return index;
                }
                Array.Copy(buffer, 1, buffer, 0, buffer.Length - 1);
                buffer[buffer.Length - 1] = data;
                ++index;
            }
            if (predicate(buffer, 0))
                return index;
            return -1;
        }

        public static ulong? FindLastSigunature(this IRandomInputByteStream<UInt64> inputStream, byte[] signature, ulong offset, ulong count)
        {
            return inputStream.FindLastSigunature((uint)signature.Length, offset, count, (buffer, index) => buffer.SigunatureEqual(index, signature));
        }

        public static ulong? FindLastSigunature(this IRandomInputByteStream<UInt64> inputStream, uint signatureLength, ulong offset, ulong count, Func<byte[], int, bool> predicate)
        {
            var buffer = new byte[signatureLength];
            var index = offset + count;
            foreach (var data in inputStream.GetReverseByteSequence(offset, count, true))
            {
                if (index <= offset + count - signatureLength)
                {
                    if (predicate(buffer, 0))
                        return index;
                }
                Array.Copy(buffer, 0, buffer, 1, buffer.Length - 1);
                buffer[0] = data;
                --index;
            }
            if (predicate(buffer, 0))
                return index;
            return null;
        }

        private static bool SigunatureEqual(this byte[] array, int index, byte[] signature)
        {
            if (signature.Length == 4)
                return array.SigunatureEqual(index, signature[0], signature[1], signature[2], signature[3]);
            else
                throw new Exception();
        }

        private static bool SigunatureEqual(this byte[] array, int index, byte signatureByte0, byte signatureByte1, byte signatureByte2, byte signatureByte3)
        {
            return
                array[index] == signatureByte0 &&
                array[index + 1] == signatureByte1 &&
                array[index + 2] == signatureByte2 &&
                array[index + 3] == signatureByte3;
        }
    }
}
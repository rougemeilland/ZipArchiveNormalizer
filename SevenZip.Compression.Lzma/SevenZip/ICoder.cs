// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using System;
using Utility.IO;

namespace SevenZip
{
    interface ICoder
    {
        /// <summary>
        /// Codes streams.
        /// </summary>
        /// <param name="inStream">
        /// input Stream.
        /// </param>
        /// <param name="outStream">
        /// output Stream.
        /// </param>
        /// <param name="inSize">
        /// input Size. -1 if unknown.
        /// </param>
        /// <param name="outSize">
        /// output Size. -1 if unknown.
        /// </param>
        /// <param name="progress">
        /// callback progress reference.
        /// </param>
        /// <exception cref="SevenZip.DataErrorException">
        /// if input stream is not valid
        /// </exception>
        void Code(IBasicInputByteStream inStream, IBasicOutputByteStream outStream, Int64 inSize, Int64 outSize, IProgress<UInt64> progress);
    }

    public interface IEncoderProperties
    {
        Int32 GetEncoderProperties(Span<byte> buffer);
    }
}

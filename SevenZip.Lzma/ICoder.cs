using System;
using System.IO;

namespace SevenZip
{
    public interface ICoder
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
        void Code(Stream inStream, Stream outStream, Int64 inSize, Int64 outSize, ICodeProgress progress);
    };
}

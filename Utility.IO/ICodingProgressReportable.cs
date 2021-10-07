using System;

namespace Utility.IO
{
    public interface ICodingProgressReportable
    {
        /// <summary>
        /// Report progress
        /// </summary>
        /// <param name="size">
        /// An <see cref="UInt64"/> value that indicates the length of data that could be processed.
        /// </param>
        void SetProgress(UInt64 size);
    };
}

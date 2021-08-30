using System;

namespace AutoImageTrimmer
{
    class ProgressChangedEventArgs
        : EventArgs
    {
        public ProgressChangedEventArgs(long totalCount, long countOfDone)
        {
            TotalCount = totalCount;
            CountOfDone = countOfDone;
        }

        public long TotalCount { get; }
        public long CountOfDone { get; }
    }
}

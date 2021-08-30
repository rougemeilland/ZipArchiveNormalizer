using System;

namespace Utility
{
    public class ProgressChangedEventArgs
        : EventArgs
    {
        private long _totalCount;
        private long _countOfDone;

        public ProgressChangedEventArgs()
        {
            IsCounterEnabled = false;
            _totalCount = -1;
            _countOfDone = -1;
        }

        public ProgressChangedEventArgs(long totalCount, long countOfDone)
        {
            IsCounterEnabled = true;
            _totalCount = totalCount;
            _countOfDone = countOfDone;
        }

        public bool IsCounterEnabled { get; }
        
        public long TotalCount
        {
            get
            {
                if (!IsCounterEnabled)
                    throw new InvalidOperationException();
                return _totalCount;
            }
        }

        public long CountOfDone
        {
            get
            {
                if (!IsCounterEnabled)
                    throw new InvalidOperationException();
                return _countOfDone;
            }
        }
    }
}
using System;

namespace Utility
{
    public class ProgressChangedEventArgs
        : EventArgs
    {
        private readonly UInt64? _totalCount;
        private readonly UInt64? _countOfDone;

        public ProgressChangedEventArgs()
        {
            _totalCount = null;
            _countOfDone = null;
        }

        public ProgressChangedEventArgs(UInt64 totalCount, UInt64 countOfDone)
        {
            _totalCount = totalCount;
            _countOfDone = countOfDone;
        }

        public bool IsCounterEnabled => _totalCount.HasValue && _countOfDone.HasValue;

        public UInt64 TotalCount
        {
            get
            {
                if (!_totalCount.HasValue)
                    throw new InvalidOperationException();
                return _totalCount.Value;
            }
        }

        public UInt64 CountOfDone
        {
            get
            {
                if (!_countOfDone.HasValue)
                    throw new InvalidOperationException();
                return _countOfDone.Value;
            }
        }
    }
}

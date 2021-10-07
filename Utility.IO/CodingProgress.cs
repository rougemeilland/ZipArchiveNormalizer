using System;

namespace Utility.IO
{
    public class CodingProgress
        : ICodingProgressReportable
    {
        private Action<UInt64> _action;

        public CodingProgress(Action<UInt64> action)
        {
            _action = action;
        }

        public void SetProgress(ulong size)
        {
            _action(size);
        }
    }
}

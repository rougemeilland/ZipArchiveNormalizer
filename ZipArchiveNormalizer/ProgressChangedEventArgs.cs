using System;

namespace ZipArchiveNormalizer
{
    class ProgressChangedEventArgs
        : EventArgs
    {
        public ProgressChangedEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}

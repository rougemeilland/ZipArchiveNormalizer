using System;

namespace ZipArchiveNormalizer
{
    class ReportedEventArgs
        : EventArgs
    {
        public ReportedEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

}

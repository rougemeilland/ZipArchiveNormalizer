using System;
using System.Runtime.Serialization;

namespace ZipUtility
{
    [Serializable]
    public class BadZipFileFormatException
        : Exception
    {
        public BadZipFileFormatException()
            : base("ZIPファイルのフォーマットに誤りを見つけました。")
        {
        }

        public BadZipFileFormatException(string message)
            : base(message)
        {
        }

        public BadZipFileFormatException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected BadZipFileFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

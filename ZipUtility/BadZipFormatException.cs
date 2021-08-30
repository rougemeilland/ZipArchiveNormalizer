using System;
using System.Runtime.Serialization;

namespace ZipUtility
{
    [Serializable]
    public class BadZipFormatException
        : Exception
    {
        public BadZipFormatException()
            : base("ZIPファイルのフォーマットに誤りを見つけました。")
        {
        }

        public BadZipFormatException(string message)
            : base(message)
        {
        }

        public BadZipFormatException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected BadZipFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

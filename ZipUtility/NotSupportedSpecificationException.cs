using System;
using System.Runtime.Serialization;

namespace ZipUtility
{
    [Serializable]
    public class NotSupportedSpecificationException
        : Exception
    {
        public NotSupportedSpecificationException()
            : base("Not supported function required to access the ZIP file.")
        {
        }

        public NotSupportedSpecificationException(string message)
            : base(message)
        {
        }

        public NotSupportedSpecificationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected NotSupportedSpecificationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
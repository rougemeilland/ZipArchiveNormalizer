using System;
using System.Runtime.Serialization;

namespace Utility.Text
{
    [Serializable]
    public class BadShiftJisEncodingException
        : Exception
    {
        public BadShiftJisEncodingException()
        {
        }

        public BadShiftJisEncodingException(string message)
            : base(message)
        {
        }

        public BadShiftJisEncodingException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected BadShiftJisEncodingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

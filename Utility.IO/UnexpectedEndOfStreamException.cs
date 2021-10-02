using System;
using System.Runtime.Serialization;

namespace Utility.IO
{
    [Serializable]
    public class UnexpectedEndOfStreamException
        : Exception
    {
        public UnexpectedEndOfStreamException()
            : base("Unexpectedly reached the end of the stream.")
        {
        }

        public UnexpectedEndOfStreamException(string message)
            : base(message)
        {
        }

        public UnexpectedEndOfStreamException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected UnexpectedEndOfStreamException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
using System;
using System.Runtime.Serialization;

namespace Utility.Text
{
    [Serializable]
    public class UnexpectedEndOfSequenceException
        : Exception
    {
        public UnexpectedEndOfSequenceException()
        {
        }

        public UnexpectedEndOfSequenceException(string message)
            : base(message)
        {
        }

        public UnexpectedEndOfSequenceException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected UnexpectedEndOfSequenceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

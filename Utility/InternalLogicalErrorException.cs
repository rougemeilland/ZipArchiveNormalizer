using System;
using System.Runtime.Serialization;

namespace Utility
{
    [Serializable]
    public class InternalLogicalErrorException
        : Exception
    {
        public InternalLogicalErrorException()
            : base("Detected internal logical error.")
        {
        }

        public InternalLogicalErrorException(string message)
            : base(message)
        {
        }

        public InternalLogicalErrorException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected InternalLogicalErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

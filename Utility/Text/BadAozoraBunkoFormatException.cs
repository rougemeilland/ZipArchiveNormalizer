using System;
using System.Runtime.Serialization;

namespace Utility.Text
{
    [Serializable]
    class BadAozoraBunkoFormatException
        : Exception
    {
        public BadAozoraBunkoFormatException()
        {
        }

        public BadAozoraBunkoFormatException(string message)
            : base(message)
        {
        }

        public BadAozoraBunkoFormatException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected BadAozoraBunkoFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

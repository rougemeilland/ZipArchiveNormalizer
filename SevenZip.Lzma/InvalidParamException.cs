using System;
using System.Runtime.Serialization;

namespace SevenZip
{
    /// <summary>
    /// The exception that is thrown when the value of an argument is outside the allowable range.
    /// </summary>
    [Serializable]
    class InvalidParamException : ApplicationException
    {
        public InvalidParamException() : base("Invalid Parameter")
        {
        }

        public InvalidParamException(string message)
            : base(message)
        {
        }

        public InvalidParamException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected InvalidParamException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

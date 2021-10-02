using System;
using System.Runtime.Serialization;

namespace Utility.IO.Compression
{
    /// <summary>
    /// The exception that is thrown when an error in input stream occurs during decoding.
    /// </summary>
    [Serializable]
    public class DataErrorException : ApplicationException
    {
        public DataErrorException()
            : base("Data Error")
        {
        }

        public DataErrorException(string message)
            : base(message)
        {
        }

        public DataErrorException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected DataErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

using System;
using System.Runtime.Serialization;

namespace ZipUtility.IO
{
    [Serializable]
    public class DataErrorException
        : Exception
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

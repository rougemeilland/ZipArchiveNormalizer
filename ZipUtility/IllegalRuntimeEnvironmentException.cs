using System;
using System.Runtime.Serialization;

namespace ZipUtility
{
    [Serializable]
    public class IllegalRuntimeEnvironmentException
        : Exception
    {
        public IllegalRuntimeEnvironmentException()
            : this("There is an error in the runtime environment.")
        {
        }

        public IllegalRuntimeEnvironmentException(string message)
            : base(message)
        {
        }

        public IllegalRuntimeEnvironmentException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected IllegalRuntimeEnvironmentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

#if false
using System;
using System.Runtime.Serialization;

namespace ZipUtility.Compression.Lzma.DataStream
{
    [Serializable]
    class InternalLzmaException
        : Exception
    {
        public InternalLzmaException()
            : base("Internal LAMA error occured.")
        {
        }

        public InternalLzmaException(string message)
            : base(message)
        {
        }

        public InternalLzmaException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InternalLzmaException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
#endif

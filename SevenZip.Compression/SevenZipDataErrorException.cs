// based on LZMA SDK 19.00 (Copyright (C) 2019 Igor Pavlov., public domain)

using System;
using System.Runtime.Serialization;

namespace SevenZip
{
    /// <summary>
    /// The exception that is thrown when an error in input stream occurs during decoding.
    /// </summary>
    [Serializable]
    public class SevenZipDataErrorException : ApplicationException
    {
        public SevenZipDataErrorException()
            : base("Data Error")
        {
        }

        public SevenZipDataErrorException(string message)
            : base(message)
        {
        }

        public SevenZipDataErrorException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected SevenZipDataErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

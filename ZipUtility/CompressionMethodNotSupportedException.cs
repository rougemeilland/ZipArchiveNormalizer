using System;
using System.Runtime.Serialization;

namespace ZipUtility
{
    [Serializable]
    public class CompressionMethodNotSupportedException
        : NotSupportedSpecificationException
    {
        public CompressionMethodNotSupportedException(ZipEntryCompressionMethodId compresssionMethodId)
            : base(string.Format("Compression method '{0}' is not supported.", compresssionMethodId))
        {
            CompresssionMethodId = compresssionMethodId;
        }

        public CompressionMethodNotSupportedException(string message, ZipEntryCompressionMethodId compresssionMethodId)
            : base(message)
        {
            CompresssionMethodId = compresssionMethodId;
        }

        public CompressionMethodNotSupportedException(string message, Exception inner, ZipEntryCompressionMethodId compresssionMethodId)
            : base(message, inner)
        {
            CompresssionMethodId = compresssionMethodId;
        }

        protected CompressionMethodNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            CompresssionMethodId = (ZipEntryCompressionMethodId)info.GetUInt16("CompresssionMethodId");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("CompresssionMethodId", (UInt16)CompresssionMethodId);
        }

        public ZipEntryCompressionMethodId CompresssionMethodId { get; }
    }
}
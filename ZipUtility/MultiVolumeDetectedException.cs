using System;
using System.Runtime.Serialization;

namespace ZipUtility
{
    [Serializable]
    public class MultiVolumeDetectedException
        : Exception
    {
        public MultiVolumeDetectedException(UInt32 lastDiskNumber)
            : base(string.Format("Detected Multi-Volume ZIP file, but not supported in the stream. : disk count = {0}", lastDiskNumber))
        {
            LastDiskNumber = lastDiskNumber;
        }

        public MultiVolumeDetectedException(string message, UInt32 lastDiskNumber)
            : base(message)
        {
            LastDiskNumber = lastDiskNumber;
        }

        public MultiVolumeDetectedException(string message, UInt32 lastDiskNumber, Exception inner)
            : base(message, inner)
        {
            LastDiskNumber = lastDiskNumber;
        }

        protected MultiVolumeDetectedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            LastDiskNumber = info.GetUInt32(nameof(LastDiskNumber));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(LastDiskNumber), LastDiskNumber);
        }

        public UInt32 LastDiskNumber { get; }
    }
}

using System;
using System.IO;

namespace Utility.IO
{
    public class PartialOutputStream
        : OutputStream
    {
        private long? _offset;

        public PartialOutputStream(Stream baseStream, long? offset, long? size, bool leaveOpen = false)
            : base(baseStream, offset, size, leaveOpen)
        {
            if (offset.HasValue && baseStream.CanSeek == false)
                throw new NotSupportedException();

            _offset = offset;
            SetDestinationStream(baseStream);
        }

        protected override void WriteToDestinationStream(Stream destinationStream, byte[] buffer, int offset, int count)
        {
            if (_offset.HasValue)
                destinationStream.Seek(_offset.Value + Position, SeekOrigin.Begin);
            destinationStream.Write(buffer, offset, count);
        }

        protected override void FlushDestinationStream(Stream destinationStream, bool isEndOfData)
        {
            destinationStream.Flush();
        }
    }
}
using System;
using System.IO;

namespace Utility.IO
{
    public class PartialInputStream
        : InputStream
    {
        private long? _offset;
        private long _size;

        public PartialInputStream(Stream baseStream, long? offset, long size, bool leaveOpen = false)
            : base(baseStream, offset, size, size, leaveOpen)
        {
            if (offset.HasValue)
            {
                if (baseStream.CanSeek == false)
                    throw new NotSupportedException();
                if (offset.Value + size > baseStream.Length)
                    throw new ArgumentException();
            }

            _offset = offset;
            _size = size;
            SetSourceStream(baseStream);
        }

        protected override int ReadFromSourceStream(Stream sourceStream, byte[] buffer, int offset, int count)
        {
            var actualCount = _size - Position;
            if (actualCount > count)
                actualCount = count;
            if (actualCount <= 0)
                return 0;
            if (_offset.HasValue)
                sourceStream.Seek(_offset.Value + Position, SeekOrigin.Begin);
            return sourceStream.Read(buffer, offset, (int)actualCount);
        }
    }
}

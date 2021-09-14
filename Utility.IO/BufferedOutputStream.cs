using System;
using System.IO;

namespace Utility.IO
{
    public class BufferedOutputStream
        : Utility.IO.BufferedStream
    {
        private byte[] _internalBuffer;
        private int _internalBufferIndex;

        public BufferedOutputStream(Stream baseStream, bool leaveOpen = false)
            : base(baseStream, leaveOpen)
        {
            Initialize();
        }

        public BufferedOutputStream(Stream baseStream, int bufferSize, bool leaveOpen = false)
            : base(baseStream, bufferSize, leaveOpen)
        {
            Initialize();
        }

        public override bool CanWrite => true;

        protected override void InternalWrite(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                if (_internalBufferIndex >= _internalBuffer.Length)
                {
                    WriteToDestination(_internalBuffer, 0, _internalBuffer.Length);
                    _internalBufferIndex = 0;
                }
                var actualCount = _internalBuffer.Length - _internalBufferIndex;
                if (actualCount > count)
                    actualCount = count;
                Array.Copy(buffer, offset, _internalBuffer, _internalBufferIndex, actualCount);
                offset += actualCount;
                count -= actualCount;
                _internalBufferIndex += actualCount;
            }
        }

        protected override void InternalFlush()
        {
            if (_internalBufferIndex > 0)
            {
                WriteToDestination(_internalBuffer, 0, _internalBufferIndex);
                _internalBufferIndex = 0;
            }
        }

        private void Initialize()
        {
            _internalBuffer = new byte[BufferSize];
            _internalBufferIndex = 0;
        }
    }
}
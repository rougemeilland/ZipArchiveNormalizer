using System;
using System.IO;

namespace Utility.IO
{
    public class BufferedInputStream
        : Utility.IO.BufferedStream
    {
        private byte[] _internalBuffer;
        private int _internalBufferCount;
        private int _internalBufferIndex;
        private bool _isEndOfStream;

        public BufferedInputStream(Stream baseStream, bool leaveOpen = false)
            : base(baseStream, leaveOpen)
        {
            Initialize();
        }

        public BufferedInputStream(Stream baseStream, int bufferSize, bool leaveOpen = false)
            : base(baseStream, bufferSize, leaveOpen)
        {
            Initialize();
        }

        public override bool CanRead => true;

        protected override int InternalRead(byte[] buffer, int offset, int count)
        {
            if (_isEndOfStream)
                return 0;

            if (_internalBufferIndex >= _internalBufferCount)
            {
                _internalBufferCount = ReadFromSource(_internalBuffer, 0, _internalBuffer.Length);
                if (_internalBufferCount <= 0)
                {
                    _isEndOfStream = true;
                    return _internalBufferCount;
                }
                _internalBufferIndex = 0;
            }
            var actualCount = _internalBufferCount - _internalBufferIndex;
            if (actualCount > count)
                actualCount = count;
            Array.Copy(_internalBuffer, _internalBufferIndex, buffer, offset, actualCount);
            _internalBufferIndex += actualCount;
            return actualCount;
        }

        private void Initialize()
        {
            _internalBuffer = new byte[BufferSize];
            _internalBufferCount = 0;
            _internalBufferIndex = 0;
            _isEndOfStream = false;
        }
    }
}
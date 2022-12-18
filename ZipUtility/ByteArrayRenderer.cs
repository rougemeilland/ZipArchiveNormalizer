using System;
using Utility;

namespace ZipUtility
{
    class ByteArrayRenderer
    {
        private readonly byte[] _destinationArray;
        private Int32 _currentIndex;

        public ByteArrayRenderer()
        {
            _destinationArray = new byte[UInt16.MaxValue];
            _currentIndex = 0;
        }

        public void WriteByte(Byte value)
        {
            const Int32 valueLength = sizeof(Byte);
            if (_currentIndex + valueLength > _destinationArray.Length)
                throw new IndexOutOfRangeException();
            _destinationArray.CopyValueLE(_currentIndex, value);
            _currentIndex += valueLength;
        }

        public void WriteBytes(ReadOnlyMemory<Byte> value)
        {
            if (_currentIndex + value.Length > _destinationArray.Length)
                throw new IndexOutOfRangeException();
            value.CopyTo(_destinationArray.Slice(_currentIndex));
            _currentIndex += value.Length;
        }

        public void WriteInt16LE(Int16 value)
        {
            const Int32 valueLength = sizeof(Int16);
            if (_currentIndex + valueLength > _destinationArray.Length)
                throw new IndexOutOfRangeException();
            _destinationArray.CopyValueLE(_currentIndex, value);
            _currentIndex += valueLength;
        }

        public void WriteUInt16LE(UInt16 value)
        {
            const Int32 valueLength = sizeof(UInt16);
            if (_currentIndex + valueLength > _destinationArray.Length)
                throw new IndexOutOfRangeException();
            _destinationArray.CopyValueLE(_currentIndex, value);
            _currentIndex += valueLength;
        }

        public void WriteInt32LE(Int32 value)
        {
            const Int32 valueLength = sizeof(Int32);
            if (_currentIndex + valueLength > _destinationArray.Length)
                throw new IndexOutOfRangeException();
            _destinationArray.CopyValueLE(_currentIndex, value);
            _currentIndex += valueLength;
        }

        public void WriteUInt32LE(UInt32 value)
        {
            const Int32 valueLength = sizeof(UInt32);
            if (_currentIndex + valueLength > _destinationArray.Length)
                throw new IndexOutOfRangeException();
            _destinationArray.CopyValueLE(_currentIndex, value);
            _currentIndex += valueLength;
        }

        public void WriteInt64LE(Int64 value)
        {
            const Int32 valueLength = sizeof(Int64);
            if (_currentIndex + valueLength > _destinationArray.Length)
                throw new IndexOutOfRangeException();
            _destinationArray.CopyValueLE(_currentIndex, value);
            _currentIndex += valueLength;
        }

        public void WriteUInt64LE(UInt64 value)
        {
            const Int32 valueLength = sizeof(UInt64);
            if (_currentIndex + valueLength > _destinationArray.Length)
                throw new IndexOutOfRangeException();
            _destinationArray.CopyValueLE(_currentIndex, value);
            _currentIndex += valueLength;
        }

        public byte[] ToByteArray()
        {
            var buffer = new byte[_currentIndex];
            _destinationArray.CopyTo(0, buffer, 0, _currentIndex);
            return buffer;
        }
    }
}

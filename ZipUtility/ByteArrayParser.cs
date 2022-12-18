using System;
using Utility;
using Utility.IO;

namespace ZipUtility
{
    class ByteArrayParser
    {
        private readonly ReadOnlyMemory<byte> _sourceArray;
        private Int32 _currentIndex;

        public ByteArrayParser(ReadOnlyMemory<byte> sourceArray)
        {
            _sourceArray = sourceArray;
            _currentIndex = 0;
        }

        public bool IsEmpty => _currentIndex >= _sourceArray.Length;

        public Byte ReadByte()
        {
            const Int32 valueLength = sizeof(Byte);
            if (_currentIndex + valueLength > _sourceArray.Length)
                throw new UnexpectedEndOfStreamException();
            var value = _sourceArray.Span[0];
            _currentIndex += valueLength;
            return value;
        }

        public Byte[] ReadBytes(Int32 length)
        {
            if (_currentIndex + length > _sourceArray.Length)
                throw new UnexpectedEndOfStreamException();
            var value = new Byte[length];
            _sourceArray.Slice(_currentIndex, value.Length).CopyTo(value);
            _currentIndex += length;
            return value;
        }

        public Byte[] ReadAllBytes()
        {
            var value = new Byte[_sourceArray.Length - _currentIndex];
            _sourceArray[_currentIndex..].CopyTo(value);
            _currentIndex = _sourceArray.Length;
            return value;
        }

        public Int16 ReadInt16LE()
        {
            const Int32 valueLength = sizeof(Int16);
            if (_currentIndex + valueLength > _sourceArray.Length)
                throw new UnexpectedEndOfStreamException();
            var value = _sourceArray[_currentIndex..].ToInt16LE();
            _currentIndex += valueLength;
            return value;
        }

        public UInt16 ReadUInt16LE()
        {
            const Int32 valueLength = sizeof(UInt16);
            if (_currentIndex + valueLength > _sourceArray.Length)
                throw new UnexpectedEndOfStreamException();
            var value = _sourceArray[_currentIndex..].ToUInt16LE();
            _currentIndex += valueLength;
            return value;
        }

        public Int32 ReadInt32LE()
        {
            const Int32 valueLength = sizeof(Int32);
            if (_currentIndex + valueLength > _sourceArray.Length)
                throw new UnexpectedEndOfStreamException();
            var value = _sourceArray[_currentIndex..].ToInt32LE();
            _currentIndex += valueLength;
            return value;
        }

        public UInt32 ReadUInt32LE()
        {
            const Int32 valueLength = sizeof(UInt32);
            if (_currentIndex + valueLength > _sourceArray.Length)
                throw new UnexpectedEndOfStreamException();
            var value = _sourceArray[_currentIndex..].ToUInt32LE();
            _currentIndex += valueLength;
            return value;
        }

        public Int64 ReadInt64LE()
        {
            const Int32 valueLength = sizeof(Int64);
            if (_currentIndex + valueLength > _sourceArray.Length)
                throw new UnexpectedEndOfStreamException();
            var value = _sourceArray[_currentIndex..].ToInt64LE();
            _currentIndex += valueLength;
            return value;
        }

        public UInt64 ReadUInt64LE()
        {
            const Int32 valueLength = sizeof(UInt64);
            if (_currentIndex + valueLength > _sourceArray.Length)
                throw new UnexpectedEndOfStreamException();
            var value = _sourceArray[_currentIndex..].ToUInt64LE();
            _currentIndex += valueLength;
            return value;
        }
    }
}

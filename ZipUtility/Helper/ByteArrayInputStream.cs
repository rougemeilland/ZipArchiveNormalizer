using System;
using System.Collections.Generic;
using System.Linq;
using Utility;

namespace ZipUtility.Helper
{
    class ByteArrayInputStream
    {
        private IReadOnlyArray<byte> _source;
        private int _index;
        private int _length;

        public ByteArrayInputStream(IEnumerable<byte> source)
            : this(source.ToArray().AsReadOnly())
        {
        }

        public ByteArrayInputStream(IReadOnlyArray<byte> source)
            : this(source, 0, source.Length)
        {
        }

        public ByteArrayInputStream(byte[] source, int offset, int count)
            : this(source.AsReadOnly(), offset, count)
        {
        }

        public ByteArrayInputStream(IReadOnlyArray<byte> source, int offset, int count)
        {
            if (source == null)
                throw new UnexpectedEndOfStreamException();
            _source = source;
            _index = offset;
            _length = offset + count;
        }

        public byte ReadByte()
        {
            if (_index + 1 > _length)
                throw new UnexpectedEndOfStreamException();
            var value = _source[_index];
            ++_index;
            return value;
        }

        public byte[] ReadBytes(UInt16 count)
        {
            if (_index + count > _length)
                throw new UnexpectedEndOfStreamException();
            var buffer = new byte[count];
            if (count > 0)
            {
                _source.CopyTo(_index, buffer, 0, buffer.Length);
                _index += count;
            }
            return buffer;
        }

        public UInt16 ReadUInt16LE()
        {
            if (_index + 2 > _length)
                throw new UnexpectedEndOfStreamException();
            var value = _source.ToUInt16LE(_index);
            _index += 2;
            return value;
        }

        public UInt32 ReadUInt32LE()
        {
            if (_index + 4 > _length)
                throw new UnexpectedEndOfStreamException();
            var value = _source.ToUInt32LE(_index);
            _index += 4;
            return value;
        }

        public Int32 ReadInt32LE()
        {
            if (_index + 4 > _length)
                throw new UnexpectedEndOfStreamException();
            var value = _source.ToInt32LE(_index);
            _index += 4;
            return value;
        }

        public UInt64 ReadUInt64LE()
        {
            if (_index + 8 > _length)
                throw new UnexpectedEndOfStreamException();
            var value = _source.ToUInt64LE(_index);
            _index += 8;
            return value;
        }

        public Int64 ReadInt64LE()
        {
            if (_index + 8 > _length)
                throw new UnexpectedEndOfStreamException();
            var value = _source.ToInt64LE(_index);
            _index += 8;
            return value;
        }

        public byte[] ReadToEnd()
        {
            if (_index > _length)
                throw new UnexpectedEndOfStreamException();
            var value = new byte[_length - _index];
            _source.CopyTo(_index, value, 0, value.Length);
            _index += value.Length;
            return value;
        }

        public bool IsEndOfStream()
        {
            return _index >= _length;
        }
    }
}

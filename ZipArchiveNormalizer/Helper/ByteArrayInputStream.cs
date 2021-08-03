using System;

namespace ZipArchiveNormalizer.Helper
{
    class ByteArrayInputStream
    {
        private byte[] _source;
        private int _index;
        private int _length;

        public ByteArrayInputStream(byte[] source)
            : this(source, 0, source.Length)
        {
        }

        public ByteArrayInputStream(byte[] source, int offset, int count)
        {
            if (source == null)
                throw new ArgumentNullException();
            _source = source; ;
            _index = offset;
            _length = offset + count;
        }

        public byte ReadByte()
        {
            if (_index + 1 > _length)
                throw new IndexOutOfRangeException();
            var value = _source[_index];
            ++_index;
            return value;
        }

        public UInt16 ReadUInt16LE()
        {
            if (_index + 2 > _length)
                throw new IndexOutOfRangeException();
            var value = BitConverter.ToUInt16(_source, _index);
            _index += 2;
            return value;
        }

        public UInt32 ReadUInt32LE()
        {
            if (_index + 4 > _length)
                throw new IndexOutOfRangeException();
            var value = BitConverter.ToUInt32(_source, _index);
            _index += 4;
            return value;
        }

        public UInt64 ReadUInt64LE()
        {
            if (_index + 8 > _length)
                throw new IndexOutOfRangeException();
            var value = BitConverter.ToUInt64(_source, _index);
            _index += 8;
            return value;
        }

        public byte[] ReadToEnd()
        {
            if (_index > _length)
                throw new IndexOutOfRangeException();
            var value = new byte[_length - _index];
            Array.Copy(_source, _index, value, 0, value.Length);
            _index += value.Length;
            return value;
        }
    }
}

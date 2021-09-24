using System;
using System.Collections.Generic;
using System.Linq;
using Utility;

namespace ZipUtility.Helper
{
    class ByteArrayOutputStream
    {
        private IEnumerable<byte> _destination;

        public ByteArrayOutputStream()
        {
            _destination = new byte[0];
        }

        public void WriteByte(byte data)
        {
            _destination = _destination.Concat(new[] { data });
        }

        public void WriteUInt16LE(UInt16 data)
        {
            _destination = _destination.Concat(data.GetBytesLE());
        }

        public void WriteUInt32LE(UInt32 data)
        {
            _destination = _destination.Concat(data.GetBytesLE());
        }

        public void WriteInt32LE(Int32 data)
        {
            _destination = _destination.Concat(data.GetBytesLE());
        }

        public void WriteUInt64LE(UInt64 data)
        {
            _destination = _destination.Concat(data.GetBytesLE());
        }

        public void WriteInt64LE(Int64 data)
        {
            _destination = _destination.Concat(data.GetBytesLE());
        }

        public void WriteBytes(byte[] data)
        {
            _destination = _destination.Concat(data);
        }

        public void WriteBytes(IReadOnlyArray<byte> data)
        {
            _destination = _destination.Concat(data);
        }

        public IReadOnlyArray<byte> ToByteArray()
        {
            return _destination.ToArray().AsReadOnly();
        }
    }
}

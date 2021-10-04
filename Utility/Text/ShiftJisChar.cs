using System;
using System.Text;

namespace Utility.Text
{
    public struct ShiftJisChar
        : IEquatable<ShiftJisChar>, IComparable<ShiftJisChar>, IEquatable<UInt16>, IComparable<UInt16>, IEquatable<int>, IComparable<int>
    {
        private static Encoding _shiftJisEncoding;
        private byte _data1;
        private byte _data2;

        static ShiftJisChar()
        {
            _shiftJisEncoding = Encoding.GetEncoding("shift_jis");
        }

        public ShiftJisChar(byte data)
        {
            _data1 = data;
            _data2 = 0;
        }

        public ShiftJisChar(byte data1, byte data2)
        {
            _data1 = data1;
            _data2 = data2;
        }

        public int CompareTo(UInt16 other) => InternalCode.CompareTo(other);
        public int CompareTo(int other) => ((int)InternalCode).CompareTo(other);
        public int CompareTo(ShiftJisChar other) => InternalCode.CompareTo(other.InternalCode);
        public bool Equals(UInt16 other) => InternalCode == other;
        public bool Equals(int other) => InternalCode == other;
        public bool Equals(ShiftJisChar other) => _data1 == other._data1 && _data2 == other._data2;
        public override bool Equals(object obj) => obj != null && GetType() == obj.GetType() && Equals((ShiftJisChar)obj);
        public override int GetHashCode() => _data1.GetHashCode() ^ _data2.GetHashCode();

        public override string ToString()
        {
            var s = _shiftJisEncoding.GetString(ToByteArray());
            return
                _data2 == 0
                ? string.Format("\"{0}\"(0x{1:x2})", s, _data1)
                : string.Format("\"{0}\"(0x{1:x2}{2:x2})", s, _data1, _data2);
        }

        internal IReadOnlyArray<byte> ToByteArray()
        {
            if (_data2 == 0)
                return new[] { _data1 }.AsReadOnly();
            else
                return new[] { _data1, _data2 }.AsReadOnly();
        }

        private UInt16 InternalCode => _data2 == 0 ? (UInt16)_data1 : (UInt16)((_data1 << 8) | (_data2 << 0));
    }
}

#define OMIT_SOURCE_ARRAY_CHECK
using System;

namespace Utility
{
    public readonly struct ReadOnlyArrayPointer<ELEMENT_T>
        : IEquatable<ArrayPointer<ELEMENT_T>>, IComparable<ArrayPointer<ELEMENT_T>>, IEquatable<ReadOnlyArrayPointer<ELEMENT_T>>, IComparable<ReadOnlyArrayPointer<ELEMENT_T>>
    {
        private readonly ELEMENT_T[] _sourceArray;
        private readonly Int32 _currentIndex;

        [Obsolete("Do not call constructor", true)]
        public ReadOnlyArrayPointer()
        {
            throw new NotSupportedException();
        }

        internal ReadOnlyArrayPointer(ELEMENT_T[] sourceArray, Int32 currentIndex)
        {
            _sourceArray = sourceArray;
            _currentIndex = currentIndex;
#if DEBUG
            if (_currentIndex < 0)
                throw new Exception();
            if (_currentIndex > _sourceArray.Length)
                throw new Exception();
#endif
        }

        public ReadOnlyMemory<ELEMENT_T> GetMemory(Int32 length)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            if (!length.IsBetween(0, _sourceArray.Length))
                throw new ArgumentOutOfRangeException(nameof(length));

            return new ReadOnlyMemory<ELEMENT_T>(_sourceArray, _currentIndex, length);
        }

        public ReadOnlyMemory<ELEMENT_T> GetRegion(UInt32 length)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            if (length > (UInt32)_sourceArray.Length)
                throw new ArgumentOutOfRangeException(nameof(length));
#if DEBUG
            if (length > Int32.MaxValue)
                throw new Exception();
#endif

            return new ReadOnlyMemory<ELEMENT_T>(_sourceArray, _currentIndex, (Int32)length);
        }

        public ReadOnlySpan<ELEMENT_T> GetSpan(Int32 length)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            if (!length.IsBetween(0, _sourceArray.Length))
                throw new ArgumentOutOfRangeException(nameof(length));

            return new ReadOnlySpan<ELEMENT_T>(_sourceArray, _currentIndex, length);
        }

        public ReadOnlySpan<ELEMENT_T> GetSpan(UInt32 length)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            if (length > (UInt32)_sourceArray.Length)
                throw new ArgumentOutOfRangeException(nameof(length));
#if DEBUG
            if (length > Int32.MaxValue)
                throw new Exception();
#endif

            return new ReadOnlySpan<ELEMENT_T>(_sourceArray, _currentIndex, (Int32)length);
        }

        public ELEMENT_T this[Int32 index]
        {
            get
            {
#if !OMIT_SOURCE_ARRAY_CHECK
                if (_sourceArray is null)
                    throw new InvalidOperationException();
#endif
                var offset = checked(_currentIndex + index);
                if (!offset.InRange(0, _sourceArray.Length))
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _sourceArray[offset];
            }
        }

        public ELEMENT_T this[UInt32 index]
        {
            get
            {
#if !OMIT_SOURCE_ARRAY_CHECK
                if (_sourceArray is null)
                    throw new InvalidOperationException();
#endif
                var offset = checked((UInt32)_currentIndex + index);
                if (offset >= _sourceArray.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
#if DEBUG
                if (offset > Int32.MaxValue)
                    throw new Exception();
#endif

                return _sourceArray[(Int32)offset];
            }
        }

        public ReadOnlyArrayPointer<ELEMENT_T> Add(Int32 offset)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            var newIndex = checked(_currentIndex + offset);
            if (!newIndex.IsBetween(0, _sourceArray.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return new ReadOnlyArrayPointer<ELEMENT_T>(_sourceArray, newIndex);
        }

        public ReadOnlyArrayPointer<ELEMENT_T> Add(UInt32 offset)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            var newIndex = checked((UInt32)_currentIndex + offset);
            if (newIndex > (UInt32)_sourceArray.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return new ReadOnlyArrayPointer<ELEMENT_T>(_sourceArray, (Int32)newIndex);
        }

        public ReadOnlyArrayPointer<ELEMENT_T> Subtract(Int32 offset)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            var newIndex = checked(_currentIndex - offset);
            if (!newIndex.IsBetween(0, _sourceArray.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return new ReadOnlyArrayPointer<ELEMENT_T>(_sourceArray, newIndex);
        }

        public ReadOnlyArrayPointer<ELEMENT_T> Subtract(UInt32 offset)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            var newIndex = checked((Int32)(_currentIndex - offset));
            if (!newIndex.IsBetween(0, _sourceArray.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return new ReadOnlyArrayPointer<ELEMENT_T>(_sourceArray, newIndex);
        }

        public Int32 Subtract(ArrayPointer<ELEMENT_T> other)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            if (!ReferenceEquals(_sourceArray, other.SourceArray))
                throw new ArgumentException($"'this' and {nameof(other)} point to different arrays.");

            checked
            {
                return _currentIndex - other.CurrentIndex;
            }
        }

        public Int32 Subtract(ReadOnlyArrayPointer<ELEMENT_T> other)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            if (!ReferenceEquals(_sourceArray, other.SourceArray))
                throw new ArgumentException($"'this' and {nameof(other)} point to different arrays.");

            checked
            {
                return _currentIndex - other.CurrentIndex;
            }
        }

        public Int32 CompareTo(ArrayPointer<ELEMENT_T> other)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            if (!ReferenceEquals(_sourceArray, other.SourceArray))
                throw new ArgumentException($"'this' and {nameof(other)} point to different arrays.");

            return _currentIndex.CompareTo(other.CurrentIndex);
        }

        public Int32 CompareTo(ReadOnlyArrayPointer<ELEMENT_T> other)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            if (!ReferenceEquals(_sourceArray, other.SourceArray))
                throw new ArgumentException($"'this' and {nameof(other)} point to different arrays.");

            return _currentIndex.CompareTo(other.CurrentIndex);
        }

        public bool Equals(ArrayPointer<ELEMENT_T> other)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            if (!ReferenceEquals(_sourceArray, other.SourceArray))
                throw new ArgumentException($"'this' and {nameof(other)} point to different arrays.");

            return _currentIndex == other.CurrentIndex;
        }

        public bool Equals(ReadOnlyArrayPointer<ELEMENT_T> other)
        {
#if !OMIT_SOURCE_ARRAY_CHECK
            if (_sourceArray is null)
                throw new InvalidOperationException();
#endif
            if (!ReferenceEquals(_sourceArray, other.SourceArray))
                throw new ArgumentException($"'this' and {nameof(other)} point to different arrays.");

            return _currentIndex == other.CurrentIndex;
        }

        public override bool Equals(object? obj) =>
            obj is not null && GetType() == obj.GetType() && Equals((ReadOnlyArrayPointer<ELEMENT_T>)obj);

        public override Int32 GetHashCode() => _currentIndex.GetHashCode();

        public static ReadOnlyArrayPointer<ELEMENT_T> operator +(ReadOnlyArrayPointer<ELEMENT_T> p, Int32 offset) =>
            p.Add(offset);

        public static ReadOnlyArrayPointer<ELEMENT_T> operator +(ReadOnlyArrayPointer<ELEMENT_T> p, UInt32 offset) =>
            p.Add(offset);

        public static ReadOnlyArrayPointer<ELEMENT_T> operator -(ReadOnlyArrayPointer<ELEMENT_T> p, Int32 offset) =>
            p.Subtract(offset);

        public static ReadOnlyArrayPointer<ELEMENT_T> operator -(ReadOnlyArrayPointer<ELEMENT_T> p, UInt32 offset) =>
            p.Subtract(offset);

        public static Int32 operator -(ReadOnlyArrayPointer<ELEMENT_T> p1, ArrayPointer<ELEMENT_T> p2) =>
            p1.Subtract(p2);

        public static Int32 operator -(ReadOnlyArrayPointer<ELEMENT_T> p1, ReadOnlyArrayPointer<ELEMENT_T> p2) =>
            p1.Subtract(p2);

        public static ReadOnlyArrayPointer<ELEMENT_T> operator ++(ReadOnlyArrayPointer<ELEMENT_T> p) =>
            p.Add(1);

        public static ReadOnlyArrayPointer<ELEMENT_T> operator --(ReadOnlyArrayPointer<ELEMENT_T> p) =>
            p.Subtract(1);

        public static bool operator ==(ReadOnlyArrayPointer<ELEMENT_T> p1, ArrayPointer<ELEMENT_T> p2) =>
            p1.Equals(p2);

        public static bool operator ==(ReadOnlyArrayPointer<ELEMENT_T> p1, ReadOnlyArrayPointer<ELEMENT_T> p2) =>
            p1.Equals(p2);

        public static bool operator !=(ReadOnlyArrayPointer<ELEMENT_T> p1, ArrayPointer<ELEMENT_T> p2) =>
            !p1.Equals(p2);

        public static bool operator !=(ReadOnlyArrayPointer<ELEMENT_T> p1, ReadOnlyArrayPointer<ELEMENT_T> p2) =>
            !p1.Equals(p2);

        public static bool operator >(ReadOnlyArrayPointer<ELEMENT_T> p1, ArrayPointer<ELEMENT_T> p2) =>
            p1.CompareTo(p2) > 0;

        public static bool operator >(ReadOnlyArrayPointer<ELEMENT_T> p1, ReadOnlyArrayPointer<ELEMENT_T> p2) =>
            p1.CompareTo(p2) > 0;

        public static bool operator >=(ReadOnlyArrayPointer<ELEMENT_T> p1, ArrayPointer<ELEMENT_T> p2) =>
            p1.CompareTo(p2) >= 0;

        public static bool operator >=(ReadOnlyArrayPointer<ELEMENT_T> p1, ReadOnlyArrayPointer<ELEMENT_T> p2) =>
            p1.CompareTo(p2) >= 0;

        public static bool operator <(ReadOnlyArrayPointer<ELEMENT_T> p1, ArrayPointer<ELEMENT_T> p2) =>
            p1.CompareTo(p2) < 0;

        public static bool operator <(ReadOnlyArrayPointer<ELEMENT_T> p1, ReadOnlyArrayPointer<ELEMENT_T> p2) =>
            p1.CompareTo(p2) < 0;

        public static bool operator <=(ReadOnlyArrayPointer<ELEMENT_T> p1, ArrayPointer<ELEMENT_T> p2) =>
            p1.CompareTo(p2) <= 0;

        public static bool operator <=(ReadOnlyArrayPointer<ELEMENT_T> p1, ReadOnlyArrayPointer<ELEMENT_T> p2) =>
            p1.CompareTo(p2) <= 0;

        internal ELEMENT_T[] SourceArray
        {
            get
            {
#if !OMIT_SOURCE_ARRAY_CHECK
                if (_sourceArray is null)
                    throw new InvalidOperationException();
#endif

                return _sourceArray;
            }
        }

        internal Int32 CurrentIndex
        {
            get
            {
#if !OMIT_SOURCE_ARRAY_CHECK
                if (_sourceArray is null)
                    throw new InvalidOperationException();
#endif

                return _currentIndex;
            }
        }
    }
}

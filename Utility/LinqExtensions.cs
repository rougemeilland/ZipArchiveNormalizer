using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utility
{
    public static class LinqExtensions
    {
        private class ArrayEnumerable<ELEMENT_T>
            : IEnumerable<ELEMENT_T>
        {
            private class Enumerator
                : IEnumerator<ELEMENT_T>
            {
                private IReadOnlyArray<ELEMENT_T> _buffer;
                private int _offset;
                private int _index;
                private int _limit;

                public Enumerator(IReadOnlyArray<ELEMENT_T> buffer, int offset, int count)
                {
                    if (offset < 0)
                        throw new ArgumentException();
                    if (count < 0)
                        throw new ArgumentException();
                    if (offset + count > buffer.Length)
                        throw new ArgumentException();
                    _buffer = buffer;
                    _offset = offset;
                    _index = offset - 1;
                    _limit = offset + count;
                }

                public ELEMENT_T Current => _index >= _offset && _index < _limit ? _buffer[_index] : throw new InvalidOperationException();

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    // NOP
                }

                public bool MoveNext()
                {
                    if (_index >= _limit)
                        throw new InvalidOperationException();
                    ++_index;
                    return _index < _limit;
                }

                public void Reset()
                {
                    _index = _offset - 1;
                }
            }

            private IReadOnlyArray<ELEMENT_T> _buffer;
            private int _offset;
            private int _count;

            public ArrayEnumerable(IReadOnlyArray<ELEMENT_T> buffer, int offset, int count)
            {
                _buffer = buffer;
                _offset = offset;
                _count = count;
            }

            public IEnumerator<ELEMENT_T> GetEnumerator()
            {
                return new Enumerator(_buffer, _offset, _count);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class ReadOnlyArrayWrapper<ELEMENT_T>
            : IReadOnlyArray<ELEMENT_T>
        {
            private ELEMENT_T[] _internalArray;

            public ReadOnlyArrayWrapper(ELEMENT_T[] sourceArray)
            {
                _internalArray = sourceArray;
            }

            public ELEMENT_T this[int index] => _internalArray[index];

            public int Length => _internalArray.Length;

            public void CopyTo(ELEMENT_T[] array, int index) => _internalArray.CopyTo(array, index);

            public void CopyTo(int sourceIndex, ELEMENT_T[] destinationArray, int destinationOffset, int count) =>
                Array.Copy(_internalArray, sourceIndex, destinationArray, destinationOffset, count);

            public IEnumerator<ELEMENT_T> GetEnumerator() => _internalArray.Cast<ELEMENT_T>().GetEnumerator();

            public ELEMENT_T[] ToArray()
            {
                var destinationArray = new ELEMENT_T[_internalArray.Length];
                Array.Copy(_internalArray, destinationArray, destinationArray.Length);
                return destinationArray;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class ReadOnlyCollectionWrapper<ELEMENT_T>
            : IReadOnlyCollection<ELEMENT_T>
        {
            private ICollection<ELEMENT_T> _internalCollection;

            public ReadOnlyCollectionWrapper(ICollection<ELEMENT_T> sourceCollection)
            {
                if (sourceCollection == null)
                    throw new ArgumentNullException();
                _internalCollection = sourceCollection;
            }

            public int Count => _internalCollection.Count;

            public IEnumerator<ELEMENT_T> GetEnumerator() => _internalCollection.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class ChunkOfArray<ELEMENT_T>
            : IEnumerable<ELEMENT_T[]>
        {
            private class Enumerator
                : IEnumerator<ELEMENT_T[]>
            {
                private bool _isDisposed;
                private IEnumerable<ELEMENT_T> _source;
                private int _count;
                private IEnumerator<ELEMENT_T> _sourceEnumerator;
                private ELEMENT_T[] _value;
                private bool _isEndOfSourceSequence;
                private bool _isEndOfSequence;

                public Enumerator(IEnumerable<ELEMENT_T> source, int count)
                {
                    _isDisposed = false;
                    _source = source;
                    _count = count;
                    _sourceEnumerator = _source.GetEnumerator();
                    _value = null;
                    _isEndOfSourceSequence = false;
                    _isEndOfSequence = false;
                }

                public ELEMENT_T[] Current
                {
                    get
                    {
                        if (_isDisposed)
                            throw new ObjectDisposedException(GetType().FullName);
                        if (_isEndOfSequence)
                            throw new InvalidOperationException();
                        if (_value == null)
                            throw new InvalidOperationException();
                        return _value;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _value = null;
                    if (_isEndOfSequence)
                        return false;
                    var buffer = new ELEMENT_T[_count];
                    int bufferCount = 0;
                    while (bufferCount < _count)
                    {
                        if (_isEndOfSourceSequence || _sourceEnumerator.MoveNext() == false)
                        {
                            _isEndOfSourceSequence = true;
                            break;
                        }
                        buffer[bufferCount] = _sourceEnumerator.Current;
                        ++bufferCount;
                    }
                    if (bufferCount <= 0)
                    {
                        _isEndOfSequence = true;
                        return false;
                    }
                    else
                    {
                        if (bufferCount != buffer.Length)
                            Array.Resize(ref buffer, bufferCount);
                        _value = buffer;
                        return true;
                    }
                }

                public void Reset()
                {
                    _sourceEnumerator?.Dispose();
                    _sourceEnumerator = _source.GetEnumerator();
                    _value = null;
                    _isEndOfSourceSequence = false;
                    _isEndOfSequence = false;
                }

                protected virtual void Dispose(bool disposing)
                {
                    if (!_isDisposed)
                    {
                        if (disposing)
                        {
                            if (_sourceEnumerator != null)
                            {
                                _sourceEnumerator.Dispose();
                                _sourceEnumerator = null;
                            }
                        }
                        _isDisposed = true;
                    }
                }

                public void Dispose()
                {
                    Dispose(disposing: true);
                    GC.SuppressFinalize(this);
                }
            }

            private IEnumerable<ELEMENT_T> _source;
            private int _count;

            public ChunkOfArray(IEnumerable<ELEMENT_T> source, int count)
            {
                _source = source;
                _count = count;
            }

            public IEnumerator<ELEMENT_T[]> GetEnumerator()
            {
                return new Enumerator(_source, _count);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public static ELEMENT_T[] QuickSort<ELEMENT_T, KEY_T>(this ELEMENT_T[] source, Func<ELEMENT_T, KEY_T> keySekecter)
            where KEY_T : IComparable<KEY_T>
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySekecter == null)
                throw new ArgumentNullException("keySekecter");
            source.QuickSort(keySekecter, 0, source.Length - 1);
#if DEBUG
            for (int index = 0; index < source.Length - 1; index++)
            {
                if (keySekecter(source[index]).CompareTo(keySekecter(source[index + 1])) > 0)
                    throw new Exception();
            }
#endif
            return source;
        }

        public static ELEMENT_T[] QuickSort<ELEMENT_T, KEY_T>(this ELEMENT_T[] source, Func<ELEMENT_T, KEY_T> keySekecter, IComparer<KEY_T> keyComparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySekecter == null)
                throw new ArgumentNullException("keySekecter");
            if (keyComparer == null)
                throw new ArgumentNullException("keyComparer");
            source.QuickSort(keySekecter, keyComparer, 0, source.Length - 1);
#if DEBUG
            for (int index = 0; index < source.Length - 1; index++)
            {
                if (keyComparer.Compare(keySekecter(source[index]), keySekecter(source[index + 1])) > 0)
                    throw new Exception();
            }
#endif
            return source;
        }

        public static IReadOnlyArray<ELEMENT_T> QuickSort<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySekecter)
            where KEY_T : IComparable<KEY_T>
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySekecter == null)
                throw new ArgumentNullException("keySekecter");
            var array = source.ToArray();
            array.QuickSort(keySekecter, 0, array.Length - 1);
            return array.AsReadOnly();
        }

        public static IReadOnlyArray<ELEMENT_T> QuickSort<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySekecter, IComparer<KEY_T> keyComparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySekecter == null)
                throw new ArgumentNullException("keySekecter");
            if (keyComparer == null)
                throw new ArgumentNullException("keyComparer");
            var array = source.ToArray();
            array.QuickSort(keySekecter, keyComparer, 0, array.Length - 1);
            return array.AsReadOnly();
        }

        public static bool None<ELEMENT_T>(this IEnumerable<ELEMENT_T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            return !source.Any();
        }

        public static bool None<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            return !source.Any(predicate);
        }

        public static bool NotAll<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            return !source.All(predicate);
        }

        public static bool IsSingle<ELEMENT_T>(this IEnumerable<ELEMENT_T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            return source.Take(2).Count() == 1;
        }

        public static IEnumerable<ELEMENT_T> GetSequence<ELEMENT_T>(this ELEMENT_T[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            return new ArrayEnumerable<ELEMENT_T>(buffer.AsReadOnly(), 0, buffer.Length);
        }

        public static IEnumerable<ELEMENT_T> GetSequence<ELEMENT_T>(this ELEMENT_T[] buffer, int offset)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentException("'offset' must not be negative.", "offset");
            if (offset > buffer.Length)
                throw new IndexOutOfRangeException("'offset' is greater than 'buffer.Length'.");
            return new ArrayEnumerable<ELEMENT_T>(buffer.AsReadOnly(), offset, buffer.Length - offset);
        }

        public static IEnumerable<ELEMENT_T> GetSequence<ELEMENT_T>(this ELEMENT_T[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentException("'offset' must not be negative.", "offset");
            if (count < 0)
                throw new ArgumentException("'count' must not be negative.", "count");
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException("'offset + count' is greater than 'buffer.Length'.");
            return new ArrayEnumerable<ELEMENT_T>(buffer.AsReadOnly(), offset, count);
        }

        public static IEnumerable<ELEMENT_T> GetSequence<ELEMENT_T>(this IReadOnlyArray<ELEMENT_T> buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            return new ArrayEnumerable<ELEMENT_T>(buffer, 0, buffer.Length);
        }

        public static IEnumerable<ELEMENT_T> GetSequence<ELEMENT_T>(this IReadOnlyArray<ELEMENT_T> buffer, int offset)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentException("'offset' must not be negative.", "offset");
            if (offset > buffer.Length)
                throw new IndexOutOfRangeException("'offset' is greater than 'buffer.Length'.");
            return new ArrayEnumerable<ELEMENT_T>(buffer, offset, buffer.Length - offset);
        }

        public static IEnumerable<ELEMENT_T> GetSequence<ELEMENT_T>(this IReadOnlyArray<ELEMENT_T> buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentException("'offset' must not be negative.", "offset");
            if (count < 0)
                throw new ArgumentException("'count' must not be negative.", "count");
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException("'offset + count' is greater than 'buffer.Length'.");
            return new ArrayEnumerable<ELEMENT_T>(buffer, offset, count);
        }

        public static bool SequenceEqual<ELEMENT_T>(this ELEMENT_T[] array1, ELEMENT_T[] array2)
            where ELEMENT_T: IEquatable<ELEMENT_T>
        {
            if (array1 == null)
                return array2 == null;
            else if (array2 == null)
                return false;
            else if (array1.Length != array2.Length)
                return false;
            for (int index = 0; index < array1.Length; index++)
            {
                if (!Equals(array1[index], array2[index]))
                    return false;
            }
            return true;
        }

        public static bool SequenceEqual<ELEMENT_T>(this IReadOnlyArray<ELEMENT_T> array1, IReadOnlyArray<ELEMENT_T> array2)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (array1 == null)
                return array2 == null;
            else if (array2 == null)
                return false;
            else if (array1.Length != array2.Length)
                return false;
            for (int index = 0; index < array1.Length; index++)
            {
                if (!Equals(array1[index], array2[index]))
                    return false;
            }
            return true;
        }

        public static int SequenceCompare<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, IEnumerable<ELEMENT_T> other)
            where ELEMENT_T: IComparable<ELEMENT_T>
        {
            return source.SequenceCompare(other, item => item);
        }

        public static int SequenceCompare<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, IComparer<ELEMENT_T> comparer)
        {
            return source.SequenceCompare(other, item => item, comparer);
        }

        public static int SequenceCompare<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IComparable<KEY_T>
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (other == null)
                throw new ArgumentNullException("other");
            if (keySelecter == null)
                throw new ArgumentNullException("keySelecter");
            var enumerator1 = (IEnumerator<ELEMENT_T>)null;
            var enumerator2 = (IEnumerator<ELEMENT_T>)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    int c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (isOk1 == false)
                        return 0;
                    if ((c = keySelecter(enumerator1.Current).CompareTo(keySelecter(enumerator2.Current))) != 0)
                        return c;
                }
            }
            finally
            {
                if (enumerator1 != null)
                    enumerator1.Dispose();
                if (enumerator2 != null)
                    enumerator2.Dispose();
            }
        }

        public static int SequenceCompare<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter, IComparer<KEY_T> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (other == null)
                throw new ArgumentNullException("other");
            if (keySelecter == null)
                throw new ArgumentNullException("keySelecter");
            if (comparer == null)
                throw new ArgumentNullException("comparer");
            var enumerator1 = (IEnumerator<ELEMENT_T>)null;
            var enumerator2 = (IEnumerator<ELEMENT_T>)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    int c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (isOk1 == false)
                        return 0;
                    if ((c = comparer.Compare(keySelecter(enumerator1.Current), keySelecter(enumerator2.Current))) != 0)
                        return c;
                }
            }
            finally
            {
                if (enumerator1 != null)
                    enumerator1.Dispose();
                if (enumerator2 != null)
                    enumerator2.Dispose();
            }
        }

        public static IComparer<VALUE_T> CreateComparer<VALUE_T>(this IEnumerable<VALUE_T> source, Func<VALUE_T, VALUE_T, int> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (comparer == null)
                throw new ArgumentNullException("comparer");
            return new CustomizableComparer<VALUE_T>(comparer);
        }

        public static IEqualityComparer<VALUE_T> CreateEqualityComparer<VALUE_T>(this IEnumerable<VALUE_T> source, Func<VALUE_T, VALUE_T, bool> equalityComparer, Func<VALUE_T, int> hashCalculater)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (equalityComparer == null)
                throw new ArgumentNullException("equalityComparer");
            if (hashCalculater == null)
                throw new ArgumentNullException("hashCalculater");
            return new CustomizableEqualityComparer<VALUE_T>(equalityComparer, hashCalculater);
        }

        public static IReadOnlyArray<ELEMENT_T> AsReadOnly<ELEMENT_T>(this ELEMENT_T[] source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            return new ReadOnlyArrayWrapper<ELEMENT_T>(source);
        }

        public static IReadOnlyCollection<ELEMENT_T> ToReadOnlyCollection<ELEMENT_T>(this IEnumerable<ELEMENT_T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            return new ReadOnlyCollectionWrapper<ELEMENT_T>(source.ToList());
        }

        public static IComparer<CAPSULE_T> Map<CAPSULE_T, VALUE_T>(this IComparer<VALUE_T> comparer, Func<CAPSULE_T, VALUE_T> selecter)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");
            if (selecter == null)
                throw new ArgumentNullException("selecter");
            return new CustomizableComparer<CAPSULE_T>((value1, value2) => comparer.Compare(selecter(value1), selecter(value2)));
        }

        public static IEqualityComparer<CAPSULE_T> Map<CAPSULE_T, VALUE_T>(this IEqualityComparer<VALUE_T> equalityComparer, Func<CAPSULE_T, VALUE_T> selecter)
        {
            if (equalityComparer == null)
                throw new ArgumentNullException("equalityComparer");
            if (selecter == null)
                throw new ArgumentNullException("selecter");
            return
                new CustomizableEqualityComparer<CAPSULE_T>(
                    (value1, value2) => equalityComparer.Equals(selecter(value1), selecter(value2)),
                    value => selecter(value).GetHashCode());
        }

        public static IEnumerable<IReadOnlyArray<ELEMENT_T>> ToChunkOfReadOnlyArray<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count <= 0)
                throw new ArgumentException("'count' must not be less than or equal to zero.", "count");
            return new ChunkOfArray<ELEMENT_T>(source, count).Select(array => array.AsReadOnly());
        }

        public static IEnumerable<ELEMENT_T[]> ToChunkOfArray<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count <= 0)
                throw new ArgumentException("'count' must not be less than or equal to zero.", "count");
            return new ChunkOfArray<ELEMENT_T>(source, count);
        }

        public static IReadOnlyArray<ELEMENT_T> Duplicate<ELEMENT_T>(this IReadOnlyArray<ELEMENT_T> array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            var buffer = new ELEMENT_T[array.Length];
            array.CopyTo(buffer, 0);
            return buffer.AsReadOnly();
        }

        public static ELEMENT_T[] Duplicate<ELEMENT_T>(this ELEMENT_T[] array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            var buffer = new ELEMENT_T[array.Length];
            array.CopyTo(buffer, 0);
            return buffer;
        }

        public static IEnumerable<ELEMENT_T> QuickDistinct<ELEMENT_T>(this IEnumerable<ELEMENT_T> source)
            where ELEMENT_T: IEquatable<ELEMENT_T>
        {
            return QuickDistinct(source, new Dictionary<ELEMENT_T, object>());
        }

        public static IEnumerable<ELEMENT_T> QuickDistinct<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, IEqualityComparer<ELEMENT_T> equalityComparer)
        {
            return QuickDistinct(source, new Dictionary<ELEMENT_T, object>(equalityComparer));
        }

        public static void ForEach<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, Action<ELEMENT_T> action)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            foreach (var element in source)
                action(element);
        }

        private static void QuickSort<ELEMENT_T, KEY_T>(this ELEMENT_T[] source, Func<ELEMENT_T, KEY_T> keySekecter, int startIndex, int endIndex)
            where KEY_T : IComparable<KEY_T>
        {
            if (endIndex <= startIndex)
            {
                // 要素が1個以下の場合はソートは不要なのでそのまま返る
                return;
            }
            else if (endIndex == startIndex + 1)
            {
                // 要素が2個の場合
                if (keySekecter(source[startIndex]).CompareTo(keySekecter(source[endIndex])) > 0)
                    source.SwapElement(startIndex, endIndex);
                return;
            }
            else
            {
                // 要素が3個以上の場合

                var pivotKey = new[]
                {
                    keySekecter(source[startIndex]),
                    keySekecter(source[(startIndex + endIndex) / 2]),
                    keySekecter(source[endIndex]),
                }
                .OrderBy(item => item)
                .Skip(1)
                .First();

                var index1 = startIndex;
                var index2 = endIndex;

                while (index1 < index2)
                {
                    while (index1 <= index2 && keySekecter(source[index1]).CompareTo(pivotKey) <= 0)
                        ++index1;
                    while (index1 <= index2 && pivotKey.CompareTo(keySekecter(source[index2])) < 0)
                        --index2;
                    if (index1 >= index2)
                        break;
#if DEBUG
                    if (index1 == index2)
                        throw new Exception();
#endif
                    source.SwapElement(index1, index2);
                    ++index1;
                    --index2;
                }
#if DEBUG
                if (index2 - index1 == 1)
                    throw new Exception();
#endif
                source.QuickSort(keySekecter, startIndex, index1 - 1);
                source.QuickSort(keySekecter, index2 + 1, endIndex);
            }
        }

        private static int CompareKey<KEY_T>(KEY_T key1, KEY_T key2)
            where KEY_T : IComparable<KEY_T>
        {
            if (key1 == null)
                return key2 == null ? 0 : -1;
            else if (key2 == null)
                return 1;
            else
                return key1.CompareTo(key2);
        }

        private static void QuickSort<ELEMENT_T, KEY_T>(this ELEMENT_T[] source, Func<ELEMENT_T, KEY_T> keySekecter, IComparer<KEY_T> keyComparer, int startIndex, int endIndex)
        {
            if (endIndex <= startIndex)
            {
                // 要素が1個以下の場合はソートは不要なのでそのまま返る
                return;
            }
            else if (endIndex == startIndex + 1)
            {
                // 要素が2個の場合
                if (keyComparer.Compare(keySekecter(source[startIndex]), keySekecter(source[endIndex])) > 0)
                    source.SwapElement(startIndex, endIndex);
                return;
            }
            else
            {
                // 要素が3個以上の場合

                var pivotKey = new[]
                {
                    keySekecter(source[startIndex]),
                    keySekecter(source[(startIndex + endIndex) / 2]),
                    keySekecter(source[endIndex]),
                }
                .OrderBy(item => item, keyComparer)
                .Skip(1)
                .First();

                var index1 = startIndex;
                var index2 = endIndex;

                while (index1 < index2)
                {
                    while (index1 <= index2 && keyComparer.Compare(keySekecter(source[index1]), pivotKey) <= 0)
                        ++index1;
                    while (index1 <= index2 && keyComparer.Compare(pivotKey, keySekecter(source[index2])) < 0)
                        --index2;
                    if (index1 >= index2)
                        break;
#if DEBUG
                    if (index1 == index2)
                        throw new Exception();
#endif
                    source.SwapElement(index1, index2);
                    ++index1;
                    --index2;
                }
#if DEBUG
                if (index2 - index1 == 1)
                    throw new Exception();
#endif
                source.QuickSort(keySekecter, keyComparer, startIndex, index1 - 1);
                source.QuickSort(keySekecter, keyComparer, index2 + 1, endIndex);
            }
        }

        private static int CompareKey<KEY_T>(KEY_T key1, KEY_T key2, IComparer<KEY_T> keyComparer)
        {
            return keyComparer.Compare(key1, key2);
        }

        private static void SwapElement<ELEMENT_T>(this ELEMENT_T[] source, int index1, int index2)
        {
            var t = source[index1];
            source[index1] = source[index2];
            source[index2] = t;
        }

        private static bool Equals<ELEMENT_T>(ELEMENT_T x, ELEMENT_T y)
            where ELEMENT_T: IEquatable<ELEMENT_T>
        {
            if (x == null)
                return y == null;
            else if (y == null)
                return false;
            else
                return x.Equals(y);
        }

        private static IEnumerable<ELEMENT_T> QuickDistinct<ELEMENT_T>(IEnumerable<ELEMENT_T> source, IDictionary<ELEMENT_T, object> outputElements)
        {
            return
                source
                .Where(element =>
                {
                    if (outputElements.ContainsKey(element))
                        return false;
                    outputElements[element] = null;
                    return true;
                });
        }
    }
}
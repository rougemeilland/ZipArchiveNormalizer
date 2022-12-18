using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utility
{
    public static class LinqExtensions
    {
        #region private class

        private class ReadOnlyCollectionWrapper<ELEMENT_T>
            : IReadOnlyCollection<ELEMENT_T>
        {
            private readonly ICollection<ELEMENT_T> _internalCollection;

            public ReadOnlyCollectionWrapper(ICollection<ELEMENT_T> sourceCollection)
            {
                if (sourceCollection is null)
                    throw new ArgumentNullException(nameof(sourceCollection));

                _internalCollection = sourceCollection;
            }

            public Int32 Count => _internalCollection.Count;

            public IEnumerator<ELEMENT_T> GetEnumerator() => _internalCollection.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        #endregion

        public static IEnumerable<ELEMENT_T> WhereNotNull<ELEMENT_T>(this IEnumerable<ELEMENT_T?> source)
            where ELEMENT_T : notnull
        {
            foreach (var element in source)
            {
                if (element is not null)
                    yield return element;
            }
        }

        #region QuickSort

        public static ReadOnlyMemory<ELEMENT_T> QuickSort<ELEMENT_T>(this IEnumerable<ELEMENT_T> source)
            where ELEMENT_T : IComparable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return source.ToArray().QuickSort().AsReadOnly();
        }

        public static ReadOnlyMemory<ELEMENT_T> QuickSort<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, IComparer<ELEMENT_T> keyComparer)
            where ELEMENT_T : IComparable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keyComparer is null)
                throw new ArgumentNullException(nameof(keyComparer));

            return source.ToArray().QuickSort(keyComparer).AsReadOnly();
        }

        public static ReadOnlyMemory<ELEMENT_T> QuickSort<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySekecter)
            where KEY_T : IComparable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySekecter is null)
                throw new ArgumentNullException(nameof(keySekecter));

            return source.ToArray().QuickSort(keySekecter).AsReadOnly();
        }

        public static ReadOnlyMemory<ELEMENT_T> QuickSort<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySekecter, IComparer<KEY_T> keyComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySekecter is null)
                throw new ArgumentNullException(nameof(keySekecter));
            if (keyComparer is null)
                throw new ArgumentNullException(nameof(keyComparer));

            return source.ToArray().QuickSort(keySekecter, keyComparer).AsReadOnly();
        }

        #endregion

        #region None

        public static bool None<ELEMENT_T>(this IEnumerable<ELEMENT_T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return !source.Any();
        }

        public static bool None<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            return !source.Any(predicate);
        }

        public static bool NotAll<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            return !source.All(predicate);
        }

        #endregion

        public static bool IsSingle<ELEMENT_T>(this IEnumerable<ELEMENT_T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return source.Take(2).Count() == 1;
        }

        #region SequenceEqual

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IEquatable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = enumerator2.MoveNext();
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var key1 = keySelecter(enumerator1.Current);
                    var key2 = keySelecter(enumerator2.Current);
                    if (key1 is null)
                    {
                        if (key2 is not null)
                            return false;
                    }
                    else
                    {
                        if (!key1.Equals(key2))
                            return false;
                    }

                }
            }
            finally
            {
                enumerator1?.Dispose();
                enumerator2?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter, IEqualityComparer<KEY_T> keyEqualityComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = enumerator2.MoveNext();
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var key1 = keySelecter(enumerator1.Current);
                    var key2 = keySelecter(enumerator2.Current);
                    if (!keyEqualityComparer.Equals(key1, key2))
                        return false;
                }
            }
            finally
            {
                enumerator1?.Dispose();
                enumerator2?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, ELEMENT_T[] other)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var element1 = enumerator1.Current;
                    var element2 = other[index2];
                    if (element1 is null)
                    {
                        if (element2 is not null)
                            return false;
                    }
                    else
                    {
                        if (!element1.Equals(element2))
                            return false;
                    }
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, ELEMENT_T[] other, IEqualityComparer<ELEMENT_T> equalityComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var element1 = enumerator1.Current;
                    var element2 = other[index2];
                    if (!equalityComparer.Equals(element1, element2))
                        return false;
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, ELEMENT_T[] other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IEquatable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var key1 = keySelecter(enumerator1.Current);
                    var key2 = keySelecter(other[index2]);
                    if (key1 is null)
                    {
                        if (key2 is not null)
                            return false;
                    }
                    else
                    {
                        if (!key1.Equals(key2))
                            return false;
                    }
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, ELEMENT_T[] other, Func<ELEMENT_T, KEY_T> keySelecter, IEqualityComparer<KEY_T> keyEqualityComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var key1 = keySelecter(enumerator1.Current);
                    var key2 = keySelecter(other[index2]);
                    if (!keyEqualityComparer.Equals(key1, key2))
                        return false;
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, Span<ELEMENT_T> other)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return source.SequenceEqual((ReadOnlySpan<ELEMENT_T>)other);
        }

        public static bool SequenceEqual<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, Span<ELEMENT_T> other, IEqualityComparer<ELEMENT_T> equalityComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));

            return source.SequenceEqual((ReadOnlySpan<ELEMENT_T>)other, equalityComparer);
        }

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, Span<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IEquatable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            return source.SequenceEqual((ReadOnlySpan<ELEMENT_T>)other, keySelecter);
        }

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, Span<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter, IEqualityComparer<KEY_T> keyEqualityComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            return source.SequenceEqual((ReadOnlySpan<ELEMENT_T>)other, keySelecter, keyEqualityComparer);
        }

        public static bool SequenceEqual<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, ReadOnlySpan<ELEMENT_T> other)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var element1 = enumerator1.Current;
                    var element2 = other[index2];
                    if (element1 is null)
                    {
                        if (element2 is not null)
                            return false;
                    }
                    else
                    {
                        if (!element1.Equals(element2))
                            return false;
                    }
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, ReadOnlySpan<ELEMENT_T> other, IEqualityComparer<ELEMENT_T> equalityComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var element1 = enumerator1.Current;
                    var element2 = other[index2];
                    if (!equalityComparer.Equals(element1, element2))
                        return false;
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, ReadOnlySpan<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IEquatable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var key1 = keySelecter(enumerator1.Current);
                    var key2 = keySelecter(other[index2]);
                    if (key1 is null)
                    {
                        if (key2 is not null)
                            return false;
                    }
                    else
                    {
                        if (!key1.Equals(key2))
                            return false;
                    }
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, ReadOnlySpan<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter, IEqualityComparer<KEY_T> keyEqualityComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var key1 = keySelecter(enumerator1.Current);
                    var key2 = keySelecter(other[index2]);
                    if (!keyEqualityComparer.Equals(key1, key2))
                        return false;
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T>(this ELEMENT_T[] source, IEnumerable<ELEMENT_T> other)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var element1 = source[index1];
                    var element2 = enumerator2.Current;
                    if (element1 is null)
                    {
                        if (element2 is not null)
                            return false;
                    }
                    else
                    {
                        if (!element1.Equals(element2))
                            return false;
                    }
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T>(this ELEMENT_T[] source, IEnumerable<ELEMENT_T> other, IEqualityComparer<ELEMENT_T> equalityComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var element1 = source[index1];
                    var element2 = enumerator2.Current;
                    if (!equalityComparer.Equals(element1, element2))
                        return false;
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this ELEMENT_T[] source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IEquatable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var key1 = keySelecter(source[index1]);
                    var key2 = keySelecter(enumerator2.Current);
                    if (key1 is null)
                    {
                        if (key2 is not null)
                            return false;
                    }
                    else
                    {
                        if (!key1.Equals(key2))
                            return false;
                    }
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this ELEMENT_T[] source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter, IEqualityComparer<KEY_T> keyEqualityComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var key1 = keySelecter(source[index1]);
                    var key2 = keySelecter(enumerator2.Current);
                    if (!keyEqualityComparer.Equals(key1, key2))
                        return false;
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T>(this Span<ELEMENT_T> source, IEnumerable<ELEMENT_T> other)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            return ((ReadOnlySpan<ELEMENT_T>)source).SequenceEqual(other);
        }

        public static bool SequenceEqual<ELEMENT_T>(this Span<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, IEqualityComparer<ELEMENT_T> equalityComparer)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));

            return ((ReadOnlySpan<ELEMENT_T>)source).SequenceEqual(other, equalityComparer);
        }

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this Span<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IEquatable<KEY_T>
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            return ((ReadOnlySpan<ELEMENT_T>)source).SequenceEqual(other, keySelecter);
        }

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this Span<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter, IEqualityComparer<KEY_T> keyEqualityComparer)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            return ((ReadOnlySpan<ELEMENT_T>)source).SequenceEqual(other, keySelecter, keyEqualityComparer);
        }

        public static bool SequenceEqual<ELEMENT_T>(this ReadOnlySpan<ELEMENT_T> source, IEnumerable<ELEMENT_T> other)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var element1 = source[index1];
                    var element2 = enumerator2.Current;
                    if (element1 is null)
                    {
                        if (element2 is not null)
                            return false;
                    }
                    else
                    {
                        if (!element1.Equals(element2))
                            return false;
                    }
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T>(this ReadOnlySpan<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, IEqualityComparer<ELEMENT_T> equalityComparer)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var element1 = source[index1];
                    var element2 = enumerator2.Current;
                    if (!equalityComparer.Equals(element1, element2))
                        return false;
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this ReadOnlySpan<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IEquatable<KEY_T>
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var key1 = keySelecter(source[index1]);
                    var key2 = keySelecter(enumerator2.Current);
                    if (key1 is null)
                    {
                        if (key2 is not null)
                            return false;
                    }
                    else
                    {
                        if (!key1.Equals(key2))
                            return false;
                    }
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static bool SequenceEqual<ELEMENT_T, KEY_T>(this ReadOnlySpan<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter, IEqualityComparer<KEY_T> keyEqualityComparer)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if (isOk1 != isOk2)
                        return false;
                    if (!isOk1)
                        return true;
                    var key1 = keySelecter(source[index1]);
                    var key2 = keySelecter(enumerator2.Current);
                    if (!keyEqualityComparer.Equals(key1, key2))
                        return false;
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        #endregion

        #region SequenceCompare

        public static Int32 SequenceCompare<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, IEnumerable<ELEMENT_T> other)
            where ELEMENT_T : IComparable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    Int32 c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var element1 = enumerator1.Current;
                    var element2 = enumerator2.Current;
                    if (element1 is null)
                    {
                        if (element2 is not null)
                            return -1;
                    }
                    else
                    {
                        if (element2 is null)
                            return 1;
                        else
                        {
                            if ((c = element1.CompareTo(element2)) != 0)
                                return c;
                        }
                    }
                }
            }
            finally
            {
                enumerator1?.Dispose();
                enumerator2?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, IComparer<ELEMENT_T> comparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    Int32 c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var element1 = enumerator1.Current;
                    var element2 = enumerator2.Current;
                    if ((c = comparer.Compare(element1, element2)) != 0)
                        return c;
                }
            }
            finally
            {
                enumerator1?.Dispose();
                enumerator2?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IComparable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    Int32 c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var key1 = keySelecter(enumerator1.Current);
                    var key2 = keySelecter(enumerator2.Current);
                    if (key1 is null)
                    {
                        if (key2 is not null)
                            return -1;
                    }
                    else
                    {
                        if (key2 is null)
                            return 1;
                        else
                        {
                            if ((c = key1.CompareTo(key2)) != 0)
                                return c;
                        }
                    }
                }
            }
            finally
            {
                enumerator1?.Dispose();
                enumerator2?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter, IComparer<KEY_T> keyComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyComparer is null)
                throw new ArgumentNullException(nameof(keyComparer));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    Int32 c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var key1 = keySelecter(enumerator1.Current);
                    var key2 = keySelecter(enumerator2.Current);
                    if ((c = keyComparer.Compare(key1, key2)) != 0)
                        return c;
                }
            }
            finally
            {
                enumerator1?.Dispose();
                enumerator2?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, ELEMENT_T[] other)
            where ELEMENT_T : IComparable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    Int32 c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var element1 = enumerator1.Current;
                    var element2 = other[index2];
                    if (element1 is null)
                    {
                        if (element2 is not null)
                            return -1;
                    }
                    else
                    {
                        if ((c = element1.CompareTo(element2)) != 0)
                            return c;
                    }
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, ELEMENT_T[] other, IComparer<ELEMENT_T> comparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    Int32 c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var element1 = enumerator1.Current;
                    var element2 = other[index2];
                    if ((c = comparer.Compare(element1, element2)) != 0)
                        return c;
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, ELEMENT_T[] other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IComparable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    Int32 c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var key1 = keySelecter(enumerator1.Current);
                    var key2 = keySelecter(other[index2]);
                    if (key1 is null)
                    {
                        if (key2 is not null)
                            return -1;
                    }
                    else
                    {
                        if ((c = key1.CompareTo(key2)) != 0)
                            return c;
                    }
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, ELEMENT_T[] other, Func<ELEMENT_T, KEY_T> keySelecter, IComparer<KEY_T> keyComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyComparer is null)
                throw new ArgumentNullException(nameof(keyComparer));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    Int32 c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var key1 = keySelecter(enumerator1.Current);
                    var key2 = keySelecter(other[index2]);
                    if ((c = keyComparer.Compare(key1, key2)) != 0)
                        return c;
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, Span<ELEMENT_T> other)
            where ELEMENT_T : IComparable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return source.SequenceCompare((ReadOnlySpan<ELEMENT_T>)other);
        }

        public static Int32 SequenceCompare<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, Span<ELEMENT_T> other, IComparer<ELEMENT_T> comparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            return source.SequenceCompare((ReadOnlySpan<ELEMENT_T>)other, comparer);
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, Span<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IComparable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            return source.SequenceCompare((ReadOnlySpan<ELEMENT_T>)other, keySelecter);
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, Span<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter, IComparer<KEY_T> keyComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyComparer is null)
                throw new ArgumentNullException(nameof(keyComparer));

            return source.SequenceCompare((ReadOnlySpan<ELEMENT_T>)other, keySelecter, keyComparer);
        }

        public static Int32 SequenceCompare<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, ReadOnlySpan<ELEMENT_T> other)
            where ELEMENT_T : IComparable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    Int32 c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var element1 = enumerator1.Current;
                    var element2 = other[index2];
                    if (element1 is null)
                    {
                        if (element2 is not null)
                            return -1;
                    }
                    else
                    {
                        if ((c = element1.CompareTo(element2)) != 0)
                            return c;
                    }
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, ReadOnlySpan<ELEMENT_T> other, IComparer<ELEMENT_T> comparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    Int32 c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var element1 = enumerator1.Current;
                    var element2 = other[index2];
                    if ((c = comparer.Compare(element1, element2)) != 0)
                        return c;
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, ReadOnlySpan<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IComparable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    Int32 c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var key1 = keySelecter(enumerator1.Current);
                    var key2 = keySelecter(other[index2]);
                    if (key1 is null)
                    {
                        if (key2 is not null)
                            return -1;
                    }
                    else
                    {
                        if ((c = key1.CompareTo(key2)) != 0)
                            return c;
                    }
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this IEnumerable<ELEMENT_T> source, ReadOnlySpan<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter, IComparer<KEY_T> keyComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyComparer is null)
                throw new ArgumentNullException(nameof(keyComparer));

            var enumerator1 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                enumerator1 = source.GetEnumerator();
                var index2 = 0;
                while (true)
                {
                    Int32 c;
                    var isOk1 = enumerator1.MoveNext();
                    var isOk2 = index2 < other.Length;
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var key1 = keySelecter(enumerator1.Current);
                    var key2 = keySelecter(other[index2]);
                    if ((c = keyComparer.Compare(key1, key2)) != 0)
                        return c;
                    ++index2;
                }
            }
            finally
            {
                enumerator1?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T>(this ELEMENT_T[] source, IEnumerable<ELEMENT_T> other)
            where ELEMENT_T : IComparable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    Int32 c;
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var element1 = source[index1];
                    var element2 = enumerator2.Current;
                    if (element1 is null)
                    {
                        if (element2 is not null)
                            return -1;
                    }
                    else
                    {
                        if (element2 is null)
                            return 1;
                        else
                        {
                            if ((c = element1.CompareTo(element2)) != 0)
                                return c;
                        }
                    }
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T>(this ELEMENT_T[] source, IEnumerable<ELEMENT_T> other, IComparer<ELEMENT_T> comparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    Int32 c;
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var element1 = source[index1];
                    var element2 = enumerator2.Current;
                    if ((c = comparer.Compare(element1, element2)) != 0)
                        return c;
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this ELEMENT_T[] source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IComparable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    Int32 c;
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var key1 = keySelecter(source[index1]);
                    var key2 = keySelecter(enumerator2.Current);
                    if (key1 is null)
                    {
                        if (key2 is not null)
                            return -1;
                    }
                    else
                    {
                        if (key2 is null)
                            return 1;
                        else
                        {
                            if ((c = key1.CompareTo(key2)) != 0)
                                return c;
                        }
                    }
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this ELEMENT_T[] source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter, IComparer<KEY_T> keyComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyComparer is null)
                throw new ArgumentNullException(nameof(keyComparer));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    Int32 c;
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var key1 = keySelecter(source[index1]);
                    var key2 = keySelecter(enumerator2.Current);
                    if ((c = keyComparer.Compare(key1, key2)) != 0)
                        return c;
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T>(this Span<ELEMENT_T> source, IEnumerable<ELEMENT_T> other)
            where ELEMENT_T : IComparable<ELEMENT_T>
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            return ((ReadOnlySpan<ELEMENT_T>)source).SequenceCompare(other);
        }

        public static Int32 SequenceCompare<ELEMENT_T>(this Span<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, IComparer<ELEMENT_T> comparer)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            return ((ReadOnlySpan<ELEMENT_T>)source).SequenceCompare(other, comparer);
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this Span<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IComparable<KEY_T>
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            return ((ReadOnlySpan<ELEMENT_T>)source).SequenceCompare(other, keySelecter);
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this Span<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter, IComparer<KEY_T> keyComparer)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyComparer is null)
                throw new ArgumentNullException(nameof(keyComparer));

            return ((ReadOnlySpan<ELEMENT_T>)source).SequenceCompare(other, keySelecter, keyComparer);
        }

        public static Int32 SequenceCompare<ELEMENT_T>(this ReadOnlySpan<ELEMENT_T> source, IEnumerable<ELEMENT_T> other)
            where ELEMENT_T : IComparable<ELEMENT_T>
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    Int32 c;
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var element1 = source[index1];
                    var element2 = enumerator2.Current;
                    if (element1 is null)
                    {
                        if (element2 is not null)
                            return -1;
                    }
                    else
                    {
                        if (element2 is null)
                            return 1;
                        else
                        {
                            if ((c = element1.CompareTo(element2)) != 0)
                                return c;
                        }
                    }
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T>(this ReadOnlySpan<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, IComparer<ELEMENT_T> comparer)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    Int32 c;
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var element1 = source[index1];
                    var element2 = enumerator2.Current;
                    if ((c = comparer.Compare(element1, element2)) != 0)
                        return c;
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this ReadOnlySpan<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter)
            where KEY_T : IComparable<KEY_T>
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    Int32 c;
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var key1 = keySelecter(source[index1]);
                    var key2 = keySelecter(enumerator2.Current);
                    if (key1 is null)
                    {
                        if (key2 is not null)
                            return -1;
                    }
                    else
                    {
                        if (key2 is null)
                            return 1;
                        else
                        {
                            if ((c = key1.CompareTo(key2)) != 0)
                                return c;
                        }
                    }
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        public static Int32 SequenceCompare<ELEMENT_T, KEY_T>(this ReadOnlySpan<ELEMENT_T> source, IEnumerable<ELEMENT_T> other, Func<ELEMENT_T, KEY_T> keySelecter, IComparer<KEY_T> keyComparer)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (keyComparer is null)
                throw new ArgumentNullException(nameof(keyComparer));

            var enumerator2 = (IEnumerator<ELEMENT_T>?)null;
            try
            {
                var index1 = 0;
                enumerator2 = other.GetEnumerator();
                while (true)
                {
                    Int32 c;
                    var isOk1 = index1 < source.Length;
                    var isOk2 = enumerator2.MoveNext();
                    if ((c = isOk1.CompareTo(isOk2)) != 0)
                        return c;
                    if (!isOk1)
                        return 0;
                    var key1 = keySelecter(source[index1]);
                    var key2 = keySelecter(enumerator2.Current);
                    if ((c = keyComparer.Compare(key1, key2)) != 0)
                        return c;
                    ++index1;
                }
            }
            finally
            {
                enumerator2?.Dispose();
            }
        }

        #endregion

        public static IComparer<VALUE_T> CreateComparer<VALUE_T>(this IEnumerable<VALUE_T> source, Func<VALUE_T, VALUE_T, Int32> comparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            return new CustomizableComparer<VALUE_T>(comparer);
        }

        public static ReadOnlyMemory<ELEMENT_T> ToReadOnlyMemory<ELEMENT_T>(this IEnumerable<ELEMENT_T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return (ReadOnlyMemory<ELEMENT_T>)source.ToArray();
        }

        public static ReadOnlySpan<ELEMENT_T> ToReadOnlySpan<ELEMENT_T>(this IEnumerable<ELEMENT_T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return (ReadOnlySpan<ELEMENT_T>)source.ToArray();
        }

        public static IReadOnlyCollection<ELEMENT_T> ToReadOnlyCollection<ELEMENT_T>(this IEnumerable<ELEMENT_T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return new ReadOnlyCollectionWrapper<ELEMENT_T>(source.ToList());
        }

        public static IComparer<CAPSULE_T> MapComparer<CAPSULE_T, VALUE_T>(this IComparer<VALUE_T> comparer, Func<CAPSULE_T, VALUE_T> selecter)
        {
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));
            if (selecter is null)
                throw new ArgumentNullException(nameof(selecter));

            return new CustomizableComparer<CAPSULE_T>((value1, value2) => comparer.Compare(selecter(value1), selecter(value2)));
        }

        public static IEnumerable<ReadOnlyMemory<ELEMENT_T>> ChunkAsReadOnlyMemory<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, Int32 count)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            return source.Chunk(count).Select(array => array.AsReadOnly());
        }

        public static IEnumerable<ELEMENT_T[]> ChunkAsArray<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, Int32 count)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            return source.Chunk(count);
        }

        #region QuickDistinct

        public static IEnumerable<ELEMENT_T> QuickDistinct<ELEMENT_T>(this IEnumerable<ELEMENT_T> source)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return QuickDistinct(source, new Dictionary<ELEMENT_T, object?>());
        }

        public static IEnumerable<ELEMENT_T> QuickDistinct<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, IEqualityComparer<ELEMENT_T> equalityComparer)
            where ELEMENT_T : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));

            return QuickDistinct(source, new Dictionary<ELEMENT_T, object?>(equalityComparer));
        }

        #endregion

        public static void ForEach<ELEMENT_T>(this IEnumerable<ELEMENT_T> source, Action<ELEMENT_T> action)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            foreach (var element in source)
                action(element);
        }

        private static IEnumerable<ELEMENT_T> QuickDistinct<ELEMENT_T>(IEnumerable<ELEMENT_T> source, IDictionary<ELEMENT_T, object?> outputElements)
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

﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.Threading
{
    // TODO:  .ConfigureAwait(false) のつけ忘れを探す正規表現 => ^(?=.* await .*)(?!.*\.ConfigureAwait\(false\).*).*$
    public static class AsyncLinqExtensions
    {
        #region private class

        private class AsAsyncEnumerable<ELEMENT_T>
            : IAsyncEnumerable<ELEMENT_T>
        {
            private class Enumerator
                : IAsyncEnumerator<ELEMENT_T>
            {
                private readonly CancellationToken _cancellationToken;
                private readonly IEnumerator<ELEMENT_T> _sourceEnumerator;

                private bool _isDisposed;

                public Enumerator(IEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
                {
                    _isDisposed = false;
                    _cancellationToken = cancellationToken;
                    _sourceEnumerator = source.GetEnumerator();
                }

                public ELEMENT_T Current
                {
                    get
                    {
                        if (_isDisposed)
                            throw new ObjectDisposedException(GetType().FullName);
                        _cancellationToken.ThrowIfCancellationRequested();

                        return _sourceEnumerator.Current;
                    }
                }

                public ValueTask<bool> MoveNextAsync()
                {
                    if (_isDisposed)
                        throw new ObjectDisposedException(GetType().FullName);
                    _cancellationToken.ThrowIfCancellationRequested();

                    return ValueTask.FromResult(_sourceEnumerator.MoveNext());
                }

                public ValueTask DisposeAsync()
                {
                    if (!_isDisposed)
                    {

                        _sourceEnumerator.Dispose();
                        _isDisposed = true;
                    }
                    GC.SuppressFinalize(this);
                    return ValueTask.CompletedTask;
                }
            }

            private readonly IEnumerable<ELEMENT_T> _source;

            public AsAsyncEnumerable(IEnumerable<ELEMENT_T> source)
            {
                _source = source;
            }

            public IAsyncEnumerator<ELEMENT_T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
                new Enumerator(_source, cancellationToken);
        }

        private class AsSyncEnumerable<ELEMENT_T>
            : IEnumerable<ELEMENT_T>
        {
            private class Enumerator
                : IEnumerator<ELEMENT_T>
            {
                private readonly CancellationToken _cancellationToken;
                private readonly IAsyncEnumerator<ELEMENT_T> _sourceEnumerator;

                private bool _isDisposed;

                public Enumerator(IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken)
                {
                    _isDisposed = false;
                    _cancellationToken = cancellationToken;
                    _sourceEnumerator = source.GetAsyncEnumerator(cancellationToken);
                }

                public ELEMENT_T Current
                {
                    get
                    {
                        if (_isDisposed)
                            throw new ObjectDisposedException(GetType().FullName);
                        _cancellationToken.ThrowIfCancellationRequested();

                        return _sourceEnumerator.Current;
                    }
                }

                object? IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_isDisposed)
                        throw new ObjectDisposedException(GetType().FullName);

                    return _sourceEnumerator.MoveNextAsync().AsTask().Result;
                }

                public void Dispose()
                {
                    Dispose(disposing: true);
                    GC.SuppressFinalize(this);
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }

                protected virtual void Dispose(bool disposing)
                {
                    if (!_isDisposed)
                    {
                        if (disposing)
                        {
                            try
                            {
                                _sourceEnumerator.DisposeAsync().AsTask().Wait();
                            }
                            catch (Exception)
                            {
                            }
                            _isDisposed = true;
                        }
                        _isDisposed = true;
                    }
                }
            }

            private readonly IAsyncEnumerable<ELEMENT_T> _source;
            private readonly CancellationToken _cancellationToken;

            public AsSyncEnumerable(IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken)
            {
                _source = source;
                _cancellationToken = cancellationToken;
            }

            public IEnumerator<ELEMENT_T> GetEnumerator() => new Enumerator(_source, _cancellationToken);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class Grouping<KEY_T, ELEMENT_T>
            : IGrouping<KEY_T, ELEMENT_T>
        {
            private readonly IEnumerable<ELEMENT_T> _elements;

            public Grouping(KEY_T key, IEnumerable<ELEMENT_T> elements)
            {
                Key = key;
                _elements = elements;
            }

            public KEY_T Key { get; }
            public IEnumerator<ELEMENT_T> GetEnumerator() => _elements.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class Lookup<KEY_T, SOURCE_T>
            : ILookup<KEY_T, SOURCE_T>
        {
            private IDictionary<KEY_T, ICollection<SOURCE_T>> _collection;

            public Lookup(IDictionary<KEY_T, ICollection<SOURCE_T>> collection)
            {
                _collection = collection;
            }

            public IEnumerable<SOURCE_T> this[KEY_T key] => _collection[key];
            public int Count => _collection.Count;
            public bool Contains(KEY_T key) => _collection.ContainsKey(key);
            public IEnumerator<IGrouping<KEY_T, SOURCE_T>> GetEnumerator() => _collection.Select(item => new Grouping<KEY_T, SOURCE_T>(item.Key, item.Value)).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class OrderedAsyncEnumerable<ELEMENT_T, KEY_T>
            : IOrderedAsyncEnumerable<ELEMENT_T>
        {
            private class Comparer
                : IComparer<ELEMENT_T>
            {
                private readonly Func<ELEMENT_T, KEY_T> _keySelector;
                private readonly IComparer<KEY_T> _keyComparer;
                private readonly bool _descending;
                private readonly IOrderedAsyncEnumerable<ELEMENT_T>? _parent;

                public Comparer(Func<ELEMENT_T, KEY_T> keySelector, IComparer<KEY_T>? keyComparer, bool descending, IOrderedAsyncEnumerable<ELEMENT_T>? parent)
                {
                    _keySelector = keySelector;
                    _keyComparer = keyComparer ?? Comparer<KEY_T?>.Default;
                    _descending = descending;
                    _parent = parent;

                }

                Int32 IComparer<ELEMENT_T>.Compare(ELEMENT_T? x, ELEMENT_T? y)
                {
                    if (_parent is not null)
                    {
                        var c = _parent.GetComparer().Compare(x, y);
                        if (c != 0)
                            return c;
                    }
                    return
                        _descending
                            ? Compare(y, x)
                            : Compare(x, y);
                }

                private int Compare(ELEMENT_T? x, ELEMENT_T? y)
                {
                    KEY_T xKey;
                    KEY_T yKey;
                    if (x is null || (xKey = _keySelector(x)) is null)
                        return (y is null || _keySelector(y) is null) ? 0 : -1;
                    else if (y is null || (yKey = _keySelector(y)) is null)
                        return 1;
                    else
                        return _keyComparer.Compare(xKey, yKey);
                }
            }

            private readonly IAsyncEnumerable<ELEMENT_T> _source;
            private readonly Func<ELEMENT_T, KEY_T> _keySelector;
            private readonly IComparer<KEY_T>? _comparer;
            private readonly bool _descending;
            private readonly IOrderedAsyncEnumerable<ELEMENT_T>? _parent;

            public OrderedAsyncEnumerable(IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector, IComparer<KEY_T>? comparer, bool descending)
            {
                _source = source;
                _keySelector = keySelector;
                _comparer = comparer;
                _descending = descending;
                _parent = null;
            }

            public OrderedAsyncEnumerable(IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector, IComparer<KEY_T>? comparer, bool descending, IOrderedAsyncEnumerable<ELEMENT_T> parent)
            {
                _source = source;
                _keySelector = keySelector;
                _comparer = comparer;
                _descending = descending;
                _parent = parent;
            }

            public async IAsyncEnumerator<ELEMENT_T> GetAsyncEnumerator(CancellationToken cancellationToken)
            {
                var comparer = GetComparer();
                var enumerator = _source.GetAsyncEnumerator(cancellationToken);
                var elements = new List<ELEMENT_T>();
                await using (enumerator.ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        elements.Add(enumerator.Current);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                var resultElements = elements.ToArray().QuickSort(comparer);
                foreach (var resultElement in resultElements)
                    yield return resultElement;
            }

            public IComparer<ELEMENT_T> GetComparer() => new Comparer(_keySelector, _comparer, _descending, _parent);
        }

        #endregion

        public static IAsyncEnumerable<ELEMENT_T> AsAsync<ELEMENT_T>(this IEnumerable<ELEMENT_T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return new AsAsyncEnumerable<ELEMENT_T>(source);
        }

        public static IEnumerable<ELEMENT_T> AsSync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return new AsSyncEnumerable<ELEMENT_T>(source, cancellationToken);
        }

        #region Aggregate

        public static async Task<ELEMENT_T> AggregateAsync<ELEMENT_T>(IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, ELEMENT_T, ELEMENT_T> func, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (func is null)
                throw new ArgumentNullException(nameof(func));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();
                cancellationToken.ThrowIfCancellationRequested();
                ELEMENT_T value = enumerator.Current;
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    value = func(value, enumerator.Current);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return value;
            }
        }

        public static async Task<ACCUMULATE_T> AggregateAsync<ELEMENT_T, ACCUMULATE_T>(this IAsyncEnumerable<ELEMENT_T> source, ACCUMULATE_T seed, Func<ACCUMULATE_T, ELEMENT_T, ACCUMULATE_T> func, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (func is null)
                throw new ArgumentNullException(nameof(func));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                ACCUMULATE_T value = seed;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    value = func(value, enumerator.Current);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return value;
            }
        }

        public static async Task<RESULT_T> AggregateAsync<ELEMENT_T, ACCUMULATE_T, RESULT_T>(this IAsyncEnumerable<ELEMENT_T> source, ACCUMULATE_T seed, Func<ACCUMULATE_T, ELEMENT_T, ACCUMULATE_T> func, Func<ACCUMULATE_T, RESULT_T> resultSelector, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            if (resultSelector is null)
                throw new ArgumentNullException(nameof(resultSelector));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                ACCUMULATE_T value = seed;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    value = func(value, enumerator.Current);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return resultSelector(value);
            }
        }

        #endregion

        public static async Task<bool> AllAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    if (!predicate(enumerator.Current))
                        return false;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return true;
            }
        }

        #region Any

        public static async Task<bool> AnyAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await enumerator.MoveNextAsync().ConfigureAwait(false);
            }
        }

        public static async Task<bool> AnyAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    if (predicate(enumerator.Current))
                        return true;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return false;
            }
        }

        #endregion

        public static async IAsyncEnumerable<ELEMENT_T> Append<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, ELEMENT_T element, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    yield return enumerator.Current;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            yield return element;
        }

        public static async IAsyncEnumerable<ELEMENT_T[]> ChunkAsArray<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Int32 size, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            var buffer = new List<ELEMENT_T>();
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    buffer.Add(enumerator.Current);
                    if (buffer.Count >= size)
                    {
                        yield return buffer.ToArray();
                        buffer.Clear();
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            if (buffer.Count > 0)
                yield return buffer.ToArray();
        }

        public static async IAsyncEnumerable<ReadOnlyMemory<ELEMENT_T>> ChunkAsReadOnlyMemory<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Int32 size, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            var buffer = new List<ELEMENT_T>();
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    buffer.Add(enumerator.Current);
                    if (buffer.Count >= size)
                    {
                        yield return buffer.ToArray();
                        buffer.Clear();
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            if (buffer.Count > 0)
                yield return buffer.ToArray();
        }

        public static async IAsyncEnumerable<ELEMENT_T> Concat<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> first, IAsyncEnumerable<ELEMENT_T> second, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));

            {
                var firstEnumerator = first.GetAsyncEnumerator(cancellationToken);
                await using (firstEnumerator.ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    while (await firstEnumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield return firstEnumerator.Current;
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }
            {
                var secondEnumerator = second.GetAsyncEnumerator(cancellationToken);
                await using (secondEnumerator.ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    while (await secondEnumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield return secondEnumerator.Current;
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }
        }

        #region Contains

        public static async Task<bool> ContainsAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, ELEMENT_T value, CancellationToken cancellationToken = default)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    if (value is null)
                    {
                        if (enumerator.Current is null)
                            return true;
                    }
                    else
                    {
                        if (value.Equals(enumerator.Current))
                            return true;
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return false;
            }
        }

        public static async Task<bool> ContainsAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, ELEMENT_T value, IEqualityComparer<ELEMENT_T> equalityComparer, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    if (equalityComparer.Equals(enumerator.Current, value))
                        return true;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return false;
            }
        }

        #endregion

        #region Count

        public static async Task<Int32> CountAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var count = 0;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    checked
                    {
                        ++count;
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return count;
            }
        }

        public static async Task<Int32> CountAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var count = 0;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    if (predicate(enumerator.Current))
                    {
                        checked
                        {
                            ++count;
                        }
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return count;
            }
        }

        #endregion

        public static IComparer<VALUE_T> CreateComparer<VALUE_T>(this IAsyncEnumerable<VALUE_T> source, Func<VALUE_T, VALUE_T, Int32> comparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            return new CustomizableComparer<VALUE_T>(comparer);
        }

        #region Distinct

        public static async IAsyncEnumerable<ELEMENT_T> Distinct<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var set = new HashSet<ELEMENT_T>();
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    if (set.Add(element))
                        yield return element;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public static async IAsyncEnumerable<ELEMENT_T> Distinct<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, IEqualityComparer<ELEMENT_T> equalityComparer, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var set = new HashSet<ELEMENT_T>(equalityComparer);
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    if (set.Add(element))
                        yield return element;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public static async IAsyncEnumerable<ELEMENT_T> Distinct<ELEMENT_T, KEY_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where ELEMENT_T : IEquatable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var set = new HashSet<KEY_T>();
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    if (set.Add(keySelector(element)))
                        yield return element;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public static async IAsyncEnumerable<ELEMENT_T> Distinct<ELEMENT_T, KEY_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector, IEqualityComparer<KEY_T> keyEqualityComparer, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var set = new HashSet<KEY_T>(keyEqualityComparer);
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    if (set.Add(keySelector(element)))
                        yield return element;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        #endregion

        #region First

        public static async Task<ELEMENT_T> FirstAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();
                return enumerator.Current;
            }
        }

        public static async Task<ELEMENT_T> FirstAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                return (await enumerator.InternalFirstOrDefaultAsync(predicate, () => throw new InvalidOperationException(), cancellationToken).ConfigureAwait(false)).FirstValue;
            }
        }

        #endregion

        #region FirstOrDefault

        public static async Task<ELEMENT_T?> FirstOrDefaultAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return
                    await enumerator.MoveNextAsync().ConfigureAwait(false)
                        ? enumerator.Current
                        : default;
            }
        }

        public static async Task<ELEMENT_T> FirstOrDefaultAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, ELEMENT_T defaultValue, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return
                    await enumerator.MoveNextAsync().ConfigureAwait(false)
                        ? enumerator.Current
                        : defaultValue;
            }
        }

        public static async Task<ELEMENT_T?> FirstOrDefaultAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    if (predicate(value))
                        return value;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return default;
            }
        }

        public static async Task<ELEMENT_T> FirstOrDefaultAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, ELEMENT_T defaultValue, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                return (await enumerator.InternalFirstOrDefaultAsync(predicate, () => defaultValue, cancellationToken).ConfigureAwait(false)).FirstValue;
            }
        }

        #endregion

        public static async Task ForEachAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Action<ELEMENT_T> action, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    action(enumerator.Current);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        #region GroupBy

        public static IAsyncEnumerable<IGrouping<KEY_T, SOURCE_T>> GroupBy<SOURCE_T, KEY_T>(this IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, CancellationToken cancellationToken = default)
            where KEY_T : notnull, IEqualityComparer<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            return InternalGroupBy<SOURCE_T, KEY_T, SOURCE_T, IGrouping<KEY_T, SOURCE_T>>(source, keySelector, element => element, (key, elements) => new Grouping<KEY_T, SOURCE_T>(key, elements), cancellationToken);
        }

        public static IAsyncEnumerable<IGrouping<KEY_T, SOURCE_T>> GroupBy<SOURCE_T, KEY_T>(this IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, IEqualityComparer<KEY_T> keyEqualityComparer, CancellationToken cancellationToken = default)
            where KEY_T : notnull
        {
            return InternalGroupBy<SOURCE_T, KEY_T, SOURCE_T, IGrouping<KEY_T, SOURCE_T>>(source, keySelector, element => element, (key, elements) => new Grouping<KEY_T, SOURCE_T>(key, elements), keyEqualityComparer, cancellationToken);
        }

        public static IAsyncEnumerable<IGrouping<KEY_T, ELEMENT_T>> GroupBy<SOURCE_T, KEY_T, ELEMENT_T>(this IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, Func<SOURCE_T, ELEMENT_T> elementSelector, CancellationToken cancellationToken = default)
            where KEY_T : notnull, IEqualityComparer<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector is null)
                throw new ArgumentNullException(nameof(elementSelector));

            return InternalGroupBy<SOURCE_T, KEY_T, ELEMENT_T, IGrouping<KEY_T, ELEMENT_T>>(source, keySelector, elementSelector, (key, elements) => new Grouping<KEY_T, ELEMENT_T>(key, elements), cancellationToken);
        }

        public static IAsyncEnumerable<IGrouping<KEY_T, ELEMENT_T>> GroupBy<SOURCE_T, KEY_T, ELEMENT_T>(this IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, Func<SOURCE_T, ELEMENT_T> elementSelector, IEqualityComparer<KEY_T> keyEqualityComparer, CancellationToken cancellationToken = default)
            where KEY_T : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector is null)
                throw new ArgumentNullException(nameof(elementSelector));

            return InternalGroupBy<SOURCE_T, KEY_T, ELEMENT_T, IGrouping<KEY_T, ELEMENT_T>>(source, keySelector, elementSelector, (key, elements) => new Grouping<KEY_T, ELEMENT_T>(key, elements), keyEqualityComparer, cancellationToken);
        }

        public static IAsyncEnumerable<RESULT_T> GroupBy<SOURCE_T, KEY_T, RESULT_T>(this IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, Func<KEY_T, IEnumerable<SOURCE_T>, RESULT_T> resultSelector, CancellationToken cancellationToken = default)
            where KEY_T : notnull, IEqualityComparer<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (resultSelector is null)
                throw new ArgumentNullException(nameof(resultSelector));

            return InternalGroupBy(source, keySelector, element => element, resultSelector, cancellationToken);
        }

        public static IAsyncEnumerable<RESULT_T> GroupBy<SOURCE_T, KEY_T, RESULT_T>(this IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, Func<KEY_T, IEnumerable<SOURCE_T>, RESULT_T> resultSelector, IEqualityComparer<KEY_T> keyEqualityComparer, CancellationToken cancellationToken = default)
            where KEY_T : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (resultSelector is null)
                throw new ArgumentNullException(nameof(resultSelector));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            return InternalGroupBy(source, keySelector, element => element, resultSelector, keyEqualityComparer, cancellationToken);
        }

        public static IAsyncEnumerable<RESULT_T> GroupBy<SOURCE_T, KEY_T, ELEMENT_T, RESULT_T>(this IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, Func<SOURCE_T, ELEMENT_T> elementSelector, Func<KEY_T, IEnumerable<ELEMENT_T>, RESULT_T> resultSelector, CancellationToken cancellationToken = default)
            where KEY_T : notnull, IEqualityComparer<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector is null)
                throw new ArgumentNullException(nameof(elementSelector));
            if (resultSelector is null)
                throw new ArgumentNullException(nameof(resultSelector));

            return InternalGroupBy(source, keySelector, elementSelector, resultSelector, cancellationToken);
        }

        public static IAsyncEnumerable<RESULT_T> GroupBy<SOURCE_T, KEY_T, ELEMENT_T, RESULT_T>(this IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, Func<SOURCE_T, ELEMENT_T> elementSelector, Func<KEY_T, IEnumerable<ELEMENT_T>, RESULT_T> resultSelector, IEqualityComparer<KEY_T> keyEqualityComparer, CancellationToken cancellationToken = default)
            where KEY_T : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector is null)
                throw new ArgumentNullException(nameof(elementSelector));
            if (resultSelector is null)
                throw new ArgumentNullException(nameof(resultSelector));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            return InternalGroupBy(source, keySelector, elementSelector, resultSelector, keyEqualityComparer, cancellationToken);
        }

        #endregion

        #region Last

        public static async Task<ELEMENT_T> LastAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();
                var lastValue = enumerator.Current;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    lastValue = enumerator.Current;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return lastValue;
            }
        }

        public static async Task<ELEMENT_T> LastAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();
                var lastValue = enumerator.Current;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    if (predicate(value))
                        lastValue = value;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return lastValue;
            }
        }

        #endregion

        #region LastOrDefault

        public static async Task<ELEMENT_T?> LastOrDefaultAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var lastValue = (ELEMENT_T?)default;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    lastValue = enumerator.Current;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return lastValue;
            }
        }

        public static async Task<ELEMENT_T> LastOrDefaultAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, ELEMENT_T defaultValue, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var lastValue = defaultValue;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    lastValue = enumerator.Current;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return lastValue;
            }
        }

        public static async Task<ELEMENT_T?> LastOrDefaultAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var lastValue = (ELEMENT_T?)default;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    if (predicate(value))
                        lastValue = value;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return lastValue;
            }
        }

        public static async Task<ELEMENT_T> LastOrDefaulAsynctAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, ELEMENT_T defaultValue, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var lastValue = defaultValue;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    if (predicate(value))
                        lastValue = value;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return lastValue;
            }
        }

        #endregion

        #region Max

        public static async Task<ELEMENT_T> MaxAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
            where ELEMENT_T : IComparable<ELEMENT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();
                var maxValue = enumerator.Current;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    if (maxValue.CompareTo(value) < 0)
                        maxValue = value;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return maxValue;
            }
        }

        public static async Task<RESULT_T> MaxAsync<ELEMENT_T, RESULT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, RESULT_T> selector, CancellationToken cancellationToken = default)
            where RESULT_T : IComparable<RESULT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();
                var maxValue = selector(enumerator.Current);
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = selector(enumerator.Current);
                    if (maxValue.CompareTo(value) < 0)
                        maxValue = value;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return maxValue;
            }
        }

        public static async Task<ELEMENT_T> MaxAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, IComparer<ELEMENT_T> comparer, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();
                var maxValue = enumerator.Current;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    if (comparer.Compare(maxValue, value) < 0)
                        maxValue = value;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return maxValue;
            }
        }

        public static async Task<RESULT_T> MaxAsync<ELEMENT_T, RESULT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, RESULT_T> selector, IComparer<RESULT_T> comparer, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();
                var maxValue = selector(enumerator.Current);
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = selector(enumerator.Current);
                    if (comparer.Compare(maxValue, value) < 0)
                        maxValue = value;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return maxValue;
            }
        }

        #endregion

        #region Min

        public static async Task<ELEMENT_T> MinAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        where ELEMENT_T : IComparable<ELEMENT_T>
        {
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();
                var minValue = enumerator.Current;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    if (minValue.CompareTo(value) > 0)
                        minValue = value;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return minValue;
            }
        }

        public static async Task<RESULT_T> MinAsync<ELEMENT_T, RESULT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, RESULT_T> selector, CancellationToken cancellationToken = default)
            where RESULT_T : IComparable<RESULT_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();
                var minValue = selector(enumerator.Current);
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = selector(enumerator.Current);
                    if (minValue.CompareTo(value) > 0)
                        minValue = value;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return minValue;
            }
        }

        public static async Task<ELEMENT_T> MinAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, IComparer<ELEMENT_T> comparer, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();
                var minValue = enumerator.Current;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    if (comparer.Compare(minValue, value) > 0)
                        minValue = value;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return minValue;
            }
        }

        public static async Task<RESULT_T> MinAsync<ELEMENT_T, RESULT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, RESULT_T> selector, IComparer<RESULT_T> comparer, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();
                var minValue = selector(enumerator.Current);
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = selector(enumerator.Current);
                    if (comparer.Compare(minValue, value) > 0)
                        minValue = value;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return minValue;
            }
        }

        #endregion

        #region None

        public static async Task<bool> NoneAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return !await enumerator.MoveNextAsync().ConfigureAwait(false);
            }
        }

        public static async Task<bool> NoneAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    if (predicate(enumerator.Current))
                        return false;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return true;
            }
        }

        #endregion

        public static async Task<bool> NotAllAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    if (!predicate(enumerator.Current))
                        return true;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return false;
            }
        }

        #region OrderBy

        public static IOrderedAsyncEnumerable<ELEMENT_T> OrderBy<ELEMENT_T, KEY_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            return new OrderedAsyncEnumerable<ELEMENT_T, KEY_T>(source, keySelector, null, false);
        }

        public static IOrderedAsyncEnumerable<ELEMENT_T> OrderBy<ELEMENT_T, KEY_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector, IComparer<KEY_T> keyComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (keyComparer is null)
                throw new ArgumentNullException(nameof(keyComparer));

            return new OrderedAsyncEnumerable<ELEMENT_T, KEY_T>(source, keySelector, keyComparer, false);
        }

        #endregion

        #region OrderByDescending

        public static IOrderedAsyncEnumerable<ELEMENT_T> OrderByDescending<ELEMENT_T, KEY_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            return new OrderedAsyncEnumerable<ELEMENT_T, KEY_T>(source, keySelector, null, true);
        }

        public static IOrderedAsyncEnumerable<ELEMENT_T> OrderByDescending<ELEMENT_T, KEY_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector, IComparer<KEY_T> keyComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (keyComparer is null)
                throw new ArgumentNullException(nameof(keyComparer));

            return new OrderedAsyncEnumerable<ELEMENT_T, KEY_T>(source, keySelector, keyComparer, true);
        }

        #endregion

        public static async IAsyncEnumerable<ELEMENT_T> Prepend<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, ELEMENT_T element, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            yield return element;
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    yield return enumerator.Current;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public async static IAsyncEnumerable<ELEMENT_T> Reverse<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var buffer = new Stack<ELEMENT_T>();
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    buffer.Push(enumerator.Current);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            while (buffer.Count > 0)
            {
                yield return buffer.Pop();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        #region Select

        public static async IAsyncEnumerable<RESULT_T> Select<ELEMENT_T, RESULT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, RESULT_T> selector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    yield return selector(enumerator.Current);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public static async IAsyncEnumerable<RESULT_T> Select<ELEMENT_T, RESULT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, Int32, RESULT_T> selector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));

            var index = 0;
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    yield return selector(enumerator.Current, checked(index++));
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        #endregion

        #region SelectMany

        public static async IAsyncEnumerable<RESULT_T> SelectMany<ELEMENT_T, RESULT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, IAsyncEnumerable<RESULT_T>> selector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));

            var enumerator1 = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator1.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator1.MoveNextAsync().ConfigureAwait(false))
                {
                    var enumerator2 = selector(enumerator1.Current).GetAsyncEnumerator(cancellationToken);
                    await using (enumerator2.ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        while (await enumerator2.MoveNextAsync().ConfigureAwait(false))
                        {
                            yield return enumerator2.Current;
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public static async IAsyncEnumerable<RESULT_T> SelectMany<ELEMENT_T, RESULT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, Int32, IAsyncEnumerable<RESULT_T>> selector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));

            var index = 0;
            var enumerator1 = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator1.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator1.MoveNextAsync().ConfigureAwait(false))
                {
                    var enumerator2 = selector(enumerator1.Current, checked(index++)).GetAsyncEnumerator(cancellationToken);
                    await using (enumerator2.ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        while (await enumerator2.MoveNextAsync().ConfigureAwait(false))
                        {
                            yield return enumerator2.Current;
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public static async IAsyncEnumerable<RESULT_T> SelectMany<ELEMENT_T, COLLECTION_T, RESULT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, IAsyncEnumerable<COLLECTION_T>> collectionSelector, Func<ELEMENT_T, COLLECTION_T, RESULT_T> resultSelector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (collectionSelector is null)
                throw new ArgumentNullException(nameof(collectionSelector));
            if (resultSelector is null)
                throw new ArgumentNullException(nameof(resultSelector));

            var enumerator1 = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator1.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator1.MoveNextAsync().ConfigureAwait(false))
                {
                    var enumerator2 = collectionSelector(enumerator1.Current).GetAsyncEnumerator(cancellationToken);
                    await using (enumerator2.ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        while (await enumerator2.MoveNextAsync().ConfigureAwait(false))
                        {
                            yield return resultSelector(enumerator1.Current, enumerator2.Current);
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public static async IAsyncEnumerable<RESULT_T> SelectMany<ELEMENT_T, COLLECTION_T, RESULT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, Int32, IAsyncEnumerable<COLLECTION_T>> collectionSelector, Func<ELEMENT_T, COLLECTION_T, RESULT_T> resultSelector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (collectionSelector is null)
                throw new ArgumentNullException(nameof(collectionSelector));
            if (resultSelector is null)
                throw new ArgumentNullException(nameof(resultSelector));

            var index = 0;
            var enumerator1 = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator1.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator1.MoveNextAsync().ConfigureAwait(false))
                {
                    var enumerator2 = collectionSelector(enumerator1.Current, checked(index++)).GetAsyncEnumerator(cancellationToken);
                    await using (enumerator2.ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        while (await enumerator2.MoveNextAsync().ConfigureAwait(false))
                        {
                            yield return resultSelector(enumerator1.Current, enumerator2.Current);
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        #endregion

        #region SequenceCompare

        public static async Task<Int32> SequenceCompareAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> first, IAsyncEnumerable<ELEMENT_T> second, CancellationToken cancellationToken = default)
            where ELEMENT_T : IComparable<ELEMENT_T>
        {
            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));

            var firstEnumerator = first.GetAsyncEnumerator(cancellationToken);
            await using (firstEnumerator.ConfigureAwait(false))
            {
                var secondEnumerator = second.GetAsyncEnumerator(cancellationToken);
                await using (secondEnumerator.ConfigureAwait(false))
                {
                    while (true)
                    {
                        var isOkFirstTask = firstEnumerator.MoveNextAsync().AsTask();
                        var isOkSecondTask = secondEnumerator.MoveNextAsync().AsTask();
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.WhenAll(isOkFirstTask, isOkSecondTask).ConfigureAwait(false);
                        var isOkFirst = isOkFirstTask.Result;
                        var isOkSecond = isOkSecondTask.Result;

                        // 片方のシーケンスだけが終端に達した場合は、シーケンスが終端に達していない方が大きいとみなす。
                        if (isOkFirst != isOkSecond)
                            return isOkFirst.CompareTo(isOkSecond);

                        // 両方のシーケンスが終端に達した場合は 0e を返す。
                        if (!isOkFirst)
                            return 0;

                        // どちらのシーケンスも終端に達していない場合は要素を比較して、異なっている場合は false を返す。
                        var firstValue = firstEnumerator.Current;
                        if (firstValue is null)
                            return secondEnumerator.Current is null ? 0 : -1;
                        else
                        {
                            Int32 c;
                            if ((c = firstEnumerator.Current.CompareTo(secondEnumerator.Current)) != 0)
                                return c;
                        }
                    }
                }
            }
        }

        public static async Task<Int32> SequenceCompareAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> first, IAsyncEnumerable<ELEMENT_T> second, IComparer<ELEMENT_T> comparer, CancellationToken cancellationToken = default)
        {
            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            var firstEnumerator = first.GetAsyncEnumerator(cancellationToken);
            await using (firstEnumerator.ConfigureAwait(false))
            {
                var secondEnumerator = second.GetAsyncEnumerator(cancellationToken);
                await using (secondEnumerator.ConfigureAwait(false))
                {
                    while (true)
                    {
                        var isOkFirstTask = firstEnumerator.MoveNextAsync().AsTask();
                        var isOkSecondTask = secondEnumerator.MoveNextAsync().AsTask();
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.WhenAll(isOkFirstTask, isOkSecondTask).ConfigureAwait(false);
                        var isOkFirst = isOkFirstTask.Result;
                        var isOkSecond = isOkSecondTask.Result;

                        // 片方のシーケンスだけが終端に達した場合は、シーケンスが終端に達していない方が大きいとみなす。
                        if (isOkFirst != isOkSecond)
                            return isOkFirst.CompareTo(isOkSecond);

                        // 両方のシーケンスが終端に達した場合は 0e を返す。
                        if (!isOkFirst)
                            return 0;

                        // どちらのシーケンスも終端に達していない場合は要素を比較して、異なっている場合は false を返す。
                        Int32 c;
                        if ((c = comparer.Compare(firstEnumerator.Current, secondEnumerator.Current)) != 0)
                            return c;
                    }
                }
            }
        }

        public static async Task<Int32> SequenceCompareAsync<ELEMENT_T, KEY_T>(this IAsyncEnumerable<ELEMENT_T> first, IAsyncEnumerable<ELEMENT_T> second, Func<ELEMENT_T, KEY_T> keySelecter, CancellationToken cancellationToken = default)
            where KEY_T : IComparable<KEY_T>
        {
            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));

            var firstEnumerator = first.GetAsyncEnumerator(cancellationToken);
            await using (firstEnumerator.ConfigureAwait(false))
            {
                var secondEnumerator = second.GetAsyncEnumerator(cancellationToken);
                await using (secondEnumerator.ConfigureAwait(false))
                {
                    while (true)
                    {
                        var isOkFirstTask = firstEnumerator.MoveNextAsync().AsTask();
                        var isOkSecondTask = secondEnumerator.MoveNextAsync().AsTask();
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.WhenAll(isOkFirstTask, isOkSecondTask).ConfigureAwait(false);
                        var isOkFirst = isOkFirstTask.Result;
                        var isOkSecond = isOkSecondTask.Result;

                        // 片方のシーケンスだけが終端に達した場合は、シーケンスが終端に達していない方が大きいとみなす。
                        if (isOkFirst != isOkSecond)
                            return isOkFirst.CompareTo(isOkSecond);

                        // 両方のシーケンスが終端に達した場合は 0e を返す。
                        if (!isOkFirst)
                            return 0;

                        // どちらのシーケンスも終端に達していない場合は要素を比較して、異なっている場合は false を返す。
                        var firstValue = keySelecter(firstEnumerator.Current);
                        if (firstValue is null)
                            return keySelecter(secondEnumerator.Current) is null ? 0 : -1;
                        else
                        {
                            Int32 c;
                            if ((c = firstValue.CompareTo(keySelecter(secondEnumerator.Current))) != 0)
                                return c;
                        }
                    }
                }
            }
        }

        public static async Task<Int32> SequenceCompareAsync<ELEMENT_T, KEY_T>(this IAsyncEnumerable<ELEMENT_T> first, IAsyncEnumerable<ELEMENT_T> second, Func<ELEMENT_T, KEY_T> keySelecter, IComparer<KEY_T> comparer, CancellationToken cancellationToken = default)
        {
            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));

            var firstEnumerator = first.GetAsyncEnumerator(cancellationToken);
            await using (firstEnumerator.ConfigureAwait(false))
            {
                var secondEnumerator = second.GetAsyncEnumerator(cancellationToken);
                await using (secondEnumerator.ConfigureAwait(false))
                {
                    while (true)
                    {
                        var isOkFirstTask = firstEnumerator.MoveNextAsync().AsTask();
                        var isOkSecondTask = secondEnumerator.MoveNextAsync().AsTask();
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.WhenAll(isOkFirstTask, isOkSecondTask).ConfigureAwait(false);
                        var isOkFirst = isOkFirstTask.Result;
                        var isOkSecond = isOkSecondTask.Result;

                        // 片方のシーケンスだけが終端に達した場合は、シーケンスが終端に達していない方が大きいとみなす。
                        if (isOkFirst != isOkSecond)
                            return isOkFirst.CompareTo(isOkSecond);

                        // 両方のシーケンスが終端に達した場合は 0e を返す。
                        if (!isOkFirst)
                            return 0;

                        // どちらのシーケンスも終端に達していない場合は要素を比較して、異なっている場合は false を返す。
                        Int32 c;
                        if ((c = comparer.Compare(keySelecter(firstEnumerator.Current), keySelecter(secondEnumerator.Current))) != 0)
                            return c;
                    }
                }
            }
        }

        #endregion

        #region SequenceEqual

        public static async Task<bool> SequenceEqualAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> first, IAsyncEnumerable<ELEMENT_T> second, CancellationToken cancellationToken = default)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));

            var firstEnumerator = first.GetAsyncEnumerator(cancellationToken);
            await using (firstEnumerator.ConfigureAwait(false))
            {
                var secondEnumerator = second.GetAsyncEnumerator(cancellationToken);
                await using (secondEnumerator.ConfigureAwait(false))
                {
                    while (true)
                    {
                        var isOkFirstTask = firstEnumerator.MoveNextAsync().AsTask();
                        var isOkSecondTask = secondEnumerator.MoveNextAsync().AsTask();
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.WhenAll(isOkFirstTask, isOkSecondTask).ConfigureAwait(false);
                        var isOkFirst = isOkFirstTask.Result;
                        var isOkSecond = isOkSecondTask.Result;

                        // 片方のシーケンスだけが終端に達した場合は false を返す。
                        if (isOkFirst != isOkSecond)
                            return false;

                        // 両方のシーケンスが終端に達した場合は true を返す。
                        if (!isOkFirst)
                            return true;

                        // どちらのシーケンスも終端に達していない場合は要素を比較する。
                        var firstValue = firstEnumerator.Current;
                        if (firstValue is null)
                        {
                            // 片方が null であり、かつもう片方が null ではない場合は false を返す。
                            if (secondEnumerator.Current is not null)
                                return false;
                        }
                        else
                        {
                            // 要素を Equals で比較し、結果が false なら false を返す。
                            if (!firstValue.Equals(secondEnumerator.Current))
                                return false;
                        }
                    }
                }
            }
        }

        public static async Task<bool> SequenceEqualAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> first, IAsyncEnumerable<ELEMENT_T> second, IEqualityComparer<ELEMENT_T> equalityComparer, CancellationToken cancellationToken = default)
        {
            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));

            var firstEnumerator = first.GetAsyncEnumerator(cancellationToken);
            await using (firstEnumerator.ConfigureAwait(false))
            {
                var secondEnumerator = second.GetAsyncEnumerator(cancellationToken);
                await using (secondEnumerator.ConfigureAwait(false))
                {
                    while (true)
                    {
                        var isOkFirstTask = firstEnumerator.MoveNextAsync().AsTask();
                        var isOkSecondTask = secondEnumerator.MoveNextAsync().AsTask();
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.WhenAll(isOkFirstTask, isOkSecondTask).ConfigureAwait(false);
                        var isOkFirst = isOkFirstTask.Result;
                        var isOkSecond = isOkSecondTask.Result;

                        // 片方のシーケンスだけが終端に達した場合は false を返す。
                        if (isOkFirst != isOkSecond)
                            return false;

                        // 両方のシーケンスが終端に達した場合は true を返す。
                        if (!isOkFirst)
                            return true;

                        // どちらのシーケンスも終端に達していない場合は要素を比較して、異なっている場合は false を返す。
                        if (!equalityComparer.Equals(firstEnumerator.Current, secondEnumerator.Current))
                            return false;
                    }
                }
            }
        }

        public static async Task<bool> SequenceEqualAsync<ELEMENT_T, KEY_T>(this IAsyncEnumerable<ELEMENT_T> first, IAsyncEnumerable<ELEMENT_T> second, Func<ELEMENT_T, KEY_T> keySelecter, CancellationToken cancellationToken = default)
            where KEY_T : IEquatable<KEY_T>
        {
            var firstEnumerator = first.GetAsyncEnumerator(cancellationToken);
            await using (firstEnumerator.ConfigureAwait(false))
            {
                var secondEnumerator = second.GetAsyncEnumerator(cancellationToken);
                await using (secondEnumerator.ConfigureAwait(false))
                {
                    while (true)
                    {
                        var isOkFirstTask = firstEnumerator.MoveNextAsync().AsTask();
                        var isOkSecondTask = secondEnumerator.MoveNextAsync().AsTask();
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.WhenAll(isOkFirstTask, isOkSecondTask).ConfigureAwait(false);
                        var isOkFirst = isOkFirstTask.Result;
                        var isOkSecond = isOkSecondTask.Result;

                        // 片方のシーケンスだけが終端に達した場合は false を返す。
                        if (isOkFirst != isOkSecond)
                            return false;

                        // 両方のシーケンスが終端に達した場合は true を返す。
                        if (!isOkFirst)
                            return true;

                        // どちらのシーケンスも終端に達していない場合は要素を比較して、異なっている場合は false を返す。
                        if (!keySelecter(firstEnumerator.Current).Equals(keySelecter(secondEnumerator.Current)))
                            return false;
                    }
                }
            }
        }

        public static async Task<bool> SequenceEqualAsync<ELEMENT_T, KEY_T>(this IAsyncEnumerable<ELEMENT_T> first, IAsyncEnumerable<ELEMENT_T> second, Func<ELEMENT_T, KEY_T> keySelecter, IEqualityComparer<KEY_T> equalityComparer, CancellationToken cancellationToken = default)
        {
            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));
            if (keySelecter is null)
                throw new ArgumentNullException(nameof(keySelecter));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));

            var firstEnumerator = first.GetAsyncEnumerator(cancellationToken);
            await using (firstEnumerator.ConfigureAwait(false))
            {
                var secondEnumerator = second.GetAsyncEnumerator(cancellationToken);
                await using (secondEnumerator.ConfigureAwait(false))
                {
                    while (true)
                    {
                        var isOkFirstTask = firstEnumerator.MoveNextAsync().AsTask();
                        var isOkSecondTask = secondEnumerator.MoveNextAsync().AsTask();
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.WhenAll(isOkFirstTask, isOkSecondTask).ConfigureAwait(false);
                        var isOkFirst = isOkFirstTask.Result;
                        var isOkSecond = isOkSecondTask.Result;

                        // 片方のシーケンスだけが終端に達した場合は false を返す。
                        if (isOkFirst != isOkSecond)
                            return false;

                        // 両方のシーケンスが終端に達した場合は true を返す。
                        if (!isOkFirst)
                            return true;

                        // どちらのシーケンスも終端に達していない場合は要素を比較して、異なっている場合は false を返す。
                        if (!equalityComparer.Equals(keySelecter(firstEnumerator.Current), keySelecter(secondEnumerator.Current)))
                            return false;
                    }
                }
            }
        }

        #endregion

        #region Single

        public static async Task<ELEMENT_T> SingleAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                // 最初の要素が存在しない場合 (シーケンスが空) は例外
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();

                // 最初の要素を復帰値として取得
                var singleValue = enumerator.Current;

                // 2番目以降の要素が存在した場合 (シーケンスの長さが2以上) は例外
                cancellationToken.ThrowIfCancellationRequested();
                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();

                // 最初の要素を復帰値として返す
                return singleValue;
            }
        }

        public static async Task<ELEMENT_T> SingleAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var (isFoundFirstValue, singleValue) = await enumerator.InternalFirstOrDefaultAsync(predicate, () => throw new InvalidOperationException(), cancellationToken).ConfigureAwait(false);

                // 条件に一致する要素が見つからなかったら既に InvalidOperationException 例外が発生しているはずなので、この時点で見つかっているはず (isFoundFirstValue == true)
#if DEBUG
                if (!isFoundFirstValue)
                    throw new Exception();
#endif
                // 以降のシーケンスに条件に一致する要素が見つかった場合は例外
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    if (predicate(value))
                        throw new InvalidOperationException();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // この時点で、条件に一致する要素が存在して、かつそれが1つだけである。
                return singleValue;
            }
        }

        #endregion

        #region SingleOrDefault

        public static async Task<ELEMENT_T?> SingleOrDefaultAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                // 最初の要素が存在しない場合 (シーケンスが空) は例外
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();

                // 最初の要素を復帰値として取得
                var singleValue = enumerator.Current;

                // 2番目以降の要素が存在した場合 (シーケンスの長さが2以上) は例外
                cancellationToken.ThrowIfCancellationRequested();
                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();

                // 最初の要素を復帰値として返す
                return singleValue;
            }
        }

        public static async Task<ELEMENT_T> SingleOrDefaultAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, ELEMENT_T defaultValue, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    return defaultValue;
                var singleValue = enumerator.Current;
                cancellationToken.ThrowIfCancellationRequested();
                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    throw new InvalidOperationException();
                return singleValue;
            }
        }

        public static async Task<ELEMENT_T?> SingleOrDefaultAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var (isEndOfSequence, singleValue) = await enumerator.InternalFirstOrDefaultAsync(predicate, () => throw new InvalidOperationException(), cancellationToken).ConfigureAwait(false);
                if (!isEndOfSequence)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        var value = enumerator.Current;
                        if (predicate(value))
                            throw new InvalidOperationException();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                return singleValue;
            }
        }

        public static async Task<ELEMENT_T> SingleOrDefaultAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, ELEMENT_T defaultValue, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var (isEndOfSequence, singleValue) = await enumerator.InternalFirstOrDefaultAsync(predicate, () => defaultValue, cancellationToken).ConfigureAwait(false);
                if (!isEndOfSequence)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        var value = enumerator.Current;
                        if (predicate(value))
                            throw new InvalidOperationException();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                return singleValue;
            }
        }

        #endregion

        public static async IAsyncEnumerable<ELEMENT_T> Skip<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Int32 count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    if (count > 0)
                    {
                        checked
                        {
                            --count;
                        }
                    }
                    else
                        yield return enumerator.Current;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public static async IAsyncEnumerable<ELEMENT_T> SkipLast<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Int32 count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var queue = new Queue<ELEMENT_T>();
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    queue.Enqueue(enumerator.Current);
                    if (queue.Count > count)
                        yield return queue.Dequeue();
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        #region SkipWhile

        public static async IAsyncEnumerable<ELEMENT_T> SkipWhile<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    if (!predicate(element))
                    {
                        yield return element;
                        break;
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    yield return enumerator.Current;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public static async IAsyncEnumerable<ELEMENT_T> SkipWhile<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, Int32, bool> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var index = 0;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    if (!predicate(element, checked(index++)))
                    {
                        yield return element;
                        break;
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    yield return enumerator.Current;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        #endregion

        public static async IAsyncEnumerable<ELEMENT_T> Take<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Int32 count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (checked(count--) > 0 && await enumerator.MoveNextAsync())
                {
                    yield return enumerator.Current;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public static async IAsyncEnumerable<ELEMENT_T> TakeLast<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Int32 count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var queue = new Queue<ELEMENT_T>();
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    queue.Enqueue(enumerator.Current);
                    if (queue.Count > count)
                        queue.Dequeue();
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            while (queue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return queue.Dequeue();
            }
        }

        #region TakeWhile

        public static async IAsyncEnumerable<ELEMENT_T> TakeWhile<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    if (!predicate(element))
                        break;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                yield break;
            }
        }

        public static async IAsyncEnumerable<ELEMENT_T> TakeWhile<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, Int32, bool> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                var index = 0;
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var element = enumerator.Current;
                    if (!predicate(element, checked(index++)))
                        break;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                yield break;
            }
        }

        #endregion

        #region ThenBy 

        public static IOrderedAsyncEnumerable<ELEMENT_T> ThenBy<ELEMENT_T, KEY_T>(this IOrderedAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            return new OrderedAsyncEnumerable<ELEMENT_T, KEY_T>(source, keySelector, null, false, source);
        }

        public static IOrderedAsyncEnumerable<ELEMENT_T> ThenBy<ELEMENT_T, KEY_T>(this IOrderedAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector, IComparer<KEY_T> keyComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (keyComparer is null)
                throw new ArgumentNullException(nameof(keyComparer));

            return new OrderedAsyncEnumerable<ELEMENT_T, KEY_T>(source, keySelector, keyComparer, false, source);
        }

        #endregion

        #region ThenByDescending  

        public static IOrderedAsyncEnumerable<ELEMENT_T> ThenByDescending<ELEMENT_T, KEY_T>(this IOrderedAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            return new OrderedAsyncEnumerable<ELEMENT_T, KEY_T>(source, keySelector, null, true, source);
        }

        public static IOrderedAsyncEnumerable<ELEMENT_T> ThenByDescending<ELEMENT_T, KEY_T>(this IOrderedAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector, IComparer<KEY_T>? keyComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (keyComparer is null)
                throw new ArgumentNullException(nameof(keyComparer));

            return new OrderedAsyncEnumerable<ELEMENT_T, KEY_T>(source, keySelector, keyComparer, true, source);
        }

        #endregion

        public static async Task<ELEMENT_T[]> ToArrayAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return (await source.InternalToListAsync(cancellationToken).ConfigureAwait(false)).ToArray();
        }

        #region ToDictionary

        public static async Task<IDictionary<KEY_T, ELEMENT_T>> ToDictionaryAsync<ELEMENT_T, KEY_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector, CancellationToken cancellationToken = default)
            where KEY_T : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            var dictionary = new Dictionary<KEY_T, ELEMENT_T>();
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    var key = keySelector(element);
                    dictionary.Add(key, element);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            return dictionary;
        }

        public static async Task<IDictionary<KEY_T, ELEMENT_T>> ToDictionaryAsync<ELEMENT_T, KEY_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector, IEqualityComparer<KEY_T> keyEqualityComparer, CancellationToken cancellationToken = default)
            where KEY_T : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            var dictionary = new Dictionary<KEY_T, ELEMENT_T>(keyEqualityComparer);
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    var key = keySelector(element);
                    dictionary.Add(key, element);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            return dictionary;
        }

        public static async Task<IDictionary<KEY_T, VALUE_T>> ToDictionaryAsync<ELEMENT_T, KEY_T, VALUE_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector, Func<ELEMENT_T, VALUE_T> valueSelector, CancellationToken cancellationToken = default)
            where KEY_T : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (valueSelector is null)
                throw new ArgumentNullException(nameof(valueSelector));

            var dictionary = new Dictionary<KEY_T, VALUE_T>();
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    var key = keySelector(element);
                    var value = valueSelector(element);
                    dictionary.Add(key, value);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            return dictionary;
        }

        public static async Task<IDictionary<KEY_T, VALUE_T>> ToDictionaryAsync<ELEMENT_T, KEY_T, VALUE_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, KEY_T> keySelector, Func<ELEMENT_T, VALUE_T> valueSelector, IEqualityComparer<KEY_T> keyEqualityComparer, CancellationToken cancellationToken = default)
            where KEY_T : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (valueSelector is null)
                throw new ArgumentNullException(nameof(valueSelector));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            var dictionary = new Dictionary<KEY_T, VALUE_T>(keyEqualityComparer);
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    var key = keySelector(element);
                    var value = valueSelector(element);
                    dictionary.Add(key, value);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            return dictionary;
        }

        #endregion

        public static Task<List<ELEMENT_T>> ToListAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return source.InternalToListAsync(cancellationToken);
        }

        #region ToLookupAsync

        public static Task<ILookup<KEY_T, SOURCE_T>> ToLookupAsync<SOURCE_T, KEY_T>(this IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, CancellationToken cancellationToken = default)
            where KEY_T : notnull, IEquatable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            return
                InternalToLookupAsync(
                    source,
                    keySelector,
                    element => element,
                    new Dictionary<KEY_T, ICollection<SOURCE_T>>(),
                    cancellationToken);
        }

        public static Task<ILookup<KEY_T, SOURCE_T>> ToLookupAsync<SOURCE_T, KEY_T>(this IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, IEqualityComparer<KEY_T> keyEqualityComparer, CancellationToken cancellationToken = default)
            where KEY_T : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            return
                InternalToLookupAsync(
                    source,
                    keySelector,
                    element => element,
                    new Dictionary<KEY_T, ICollection<SOURCE_T>>(keyEqualityComparer),
                    cancellationToken);
        }

        public static Task<ILookup<KEY_T, ELEMENT_T>> ToLookupAsync<SOURCE_T, KEY_T, ELEMENT_T>(this IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, Func<SOURCE_T, ELEMENT_T> elementSelector, CancellationToken cancellationToken = default)
            where KEY_T : notnull, IEquatable<KEY_T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector is null)
                throw new ArgumentNullException(nameof(elementSelector));

            return
                InternalToLookupAsync(
                    source,
                    keySelector,
                    elementSelector,
                    new Dictionary<KEY_T, ICollection<ELEMENT_T>>(),
                    cancellationToken);
        }

        public static Task<ILookup<KEY_T, ELEMENT_T>> ToLookupAsync<SOURCE_T, KEY_T, ELEMENT_T>(this IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, Func<SOURCE_T, ELEMENT_T> elementSelector, IEqualityComparer<KEY_T> keyEqualityComparer, CancellationToken cancellationToken = default)
            where KEY_T : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector is null)
                throw new ArgumentNullException(nameof(elementSelector));
            if (keyEqualityComparer is null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            return
                InternalToLookupAsync(
                    source,
                    keySelector,
                    elementSelector,
                    new Dictionary<KEY_T, ICollection<ELEMENT_T>>(keyEqualityComparer),
                    cancellationToken);
        }

        #endregion

        public static async Task<IReadOnlyCollection<ELEMENT_T>> ToReadOnlyCollectionAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return (await source.InternalToListAsync(cancellationToken)).AsReadOnly();
        }

        public static async Task<ReadOnlyMemory<ELEMENT_T>> ToReadOnlyMemoryAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return (await source.InternalToListAsync(cancellationToken)).ToArray().AsMemory();
        }

        public static async IAsyncEnumerable<ELEMENT_T> Where<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, bool> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    if (predicate(element))
                        yield return element;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public static async IAsyncEnumerable<ELEMENT_T> Where<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, Func<ELEMENT_T, Int32, bool> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var index = 0;
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    if (predicate(element, checked(index++)))
                        yield return element;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public static async IAsyncEnumerable<ELEMENT_T> WhereNotNull<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T?> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    if (element is not null)
                        yield return element;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        #region Zip

        public static async IAsyncEnumerable<RESULT_T> Zip<FIRST_T, SECOND_T, RESULT_T>(this IAsyncEnumerable<FIRST_T> first, IAsyncEnumerable<SECOND_T> second, Func<FIRST_T, SECOND_T, RESULT_T> resultSelector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));
            if (resultSelector is null)
                throw new ArgumentNullException(nameof(resultSelector));

            var firstEnumerator = first.GetAsyncEnumerator(cancellationToken);
            await using (firstEnumerator.ConfigureAwait(false))
            {
                var secondEnumerator = second.GetAsyncEnumerator(cancellationToken);
                await using (secondEnumerator.ConfigureAwait(false))
                {
                    while (true)
                    {
                        var isOkFirstTask = firstEnumerator.MoveNextAsync().AsTask();
                        var isOkSecondTask = secondEnumerator.MoveNextAsync().AsTask();
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.WhenAll(isOkFirstTask, isOkSecondTask).ConfigureAwait(false);
                        if (!isOkFirstTask.Result || !isOkSecondTask.Result)
                            break;
                        var element = firstEnumerator.Current;
                        if (element is not null)
                            yield return resultSelector(firstEnumerator.Current, secondEnumerator.Current);
                    }
                }
            }
        }

        public static async IAsyncEnumerable<(FIRST_T First, SECOND_T Second)> Zip<FIRST_T, SECOND_T>(this IAsyncEnumerable<FIRST_T> first, IAsyncEnumerable<SECOND_T> second, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));

            var firstEnumerator = first.GetAsyncEnumerator(cancellationToken);
            await using (firstEnumerator.ConfigureAwait(false))
            {
                var secondEnumerator = second.GetAsyncEnumerator(cancellationToken);
                await using (secondEnumerator.ConfigureAwait(false))
                {
                    while (true)
                    {
                        var isOkFirstTask = firstEnumerator.MoveNextAsync().AsTask();
                        var isOkSecondTask = secondEnumerator.MoveNextAsync().AsTask();
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.WhenAll(isOkFirstTask, isOkSecondTask).ConfigureAwait(false);
                        if (!isOkFirstTask.Result || !isOkSecondTask.Result)
                            break;
                        yield return (firstEnumerator.Current, secondEnumerator.Current);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }
        }

        public static async IAsyncEnumerable<(FIRST_T First, SECOND_T Second, THIRD_T Third)> Zip<FIRST_T, SECOND_T, THIRD_T>(this IAsyncEnumerable<FIRST_T> first, IAsyncEnumerable<SECOND_T> second, IAsyncEnumerable<THIRD_T> third, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));
            if (third is null)
                throw new ArgumentNullException(nameof(third));

            var firstEnumerator = first.GetAsyncEnumerator(cancellationToken);
            await using (firstEnumerator.ConfigureAwait(false))
            {
                var secondEnumerator = second.GetAsyncEnumerator(cancellationToken);
                await using (secondEnumerator.ConfigureAwait(false))
                {
                    var thirdEnumerator = third.GetAsyncEnumerator(cancellationToken);
                    await using (secondEnumerator.ConfigureAwait(false))
                    {
                        while (true)
                        {
                            var isOkFirstTask = firstEnumerator.MoveNextAsync().AsTask();
                            var isOkSecondTask = secondEnumerator.MoveNextAsync().AsTask();
                            var isOkThirdTask = thirdEnumerator.MoveNextAsync().AsTask();
                            cancellationToken.ThrowIfCancellationRequested();
                            await Task.WhenAll(isOkFirstTask, isOkSecondTask, isOkThirdTask).ConfigureAwait(false);
                            if (!isOkFirstTask.Result || !isOkSecondTask.Result || !isOkThirdTask.Result)
                                break;
                            yield return (firstEnumerator.Current, secondEnumerator.Current, thirdEnumerator.Current);
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                }
            }
        }

        #endregion

        private static async Task<(bool IsFoundFirstValue, ELEMENT_T FirstValue)> InternalFirstOrDefaultAsync<ELEMENT_T>(this IAsyncEnumerator<ELEMENT_T> enumerator, Func<ELEMENT_T, bool> predicate, Func<ELEMENT_T> defaultGettet, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                var value = enumerator.Current;
                if (predicate(value))
                    return (true, value);
                cancellationToken.ThrowIfCancellationRequested();
            }
            return (false, defaultGettet());
        }

        #region InternalGroupBy

        private static IAsyncEnumerable<RESULT_T> InternalGroupBy<SOURCE_T, KEY_T, ELEMENT_T, RESULT_T>(IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, Func<SOURCE_T, ELEMENT_T> elementSelector, Func<KEY_T, IEnumerable<ELEMENT_T>, RESULT_T> resultSelector, CancellationToken cancellationToken)
            where KEY_T : notnull, IEqualityComparer<KEY_T>
        {
            return
                InternalGroupBy(
                    new Dictionary<KEY_T, ICollection<ELEMENT_T>>(),
                    source,
                    keySelector,
                    elementSelector,
                    resultSelector,
                    cancellationToken);
        }

        private static IAsyncEnumerable<RESULT_T> InternalGroupBy<SOURCE_T, KEY_T, ELEMENT_T, RESULT_T>(IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, Func<SOURCE_T, ELEMENT_T> elementSelector, Func<KEY_T, IEnumerable<ELEMENT_T>, RESULT_T> resultSelector, IEqualityComparer<KEY_T> keyEqualityComparer, CancellationToken cancellationToken)
            where KEY_T : notnull
        {
            return
                InternalGroupBy(
                    new Dictionary<KEY_T, ICollection<ELEMENT_T>>(keyEqualityComparer),
                    source,
                    keySelector,
                    elementSelector,
                    resultSelector,
                    cancellationToken);
        }

        private static async IAsyncEnumerable<RESULT_T> InternalGroupBy<SOURCE_T, KEY_T, ELEMENT_T, RESULT_T>(IDictionary<KEY_T, ICollection<ELEMENT_T>> groups, IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, Func<SOURCE_T, ELEMENT_T> elementSelector, Func<KEY_T, IEnumerable<ELEMENT_T>, RESULT_T> resultSelector, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var sourceElement = enumerator.Current;
                    var key = keySelector(sourceElement);
                    if (!groups.TryGetValue(key, out ICollection<ELEMENT_T>? collection))
                    {
                        collection = new List<ELEMENT_T>();
                        groups.Add(key, collection);
                    }
                    collection.Add(elementSelector(sourceElement));
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            foreach (var group in groups)
                yield return resultSelector(group.Key, group.Value);
        }

        #endregion

        private static async Task<List<ELEMENT_T>> InternalToListAsync<ELEMENT_T>(this IAsyncEnumerable<ELEMENT_T> source, CancellationToken cancellationToken)
        {
            var elements = new List<ELEMENT_T>();
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    elements.Add(enumerator.Current);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            return elements;
        }

        private static async Task<ILookup<KEY_T, ELEMENT_T>> InternalToLookupAsync<SOURCE_T, KEY_T, ELEMENT_T>(IAsyncEnumerable<SOURCE_T> source, Func<SOURCE_T, KEY_T> keySelector, Func<SOURCE_T, ELEMENT_T> elementSelector, Dictionary<KEY_T, ICollection<ELEMENT_T>> dictionary, CancellationToken cancellationToken) where KEY_T : notnull
        {
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var element = enumerator.Current;
                    var key = keySelector(element);
                    if (!dictionary.TryGetValue(key, out var collection))
                    {
                        collection = new List<ELEMENT_T>();
                        dictionary.Add(key, collection);
                    }
                    collection.Add(elementSelector(element));
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            return new Lookup<KEY_T, ELEMENT_T>(dictionary);
        }
    }
}

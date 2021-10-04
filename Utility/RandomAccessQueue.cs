using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utility
{
    public class RandomAccessQueue<ELEMENT_T>
        : IReadOnlyArray<ELEMENT_T>, ICloneable<RandomAccessQueue<ELEMENT_T>>, IEquatable<RandomAccessQueue<ELEMENT_T>>
        where ELEMENT_T : IEquatable<ELEMENT_T>
    {
        private SortedDictionary<ulong, ELEMENT_T> _queue;
        private ulong _indexOfStart;
        private ulong _indexOfEnd;

        public RandomAccessQueue()
        {
            _queue = new SortedDictionary<ulong, ELEMENT_T>();
            _indexOfStart = 0;
            _indexOfEnd = 0;
        }

        public RandomAccessQueue(IEnumerable<ELEMENT_T> dataSource)
            : this()
        {
            foreach (var value in dataSource)
            {
                _queue.Add(_indexOfEnd, value);
                ++_indexOfEnd;
            }
            Normalize();
#if DEBUG
            Check();
#endif
        }

        public void Clear()
        {
            _queue.Clear();
            _indexOfStart = 0;
            _indexOfEnd = 0;
        }

        public void Enqueue(ELEMENT_T value)
        {
            _queue.Add(_indexOfEnd, value);
            ++_indexOfEnd;
            Normalize();
#if DEBUG
            Check();
#endif
        }

        public ELEMENT_T Dequeue()
        {
            if (_queue.Count <= 0)
                throw new InvalidOperationException();
            var value = _queue[_indexOfStart];
            _queue.Remove(_indexOfStart);
            ++_indexOfStart;
            Normalize();
#if DEBUG
            Check();
#endif
            return value;
        }

        public ELEMENT_T this[int index]
        {
            get
            {
                if (index < 0)
                    throw new IndexOutOfRangeException("'index' is a negative value.");
                try
                {
                    checked
                    {
                        return _queue[_indexOfStart + (uint)index];

                    }
                }
                catch (OverflowException ex)
                {
                    throw new IndexOutOfRangeException("'index' exceeds the upper limit.", ex);
                }
                catch (KeyNotFoundException ex)
                {
                    throw new IndexOutOfRangeException("'index' exceeds the upper limit.", ex);
                }
            }
        }
        public int Length => _queue.Count;
        public int Count => _queue.Count;
        public IEnumerator<ELEMENT_T> GetEnumerator() => _queue.Values.GetEnumerator();
        public ELEMENT_T[] DuplicateAsWritableArray() => _queue.Values.ToArray();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [Obsolete]
        ELEMENT_T[] IReadOnlyArray<ELEMENT_T>.ToArray() => throw new NotSupportedException();


        public void CopyTo(ELEMENT_T[] destinationArray, int destinationOffset)
        {
            if (destinationArray == null)
                throw new ArgumentNullException();
            if (destinationOffset < 0)
                throw new IndexOutOfRangeException();
            if (destinationOffset > destinationArray.Length)
                throw new IndexOutOfRangeException();
            if (destinationArray.Length - destinationOffset > _queue.Count)
                throw new IndexOutOfRangeException();
            InternalCopyTo(0, destinationArray, destinationOffset, destinationArray.Length - destinationOffset);
        }

        public void CopyTo(int sourceOffset, ELEMENT_T[] destinationArray, int destinationOffset, int count)
        {
            if (sourceOffset < 0)
                throw new IndexOutOfRangeException();
            if (destinationArray == null)
                throw new ArgumentNullException();
            if (destinationOffset < 0)
                throw new IndexOutOfRangeException();
            if (count < 0)
                throw new IndexOutOfRangeException();
            if (sourceOffset + count > _queue.Count)
                throw new IndexOutOfRangeException();
            if (destinationOffset + count > destinationArray.Length)
                throw new IndexOutOfRangeException();
            InternalCopyTo(sourceOffset, destinationArray, destinationOffset, count);
        }

        public RandomAccessQueue<ELEMENT_T> Clone()
        {
            return new RandomAccessQueue<ELEMENT_T>(_queue.Values);
        }

        public bool Equals(RandomAccessQueue<ELEMENT_T> other)
        {
            if (other == null)
                return false;
            if (_queue.Count != other._queue.Count)
                return false;
            if (_queue.Values.SequenceEqual(other._queue.Values) == false)
                return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            return Equals((RandomAccessQueue<ELEMENT_T>)obj);
        }

        public override int GetHashCode()
        {
            return _queue.Values.Aggregate(0, (hashCode, value) => hashCode ^ value.GetHashCode());
        }

        private void InternalCopyTo(int sourceOffset, ELEMENT_T[] destinationArray, int destinationOffset, int count)
        {
#if DEBUG
            if (sourceOffset < 0)
                throw new Exception();
            if (destinationOffset < 0)
                throw new Exception();
            if (count < 0)
                throw new Exception();
#endif
            for (var index = 0; index < count; ++index)
                destinationArray[destinationOffset + index] = _queue[_indexOfStart + (uint)sourceOffset + (uint)index];
        }

        private void Normalize()
        {
            if (_queue.Count <= 0)
            {
                _indexOfStart = 0;
                _indexOfEnd = 0;
            }

            // _indexOfStart のオーバーフロー対策
            // ある程度 _indexOfStart が大きくなったら _queue を再構築する
            if (_indexOfStart > UInt32.MaxValue || _indexOfEnd > UInt32.MaxValue)
            {
                if (_queue.Count > 0)
                {
                    var firstKey = _queue.Keys.First();
                    foreach (var item in _queue)
                    {
#if DEBUG
                        if (item.Key < firstKey)
                            throw new Exception();
#endif
                        _queue.Remove(item.Key);
                        _queue[item.Key - firstKey] = item.Value;
                    }
                }
                _indexOfStart = 0;
                _indexOfEnd = (uint)_queue.Count;
            }
        }

#if DEBUG
        private void Check()
        {
            if (_queue.Count > 0)
            {
                if (_queue.Keys.First() != _indexOfStart)
                    throw new Exception();
                if (_queue.Keys.Last() + 1 != _indexOfEnd)
                    throw new Exception();
                checked
                {
                    if ((uint)_queue.Count != _indexOfEnd - _indexOfStart)
                        throw new Exception();
                }
            }
            else
            {
                if (_indexOfStart != 0)
                    throw new Exception();
                if (_indexOfEnd != 0)
                    throw new Exception();
            }
        }
#endif
    }


}

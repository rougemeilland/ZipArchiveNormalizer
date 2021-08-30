using System;

namespace Utility
{
    class CustomizableComparer<VALUE_T>
        : IUniversalComparer<VALUE_T>
    {
        private Func<VALUE_T, VALUE_T, int> _comparer;
        private Func<VALUE_T, VALUE_T, bool> _equalityComparer;
        private Func<VALUE_T, int> _hashCalculater;

        public CustomizableComparer(Func<VALUE_T, VALUE_T, int> comparer, Func<VALUE_T, VALUE_T, bool> equalityComparer, Func<VALUE_T, int> hashCalculater)
        {
            _comparer = comparer;
            _equalityComparer = equalityComparer;
            _hashCalculater = hashCalculater;
        }

        public int Compare(VALUE_T x, VALUE_T y)
        {
            return _comparer(x, y);
        }

        public bool Equals(VALUE_T x, VALUE_T y)
        {
            return _equalityComparer(x, y);
        }

        public int GetHashCode(VALUE_T obj)
        {
            return _hashCalculater(obj);
        }
    }
}
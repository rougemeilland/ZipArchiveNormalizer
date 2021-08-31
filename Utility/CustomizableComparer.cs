using System;

namespace Utility
{
    public class CustomizableComparer<VALUE_T>
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
            if (x == null)
                return y == null ? 0 : -1;
            else if (y == null)
                return 1;
            else
                return _comparer(x, y);
        }

        public bool Equals(VALUE_T x, VALUE_T y)
        {
            if (x == null)
                return y == null;
            else if (y == null)
                return false;
            else
                return _equalityComparer(x, y);
        }

        public int GetHashCode(VALUE_T obj)
        {
            return obj == null ? 0 : _hashCalculater(obj);
        }
    }
}
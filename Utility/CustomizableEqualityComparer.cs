using System;
using System.Collections.Generic;

namespace Utility
{
    class CustomizableEqualityComparer<VALUE_T>
        : IEqualityComparer<VALUE_T>
    {
        private Func<VALUE_T, VALUE_T, bool> _equalityComparer;
        private Func<VALUE_T, int> _hashCalculater;

        public CustomizableEqualityComparer(Func<VALUE_T, VALUE_T, bool> equalityComparer, Func<VALUE_T, int> hashCalculater)
        {
            _equalityComparer = equalityComparer;
            _hashCalculater = hashCalculater;
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
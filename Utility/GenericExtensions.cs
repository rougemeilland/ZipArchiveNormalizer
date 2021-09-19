using System;

namespace Utility
{
    public static class GenericExtensions
    {
        public static bool IsBetween<VALUE_T>(this VALUE_T value, VALUE_T lowerValue, VALUE_T upperValue)
            where VALUE_T : IComparable<VALUE_T>
        {
            if (value == null)
                return lowerValue == null;
            else
                return value.CompareTo(lowerValue) >= 0 && value.CompareTo(upperValue) <= 0;
        }

        public static bool IsNoneOf<VALUE_T>(this VALUE_T value, VALUE_T value1, VALUE_T value2)
            where VALUE_T : IEquatable<VALUE_T>
        {
            return !value.IsAnyOf(value1, value2);
        }

        public static bool IsNoneOf<VALUE_T>(this VALUE_T value, VALUE_T value1, VALUE_T value2, VALUE_T value3)
            where VALUE_T : IEquatable<VALUE_T>
        {
            return !value.IsAnyOf(value1, value2, value3);
        }

        public static bool IsNoneOf<VALUE_T>(this VALUE_T value, VALUE_T value1, VALUE_T value2, VALUE_T value3, VALUE_T value4)
            where VALUE_T : IEquatable<VALUE_T>
        {
            return !value.IsAnyOf(value1, value2, value3, value4);
        }

        public static bool IsAnyOf<VALUE_T>(this VALUE_T value, VALUE_T value1, VALUE_T value2)
            where VALUE_T : IEquatable<VALUE_T>
        {
            if (value == null)
                return value1 == null || value2 == null;
            else
                return value.Equals(value1) || value.Equals(value2);
        }

        public static bool IsAnyOf<VALUE_T>(this VALUE_T value, VALUE_T value1, VALUE_T value2, VALUE_T value3)
            where VALUE_T : IEquatable<VALUE_T>
        {
            if (value == null)
                return value1 == null || value2 == null || value3 == null;
            else
                return value.Equals(value1) || value.Equals(value2) || value.Equals(value3);
        }

        public static bool IsAnyOf<VALUE_T>(this VALUE_T value, VALUE_T value1, VALUE_T value2, VALUE_T value3, VALUE_T value4)
            where VALUE_T : IEquatable<VALUE_T>
        {
            if (value == null)
                return value1 == null || value2 == null || value3 == null || value4 == null;
            else
                return value.Equals(value1) || value.Equals(value2) || value.Equals(value3) || value.Equals(value4);
        }

        public static VALUE_T Minimum<VALUE_T>(this VALUE_T x, VALUE_T y)
            where VALUE_T : IComparable<VALUE_T>
        {
            if (x == null)
                return x;
            else if (x.CompareTo(y) > 0)
                return y;
            else
                return x;
        }

        public static VALUE_T Maximum<VALUE_T>(this VALUE_T x, VALUE_T y)
            where VALUE_T : IComparable<VALUE_T>
        {
            if (x == null)
                return y;
            else if (x.CompareTo(y) > 0)
                return x;
            else
                return y;
        }

        public static VALUE_T Duplicate<VALUE_T>(this VALUE_T value)
            where VALUE_T: ICloneable<VALUE_T>
        {
            if (value == null)
                throw new ArgumentNullException();
            return value.Clone();
        }
    }
}

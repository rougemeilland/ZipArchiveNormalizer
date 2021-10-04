using System;

namespace Utility
{
    public static class GenericExtensions
    {
        public static bool IsBetween<VALUE1_T, VALUE2_T>(this VALUE1_T value, VALUE2_T lowerValue, VALUE2_T upperValue)
            where VALUE1_T : IComparable<VALUE2_T>
        {
            if (value == null)
                return lowerValue == null;
            else
                return value.CompareTo(lowerValue) >= 0 && value.CompareTo(upperValue) <= 0;
        }

        public static bool IsNoneOf<VALUE1_T, VALUE2_T>(this VALUE1_T value, VALUE2_T value1, VALUE2_T value2)
            where VALUE1_T : IEquatable<VALUE2_T>
        {
            if (value == null)
                return value1 != null && value2 != null;
            else
                return !value.Equals(value1) && !value.Equals(value2);
        }

        public static bool IsNoneOf<VALUE1_T, VALUE2_T>(this VALUE1_T value, VALUE2_T value1, VALUE2_T value2, VALUE2_T value3)
            where VALUE1_T : IEquatable<VALUE2_T>
        {
            if (value == null)
                return value1 != null && value2 != null && value3 != null;
            else
                return !value.Equals(value1) && !value.Equals(value2) && !value.Equals(value3);
        }

        public static bool IsNoneOf<VALUE1_T, VALUE2_T>(this VALUE1_T value, VALUE2_T value1, VALUE2_T value2, VALUE2_T value3, VALUE2_T value4)
            where VALUE1_T : IEquatable<VALUE2_T>
        {
            if (value == null)
                return value1 != null && value2 != null && value3 != null && value4 != null;
            else
                return !value.Equals(value1) && !value.Equals(value2) && !value.Equals(value3) && !value.Equals(value4);
        }

        public static bool IsNoneOf<VALUE1_T, VALUE2_T>(this VALUE1_T value, VALUE2_T value1, VALUE2_T value2, VALUE2_T value3, VALUE2_T value4, VALUE2_T value5)
            where VALUE1_T : IEquatable<VALUE2_T>
        {
            if (value == null)
                return value1 != null && value2 != null && value3 != null && value4 != null && value5 != null;
            else
                return !value.Equals(value1) && !value.Equals(value2) && !value.Equals(value3) && !value.Equals(value4) && !value.Equals(value5);
        }

        public static bool IsNoneOf<VALUE1_T, VALUE2_T>(this VALUE1_T value, VALUE2_T value1, VALUE2_T value2, VALUE2_T value3, VALUE2_T value4, VALUE2_T value5, VALUE2_T value6)
            where VALUE1_T : IEquatable<VALUE2_T>
        {
            if (value == null)
                return value1 != null && value2 != null && value3 != null && value4 != null && value5 != null && value6 != null;
            else
                return !value.Equals(value1) && !value.Equals(value2) && !value.Equals(value3) && !value.Equals(value4) && !value.Equals(value5) && !value.Equals(value6);
        }

        public static bool IsAnyOf<VALUE1_T, VALUE2_T>(this VALUE1_T value, VALUE2_T value1, VALUE2_T value2)
            where VALUE1_T : IEquatable<VALUE2_T>
        {
            if (value == null)
                return value1 == null || value2 == null;
            else
                return value.Equals(value1) || value.Equals(value2);
        }

        public static bool IsAnyOf<VALUE1_T, VALUE2_T>(this VALUE1_T value, VALUE2_T value1, VALUE2_T value2, VALUE2_T value3)
            where VALUE1_T : IEquatable<VALUE2_T>
        {
            if (value == null)
                return value1 == null || value2 == null || value3 == null;
            else
                return value.Equals(value1) || value.Equals(value2) || value.Equals(value3);
        }

        public static bool IsAnyOf<VALUE1_T, VALUE2_T>(this VALUE1_T value, VALUE2_T value1, VALUE2_T value2, VALUE2_T value3, VALUE2_T value4)
            where VALUE1_T : IEquatable<VALUE2_T>
        {
            if (value == null)
                return value1 == null || value2 == null || value3 == null || value4 == null;
            else
                return value.Equals(value1) || value.Equals(value2) || value.Equals(value3) || value.Equals(value4);
        }

        public static bool IsAnyOf<VALUE1_T, VALUE2_T>(this VALUE1_T value, VALUE2_T value1, VALUE2_T value2, VALUE2_T value3, VALUE2_T value4, VALUE2_T value5)
            where VALUE1_T : IEquatable<VALUE2_T>
        {
            if (value == null)
                return value1 == null || value2 == null || value3 == null || value4 == null || value5 == null;
            else
                return value.Equals(value1) || value.Equals(value2) || value.Equals(value3) || value.Equals(value4) || value.Equals(value5);
        }

        public static bool IsAnyOf<VALUE1_T, VALUE2_T>(this VALUE1_T value, VALUE2_T value1, VALUE2_T value2, VALUE2_T value3, VALUE2_T value4, VALUE2_T value5, VALUE2_T value6)
            where VALUE1_T : IEquatable<VALUE2_T>
        {
            if (value == null)
                return value1 == null || value2 == null || value3 == null || value4 == null || value5 == null || value6 == null;
            else
                return value.Equals(value1) || value.Equals(value2) || value.Equals(value3) || value.Equals(value4) || value.Equals(value5) || value.Equals(value6);
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

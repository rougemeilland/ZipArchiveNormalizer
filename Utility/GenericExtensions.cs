using System;

namespace Utility
{
    public static class GenericExtensions
    {
        public static bool IsBetween<ELEMENT_T>(this ELEMENT_T value, ELEMENT_T lowerValue, ELEMENT_T upperValue)
            where ELEMENT_T : IComparable<ELEMENT_T>
        {
            if (value == null)
                return lowerValue == null;
            else
                return value.CompareTo(lowerValue) >= 0 && value.CompareTo(upperValue) <= 0;
        }

        public static bool IsNoneOf<ELEMENT_T>(this ELEMENT_T value, ELEMENT_T value1, ELEMENT_T value2)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            return !value.IsAnyOf(value1, value2);
        }

        public static bool IsNoneOf<ELEMENT_T>(this ELEMENT_T value, ELEMENT_T value1, ELEMENT_T value2, ELEMENT_T value3)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            return !value.IsAnyOf(value1, value2, value3);
        }

        public static bool IsNoneOf<ELEMENT_T>(this ELEMENT_T value, ELEMENT_T value1, ELEMENT_T value2, ELEMENT_T value3, ELEMENT_T value4)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            return !value.IsAnyOf(value1, value2, value3, value4);
        }

        public static bool IsAnyOf<ELEMENT_T>(this ELEMENT_T value, ELEMENT_T value1, ELEMENT_T value2)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (value == null)
                return value1 == null || value2 == null;
            else
                return value.Equals(value1) || value.Equals(value2);
        }

        public static bool IsAnyOf<ELEMENT_T>(this ELEMENT_T value, ELEMENT_T value1, ELEMENT_T value2, ELEMENT_T value3)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (value == null)
                return value1 == null || value2 == null || value3 == null;
            else
                return value.Equals(value1) || value.Equals(value2) || value.Equals(value3);
        }

        public static bool IsAnyOf<ELEMENT_T>(this ELEMENT_T value, ELEMENT_T value1, ELEMENT_T value2, ELEMENT_T value3, ELEMENT_T value4)
            where ELEMENT_T : IEquatable<ELEMENT_T>
        {
            if (value == null)
                return value1 == null || value2 == null || value3 == null || value4 == null;
            else
                return value.Equals(value1) || value.Equals(value2) || value.Equals(value3) || value.Equals(value4);
        }
    }
}

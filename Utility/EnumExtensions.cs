using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Utility
{
    public static class EnumExtensions
    {
        private static IDictionary<Type, int> _typeSizes;

        static EnumExtensions()
        {
            _typeSizes = new[]
            {
                new { type = typeof(byte), size=sizeof(byte) },
                new { type = typeof(sbyte), size=sizeof(sbyte ) },
                new { type = typeof(short), size=sizeof(short) },
                new { type = typeof(ushort), size=sizeof(ushort) },
                new { type = typeof(int), size=sizeof(int) },
                new { type = typeof(uint), size=sizeof(uint) },
                new { type = typeof(long), size=sizeof(long) },
                new { type = typeof(ulong), size=sizeof(ulong) },
            }
            .ToDictionary(item => item.type, item => item.size);
        }

        public static bool IsNoneOf<ELEMENT_T>(this ELEMENT_T value, ELEMENT_T value1, ELEMENT_T value2)
            where ELEMENT_T : Enum
        {
            return !value.IsAnyOf(value1, value2);
        }

        public static bool IsNoneOf<ELEMENT_T>(this ELEMENT_T value, ELEMENT_T value1, ELEMENT_T value2, ELEMENT_T value3)
            where ELEMENT_T : Enum
        {
            return !value.IsAnyOf(value1, value2, value3);
        }

        public static bool IsNoneOf<ELEMENT_T>(this ELEMENT_T value, ELEMENT_T value1, ELEMENT_T value2, ELEMENT_T value3, ELEMENT_T value4)
            where ELEMENT_T : Enum
        {
            return !value.IsAnyOf(value1, value2, value3, value4);
        }

        public static bool IsAnyOf<ELEMENT_T>(this ELEMENT_T value, ELEMENT_T value1, ELEMENT_T value2)
            where ELEMENT_T : Enum
        {
            if (value == null)
                return value1 == null || value2 == null;
            else
                return value.Equals(value1) || value.Equals(value2);
        }

        public static bool IsAnyOf<ELEMENT_T>(this ELEMENT_T value, ELEMENT_T value1, ELEMENT_T value2, ELEMENT_T value3)
            where ELEMENT_T : Enum
        {
            if (value == null)
                return value1 == null || value2 == null || value3 == null;
            else
                return value.Equals(value1) || value.Equals(value2) || value.Equals(value3);
        }

        public static bool IsAnyOf<ELEMENT_T>(this ELEMENT_T value, ELEMENT_T value1, ELEMENT_T value2, ELEMENT_T value3, ELEMENT_T value4)
            where ELEMENT_T : Enum
        {
            if (value == null)
                return value1 == null || value2 == null || value3 == null || value4 == null;
            else
                return value.Equals(value1) || value.Equals(value2) || value.Equals(value3) || value.Equals(value4);
        }
    }
}

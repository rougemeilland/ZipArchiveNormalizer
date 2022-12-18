using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace Utility.Numerics
{
    public readonly struct UBigInt
        : IComparable, IComparable<BigInt>, IComparable<UBigInt>, IComparable<BigInteger>, IComparable<Int64>, IComparable<UInt64>, IComparable<Int32>, IComparable<UInt32>, IEquatable<BigInt>, IEquatable<UBigInt>, IEquatable<BigInteger>, IEquatable<Int64>, IEquatable<UInt64>, IEquatable<Int32>, IEquatable<UInt32>, IFormattable, IBigIntInternalValue
    {
        private readonly BigInteger _value;

        #region constructor

        static UBigInt()
        {
            One = new UBigInt(BigInteger.One);
            Zero = new UBigInt(BigInteger.Zero);
        }

        public UBigInt()
            : this(BigInteger.Zero)
        {
        }

        public UBigInt(Int32 value)
            : this(new BigInteger(value))
        {
        }

        public UBigInt(UInt32 value)
            : this(new BigInteger(value))
        {
        }

        public UBigInt(Int64 value)
            : this(new BigInteger(value))
        {
        }

        public UBigInt(UInt64 value)
            : this(new BigInteger(value))
        {
        }

        public UBigInt(BigInteger value)
        {
            if (value < 0)
                throw new OverflowException();

            _value = value;
        }

        public UBigInt(Single value)
            : this(new BigInteger(value))
        {
        }

        public UBigInt(Double value)
            : this(new BigInteger(value))
        {
        }

        public UBigInt(Decimal value)
            : this(new BigInteger(value))
        {
        }

        public UBigInt(ReadOnlyMemory<byte> value, bool isBigEndian = false)
            : this(new BigInteger(value.Span, true, isBigEndian))
        {
        }

        public UBigInt(ReadOnlySpan<byte> value, bool isBigEndian = false)
            : this(new BigInteger(value, true, isBigEndian))
        {
        }

        #endregion

        #region properties

        public static UBigInt Zero { get; }
        public static UBigInt One { get; }
        BigInteger IBigIntInternalValue.Value => _value;

        #endregion

        public UBigInt Add(UBigInt other) => new(_value + other._value);
        public UBigInt Subtract(UBigInt other) => new(_value - other._value);
        public UBigInt Multiply(UBigInt other) => new(_value * other._value);
        public UBigInt Divide(UBigInt other) => new(_value / other._value);

        #region Remainder

        public UBigInt Remainder(UBigInt other) => new(_value % other._value);
        public UInt64 Remainder(UInt64 other) => (UInt64)(_value % other);
        public UInt32 Remainder(UInt32 other) => (UInt32)(_value % other);

        #endregion

        #region Modulo

        public UBigInt Modulo(UBigInt other) => new(_value.Modulo(other._value));
        public UInt64 Modulo(UInt64 other) => _value.Modulo(other);
        public UInt32 Modulo(UInt32 other) => _value.Modulo(other);

        #endregion

        #region DivRem

        public (UBigInt Quotient, UBigInt Remainder) DivRem(UBigInt divisor)
        {
            var result = _value.DivRem(divisor._value);
            return (new UBigInt(result.Quotient), new UBigInt(result.Remainder));
        }

        public (UBigInt Quotient, UInt64 Remainder) DivRem(UInt64 divisor)
        {
            var result = _value.DivRem(divisor);
#if DEBUG
            checked
#endif
            {
                return (new UBigInt(result.Quotient), (UInt64)result.Remainder);
            }
        }

        public (UBigInt Quotient, UInt32 Remainder) DivRem(UInt32 divisor)
        {
            var result = _value.DivRem(divisor);
#if DEBUG
            checked
#endif
            {
                return (new UBigInt(result.Quotient), (UInt32)result.Remainder);
            }
        }

        #endregion

        #region DivMod

        public (UBigInt Quotient, UBigInt Remainder) DivMod(UBigInt divisor)
        {
            var result = _value.DivMod(divisor._value);
#if DEBUG
            checked
#endif
            {
                return (new UBigInt(result.Quotient), new UBigInt(result.Modulo));
            }
        }

        public (UBigInt Quotient, UInt64 Remainder) DivMod(UInt64 divisor)
        {
            var result = _value.DivMod(divisor);
#if DEBUG
            checked
#endif
            {
                return (new UBigInt(result.Quotient), result.Modulo);
            }
        }

        public (UBigInt Quotient, UInt32 Remainder) DivMod(UInt32 divisor)
        {
            var result = _value.DivMod(divisor);
#if DEBUG
            checked
#endif
            {
                return (new UBigInt(result.Quotient), result.Modulo);
            }
        }

        #endregion

        public UBigInt Xor(UBigInt other) => new(_value ^ other._value);

        #region BitwiseAnd

        public UBigInt BitwiseAnd(UBigInt other) => new(_value & other._value);
        public UInt64 BitwiseAnd(UInt64 other) => (UInt64)(_value & other);
        public UInt32 BitwiseAnd(UInt32 other) => (UInt32)(_value & other);

        #endregion

        public UBigInt BitwiseOr(UBigInt other) => new(_value | other._value);
        public UBigInt LeftShift(Int32 shiftCount) => new(_value << shiftCount);
        public UBigInt RightShift(Int32 shiftCount) => new(_value >> shiftCount);
        public UBigInt Decrement() => new(_value - 1);
        public UBigInt Increment() => new(_value + 1);
        public BigInt Negate() => new(BigInteger.Negate(_value));
        public UBigInt Plus() => this;
        public BigInt OnesComplement() => new(~_value);
        public Int64 GetBitLength() => _value.GetBitLength();
        public Int32 GetByteCount() => _value.GetByteCount(false);
        public UBigInt GreatestCommonDivisor(UBigInt other) => new(BigInteger.GreatestCommonDivisor(_value, other._value));
        public UBigInt Pow(Int32 exponent) => new(BigInteger.Pow(_value, exponent));
        public UBigInt ModPow(UBigInt exponent, UBigInt modulus) => new(BigInteger.ModPow(_value, exponent._value, modulus._value));
        public double Log() => BigInteger.Log(_value);
        public double Log(double baseValue) => BigInteger.Log(_value, baseValue);
        public double Log10() => BigInteger.Log10(_value);

        #region Parse

        public static UBigInt Parse(string value) => new(BigInteger.Parse(value));
        public static UBigInt Parse(string value, NumberStyles style) => new(BigInteger.Parse(value, style));
        public static UBigInt Parse(string value, IFormatProvider? provider) => new(BigInteger.Parse(value, provider));
        public static UBigInt Parse(string value, NumberStyles style, IFormatProvider? provider) => new(BigInteger.Parse(value, style, provider));

        #endregion

        #region TryParse

        public static bool TryParse([NotNullWhen(true)] string? value, NumberStyles style, IFormatProvider? provider, out UBigInt result)
        {
            if (!BigInteger.TryParse(value, style, provider, out BigInteger bigIntegerValue))
            {
                result = Zero;
                return false;
            }
            result = new UBigInt(bigIntegerValue);
            return true;
        }

        public static bool TryParse([NotNullWhen(true)] string? value, out UBigInt result)
        {
            if (!BigInteger.TryParse(value, out BigInteger bigIntegerValue))
            {
                result = Zero;
                return false;
            }
            result = new UBigInt(bigIntegerValue);
            return true;
        }

        #endregion

        #region ToByteArray

        public byte[] ToByteArray(bool isBigEndian = false) => _value.ToByteArray(true, isBigEndian);

        #endregion

        #region CompareTo

        public Int32 CompareTo(BigInt other) => _value.CompareTo(((IBigIntInternalValue)other).Value);
        public Int32 CompareTo(UBigInt other) => _value.CompareTo(other._value);
        public Int32 CompareTo(BigInteger other) => _value.CompareTo(other);
        public Int32 CompareTo(Int64 other) => _value.CompareTo(other);
        public Int32 CompareTo(UInt64 other) => _value.CompareTo(other);
        public Int32 CompareTo(Int32 other) => _value.CompareTo(other);
        public Int32 CompareTo(UInt32 other) => _value.CompareTo(other);

        public Int32 CompareTo(object? obj)
        {
            if (obj is null)
                return 1;
            if (obj is BigInt BigIntValue)
                return CompareTo(BigIntValue);
            else if (obj is UBigInt UBigIntValue)
                return CompareTo(UBigIntValue);
            else if (obj is BigInteger BigIntegerValue)
                return CompareTo(BigIntegerValue);
            else if (obj is Int64 Int64Value)
                return CompareTo(Int64Value);
            else if (obj is UInt64 UInt64Value)
                return CompareTo(UInt64Value);
            else if (obj is Int32 Int32Value)
                return CompareTo(Int32Value);
            else if (obj is UInt32 UInt32Value)
                return CompareTo(UInt32Value);
            else
                return _value.CompareTo(obj);
        }

        #endregion

        #region Equals

        public bool Equals(BigInt other) => _value.Equals(((IBigIntInternalValue)other).Value);
        public bool Equals(UBigInt other) => _value.Equals(other._value);
        public bool Equals(BigInteger other) => _value.Equals(other);
        public bool Equals(Int64 other) => _value.Equals(other);
        public bool Equals(UInt64 other) => _value.Equals(other);
        public bool Equals(Int32 other) => _value.Equals(other);
        public bool Equals(UInt32 other) => _value.Equals(other);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null)
                return false;
            if (obj is BigInt BigIntValue)
                return Equals(BigIntValue);
            else if (obj is UBigInt UBigIntValue)
                return Equals(UBigIntValue);
            else if (obj is BigInteger BigIntegerValue)
                return Equals(BigIntegerValue);
            else if (obj is Int64 Int64Value)
                return Equals(Int64Value);
            else if (obj is UInt64 UInt64Value)
                return Equals(UInt64Value);
            else if (obj is Int32 Int32Value)
                return Equals(Int32Value);
            else if (obj is UInt32 UInt32Value)
                return Equals(UInt32Value);
            else
                return _value.Equals(obj);
        }

        #endregion

        public override Int32 GetHashCode() => _value.GetHashCode();

        #region ToString

        public string ToString(string? format) => _value.ToString(format, null);
        public string ToString(IFormatProvider? formatProvider) => _value.ToString(null, formatProvider);
        public string ToString(string? format, IFormatProvider? formatProvider) => _value.ToString(format, formatProvider);
        public override string ToString() => _value.ToString();

        #endregion

        public static UBigInt operator +(UBigInt left, UBigInt right) => new(left._value + right._value);

        #region operator -

        public static UBigInt operator -(UBigInt left, UBigInt right) => new(left._value - right._value);
        public static UInt64 operator -(UInt64 left, UBigInt right) => (UInt64)(left - right._value);
        public static UInt32 operator -(UInt32 left, UBigInt right) => (UInt32)(left - right._value);

        #endregion

        public static UBigInt operator *(UBigInt left, UBigInt right) => new(left._value * right._value);


        #region operator /

        public static UBigInt operator /(UBigInt left, UBigInt right) => new(left._value / right._value);
        public static UBigInt operator /(UBigInt left, UInt64 right) => new(left._value / right);
        public static UBigInt operator /(UBigInt left, UInt32 right) => new(left._value / right);
        public static UInt64 operator /(UInt64 left, UBigInt right) => (UInt64)(left / right._value);
        public static UInt32 operator /(UInt32 left, UBigInt right) => (UInt32)(left / right._value);

        #endregion

        #region operator %

        public static UBigInt operator %(UBigInt left, UBigInt right) => new(left._value % right._value);
        public static UInt64 operator %(UBigInt left, UInt64 right) => (UInt64)(left._value % right);
        public static UInt32 operator %(UBigInt left, UInt32 right) => (UInt32)(left._value % right);
        public static UInt64 operator %(UInt64 left, UBigInt right) => (UInt64)(left % right._value);
        public static UInt32 operator %(UInt32 left, UBigInt right) => (UInt32)(left % right._value);

        #endregion

        public static UBigInt operator ++(UBigInt value) => new(value._value + 1);
        public static UBigInt operator --(UBigInt value) => new(value._value - 1);
        public static UBigInt operator +(UBigInt value) => value;
        public static BigInt operator -(UBigInt value) => new(-value._value);

        public static UBigInt operator <<(UBigInt value, Int32 shift) => new(value._value << shift);
        public static UBigInt operator >>(UBigInt value, Int32 shift) => new(value._value >> shift);

        #region operator &

        public static UBigInt operator &(UBigInt left, UBigInt right) => new(left._value & right._value);
        public static UInt64 operator &(UBigInt left, UInt64 right) => (UInt64)(left._value & right);
        public static UInt32 operator &(UBigInt left, UInt32 right) => (UInt32)(left._value & right);
        public static UInt64 operator &(UInt64 left, UBigInt right) => (UInt64)(left & right._value);
        public static UInt32 operator &(UInt32 left, UBigInt right) => (UInt32)(left & right._value);

        #endregion

        public static UBigInt operator |(UBigInt left, UBigInt right) => new(left._value | right._value);

        public static UBigInt operator ^(UBigInt left, UBigInt right) => new(left._value ^ right._value);

        #region oeprator ==

        public static bool operator ==(UBigInt left, UBigInt right) => left.Equals(right);
        public static bool operator ==(UBigInt left, BigInteger right) => left.Equals(right);
        public static bool operator ==(UBigInt left, Int64 right) => left.Equals(right);
        public static bool operator ==(UBigInt left, UInt64 right) => left.Equals(right);
        public static bool operator ==(UBigInt left, Int32 right) => left.Equals(right);
        public static bool operator ==(UBigInt left, UInt32 right) => left.Equals(right);
        public static bool operator ==(BigInteger left, UBigInt right) => right.Equals(left);
        public static bool operator ==(Int64 left, UBigInt right) => right.Equals(left);
        public static bool operator ==(UInt64 left, UBigInt right) => right.Equals(left);
        public static bool operator ==(Int32 left, UBigInt right) => right.Equals(left);
        public static bool operator ==(UInt32 left, UBigInt right) => right.Equals(left);

        #endregion

        #region oeprator !=

        public static bool operator !=(UBigInt left, UBigInt right) => !left.Equals(right);
        public static bool operator !=(UBigInt left, BigInteger right) => !left.Equals(right);
        public static bool operator !=(UBigInt left, Int64 right) => !left.Equals(right);
        public static bool operator !=(UBigInt left, UInt64 right) => !left.Equals(right);
        public static bool operator !=(UBigInt left, Int32 right) => !left.Equals(right);
        public static bool operator !=(UBigInt left, UInt32 right) => !left.Equals(right);
        public static bool operator !=(BigInteger left, UBigInt right) => !right.Equals(left);
        public static bool operator !=(Int64 left, UBigInt right) => !right.Equals(left);
        public static bool operator !=(UInt64 left, UBigInt right) => !right.Equals(left);
        public static bool operator !=(Int32 left, UBigInt right) => !right.Equals(left);
        public static bool operator !=(UInt32 left, UBigInt right) => !right.Equals(left);

        #endregion

        #region oeprator >

        public static bool operator >(UBigInt left, UBigInt right) => left.CompareTo(right) > 0;
        public static bool operator >(UBigInt left, BigInteger right) => left.CompareTo(right) > 0;
        public static bool operator >(UBigInt left, Int64 right) => left.CompareTo(right) > 0;
        public static bool operator >(UBigInt left, UInt64 right) => left.CompareTo(right) > 0;
        public static bool operator >(UBigInt left, Int32 right) => left.CompareTo(right) > 0;
        public static bool operator >(UBigInt left, UInt32 right) => left.CompareTo(right) > 0;
        public static bool operator >(BigInteger left, UBigInt right) => right.CompareTo(left) < 0;
        public static bool operator >(Int64 left, UBigInt right) => right.CompareTo(left) < 0;
        public static bool operator >(UInt64 left, UBigInt right) => right.CompareTo(left) < 0;
        public static bool operator >(Int32 left, UBigInt right) => right.CompareTo(left) < 0;
        public static bool operator >(UInt32 left, UBigInt right) => right.CompareTo(left) < 0;

        #endregion

        #region oeprator >=

        public static bool operator >=(UBigInt left, UBigInt right) => left.CompareTo(right) >= 0;
        public static bool operator >=(UBigInt left, BigInteger right) => left.CompareTo(right) >= 0;
        public static bool operator >=(UBigInt left, Int64 right) => left.CompareTo(right) >= 0;
        public static bool operator >=(UBigInt left, UInt64 right) => left.CompareTo(right) >= 0;
        public static bool operator >=(UBigInt left, Int32 right) => left.CompareTo(right) >= 0;
        public static bool operator >=(UBigInt left, UInt32 right) => left.CompareTo(right) >= 0;
        public static bool operator >=(BigInteger left, UBigInt right) => right.CompareTo(left) <= 0;
        public static bool operator >=(Int64 left, UBigInt right) => right.CompareTo(left) <= 0;
        public static bool operator >=(UInt64 left, UBigInt right) => right.CompareTo(left) <= 0;
        public static bool operator >=(Int32 left, UBigInt right) => right.CompareTo(left) <= 0;
        public static bool operator >=(UInt32 left, UBigInt right) => right.CompareTo(left) <= 0;

        #endregion

        #region oeprator <

        public static bool operator <(UBigInt left, UBigInt right) => left.CompareTo(right) < 0;
        public static bool operator <(UBigInt left, BigInteger right) => left.CompareTo(right) < 0;
        public static bool operator <(UBigInt left, Int64 right) => left.CompareTo(right) < 0;
        public static bool operator <(UBigInt left, UInt64 right) => left.CompareTo(right) < 0;
        public static bool operator <(UBigInt left, Int32 right) => left.CompareTo(right) < 0;
        public static bool operator <(UBigInt left, UInt32 right) => left.CompareTo(right) < 0;
        public static bool operator <(BigInteger left, UBigInt right) => right.CompareTo(left) > 0;
        public static bool operator <(Int64 left, UBigInt right) => right.CompareTo(left) > 0;
        public static bool operator <(UInt64 left, UBigInt right) => right.CompareTo(left) > 0;
        public static bool operator <(Int32 left, UBigInt right) => right.CompareTo(left) > 0;
        public static bool operator <(UInt32 left, UBigInt right) => right.CompareTo(left) > 0;

        #endregion

        #region oeprator <=

        public static bool operator <=(UBigInt left, UBigInt right) => left.CompareTo(right) <= 0;
        public static bool operator <=(UBigInt left, BigInteger right) => left.CompareTo(right) <= 0;
        public static bool operator <=(UBigInt left, Int64 right) => left.CompareTo(right) <= 0;
        public static bool operator <=(UBigInt left, UInt64 right) => left.CompareTo(right) <= 0;
        public static bool operator <=(UBigInt left, Int32 right) => left.CompareTo(right) <= 0;
        public static bool operator <=(UBigInt left, UInt32 right) => left.CompareTo(right) <= 0;
        public static bool operator <=(BigInteger left, UBigInt right) => right.CompareTo(left) >= 0;
        public static bool operator <=(Int64 left, UBigInt right) => right.CompareTo(left) >= 0;
        public static bool operator <=(UInt64 left, UBigInt right) => right.CompareTo(left) >= 0;
        public static bool operator <=(Int32 left, UBigInt right) => right.CompareTo(left) >= 0;
        public static bool operator <=(UInt32 left, UBigInt right) => right.CompareTo(left) >= 0;

        #endregion

        #region operator explicit

        public static explicit operator SByte(UBigInt value) => (SByte)value._value;
        public static explicit operator Byte(UBigInt value) => (Byte)value._value;
        public static explicit operator Int16(UBigInt value) => (Int16)value._value;
        public static explicit operator UInt16(UBigInt value) => (UInt16)value._value;
        public static explicit operator Int32(UBigInt value) => (Int32)value._value;
        public static explicit operator UInt32(UBigInt value) => (UInt32)value._value;
        public static explicit operator Int64(UBigInt value) => (Int64)value._value;
        public static explicit operator UInt64(UBigInt value) => (UInt64)value._value;
        public static explicit operator BigInteger(UBigInt value) => value._value;
        public static explicit operator Single(UBigInt value) => (Single)value._value;
        public static explicit operator Double(UBigInt value) => (Double)value._value;
        public static explicit operator Decimal(UBigInt value) => (Decimal)value._value;
        public static explicit operator UBigInt(Single value) => new(value);
        public static explicit operator UBigInt(Double value) => new(value);
        public static explicit operator UBigInt(Decimal value) => new(value);

        #endregion

        #region operator implicit

        public static implicit operator UBigInt(Byte value) => new(value);
        public static implicit operator UBigInt(UInt16 value) => new(value);
        public static implicit operator UBigInt(UInt32 value) => new(value);
        public static implicit operator UBigInt(UInt64 value) => new(value);

        #endregion
    }
}

﻿using System;
using System.Globalization;
using System.IO;

namespace ZipUtility
{
    readonly struct ZipStreamPosition
        : IEquatable<ZipStreamPosition>, IComparable<ZipStreamPosition>, IZipStreamPositionValue
    {
        private readonly UInt32 _diskNumber;
        private readonly UInt64 _offsetOnTheDisk;
        private readonly IVirtualZipFile _multiVolumeInfo;

        internal ZipStreamPosition(UInt32 diskNumber, UInt64 offsetOnTheDisk, IVirtualZipFile multiVolumeInfo)
        {
            if (multiVolumeInfo is null)
                throw new ArgumentNullException(nameof(multiVolumeInfo));

            _diskNumber = diskNumber;
            _offsetOnTheDisk = offsetOnTheDisk;
            _multiVolumeInfo = multiVolumeInfo;
        }

        #region operator +

        public static ZipStreamPosition operator +(ZipStreamPosition x, Int64 y) => x.Add(y);
        public static ZipStreamPosition operator +(ZipStreamPosition x, UInt64 y) => x.Add(y);
        public static ZipStreamPosition operator +(ZipStreamPosition x, Int32 y) => x.Add(y);

        #endregion

        #region operator -

        public static UInt64 operator -(ZipStreamPosition x, ZipStreamPosition y) => x.Subtract(y);
        public static ZipStreamPosition operator -(ZipStreamPosition x, Int64 y) => x.Subtract(y);
        public static ZipStreamPosition operator -(ZipStreamPosition x, UInt64 y) => x.Subtract(y);
        public static ZipStreamPosition operator -(ZipStreamPosition x, Int32 y) => x.Subtract(y);

        #endregion

        #region other operator

        public static bool operator ==(ZipStreamPosition x, ZipStreamPosition y) => x.Equals(y);
        public static bool operator !=(ZipStreamPosition x, ZipStreamPosition y) => !x.Equals(y);
        public static bool operator >(ZipStreamPosition x, ZipStreamPosition y) => x.CompareTo(y) > 0;
        public static bool operator >=(ZipStreamPosition x, ZipStreamPosition y) => x.CompareTo(y) >= 0;
        public static bool operator <(ZipStreamPosition x, ZipStreamPosition y) => x.CompareTo(y) < 0;
        public static bool operator <=(ZipStreamPosition x, ZipStreamPosition y) => x.CompareTo(y) <= 0;

        #endregion

        #region Add

        public ZipStreamPosition Add(Int64 x)
        {
#if DEBUG
            checked
#endif
            {
                if (x >= 0)
                    return Add((UInt64)x);
                else if (x != Int64.MinValue)
                    return Subtract((UInt64)(-x));
                else
                    return
                        Subtract(-(Int64.MinValue + 1))
                        .Subtract(1);
            }
        }

        public ZipStreamPosition Add(UInt64 x)
        {
            if (_multiVolumeInfo is null)
                throw new InvalidOperationException("multiVolumeInfo not set");

            return _multiVolumeInfo.Add(this, x) ?? throw new OverflowException();
        }

        public ZipStreamPosition Add(Int32 x)
        {
            checked
            {
                if (x >= 0)
                    return Add((UInt64)x);
                else
                    return Subtract((UInt64)(-(Int64)x));
            }
        }

        #endregion

        #region Subtract

        public UInt64 Subtract(ZipStreamPosition x)
        {
            if (_multiVolumeInfo is null)
                throw new InvalidOperationException("multiVolumeInfo not set");

            return _multiVolumeInfo.Subtract(this, x);
        }

        public ZipStreamPosition Subtract(Int64 x)
        {
            checked
            {
                if (x >= 0)
                    return Subtract((UInt64)x);
                else if (x != Int64.MinValue)
                    return Add((UInt64)(-x));
                else
                    return
                        Add(-(Int64.MinValue + 1))
                        .Add(1);
            }
        }

        public ZipStreamPosition Subtract(UInt64 x)
        {
            if (_multiVolumeInfo is null)
                throw new InvalidOperationException("multiVolumeInfo not set");

            return _multiVolumeInfo.Subtract(this, x) ?? throw new IOException("Invalid file position");
        }

        public ZipStreamPosition Subtract(Int32 x)
        {
            checked
            {
                if (x >= 0)
                    return Subtract((UInt64)x);
                else
                    return Add((UInt64)(-(Int64)x));
            }
        }

        #endregion

        public Int32 CompareTo(ZipStreamPosition other)
        {
            Int32 c;
            if ((c = _diskNumber.CompareTo(other._diskNumber)) != 0)
                return c;
            if ((c = _offsetOnTheDisk.CompareTo(other._offsetOnTheDisk)) != 0)
                return c;
            return 0;
        }

        public bool Equals(ZipStreamPosition other)
        {
            return
                _diskNumber.Equals(other._diskNumber) &&
                _offsetOnTheDisk.Equals(other._offsetOnTheDisk);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null || GetType() != obj.GetType())
                return false;
            return Equals((ZipStreamPosition)obj);
        }

        public override Int32 GetHashCode()
        {
            return _diskNumber.GetHashCode() ^ _offsetOnTheDisk.GetHashCode();
        }

        public string ToString(string? format)
        {
            return
                (format ?? "G").ToUpperInvariant() switch
                {
                    "G" or "" or null => string.Format(CultureInfo.InvariantCulture, "{0}:{1}", _diskNumber, _offsetOnTheDisk),
                    "X" => string.Format(CultureInfo.InvariantCulture, "0x{0:x8}:0x{1:x16}", _diskNumber, _offsetOnTheDisk),
                    _ => throw new FormatException(),
                };
        }

        public override string ToString()
        {
            return ToString("G");
        }

        UInt32 IZipStreamPositionValue.DiskNumber => _diskNumber;

        UInt64 IZipStreamPositionValue.OffsetOnTheDisk => _offsetOnTheDisk;
    }
}

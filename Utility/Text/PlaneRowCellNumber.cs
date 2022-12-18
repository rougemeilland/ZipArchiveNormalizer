using System;

namespace Utility.Text
{
    public struct PlaneRowCellNumber
        : IEquatable<PlaneRowCellNumber>
    {
        [Obsolete("Do not call the default constructor.")]
        public PlaneRowCellNumber()
        {
            throw new NotImplementedException();
        }

        public PlaneRowCellNumber(Int32 cell)
        {
            if (!cell.IsBetween(byte.MinValue, (Int32)Byte.MaxValue))
                throw new ArgumentOutOfRangeException(nameof(cell));
            Plane = 0;
            Row = 0;
            Cell = (Byte)cell;
        }

        public PlaneRowCellNumber(Int32 plane, Int32 row, Int32 cell)
        {
            if (!plane.IsBetween(1, 2))
                throw new ArgumentOutOfRangeException(nameof(plane));
            if (!row.IsBetween(1, 94))
                throw new ArgumentOutOfRangeException(nameof(row));
            if (!cell.IsBetween(1, 94))
                throw new ArgumentOutOfRangeException(nameof(cell));
            Plane = (Byte)plane;
            Row = (Byte)row;
            Cell = (Byte)cell;
        }

        public Byte Plane { get; }
        public Byte Row { get; }
        public Byte Cell { get; }

        public bool IsSingleByte
        {
            get
            {
                if (Plane > 0)
                    return false;
                else if (Row > 0)
                    throw new InternalLogicalErrorException();
                else
                    return true;
            }
        }

        public bool Equals(PlaneRowCellNumber other) =>
            Plane == other.Plane && Row == other.Row && Cell == other.Cell;

        public override bool Equals(object? obj) =>
            obj is not null && GetType() == obj.GetType() && Equals((PlaneRowCellNumber)obj);

        public override Int32 GetHashCode() => Plane.GetHashCode() ^ Row.GetHashCode() ^ Cell.GetHashCode();

        public override string ToString()
        {
            if (Plane > 0)
                return $"{Plane}-{Row}-{Cell}";
            else if (Row > 0)
                return $"{Row}-{Cell}";
            else
                return $"{Cell}";
        }

        public static bool operator ==(PlaneRowCellNumber left, PlaneRowCellNumber right) => left.Equals(right);

        public static bool operator !=(PlaneRowCellNumber left, PlaneRowCellNumber right) => !(left == right);
    }
}

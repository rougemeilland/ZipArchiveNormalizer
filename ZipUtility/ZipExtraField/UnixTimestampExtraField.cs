﻿using System;
using Utility;

namespace ZipUtility.ZipExtraField
{
    public abstract class UnixTimestampExtraField
        : TimestampExtraField
    {
        private static readonly DateTime _baseTime;

        static UnixTimestampExtraField()
        {
            _baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        protected UnixTimestampExtraField(UInt16 extraFieldId)
            : base(extraFieldId)
        {
        }

        public override abstract ReadOnlyMemory<byte>? GetData(ZipEntryHeaderType headerType);
        public override abstract void SetData(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data);

        protected static DateTime? FromUnixTimeStamp(Int32 timeStamp)
        {
            if (timeStamp == 0)
                return null;
            var dateTime = _baseTime.AddSeconds(timeStamp);
#if DEBUG
            if (dateTime.Kind != DateTimeKind.Utc)
                throw new Exception();
#endif
            return dateTime;
        }

        protected static Int32 ToUnixTimeStamp(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
                throw new InternalLogicalErrorException();
            var timeStamp = (dateTime.ToUniversalTime() - _baseTime).TotalSeconds;
            if (!timeStamp.IsBetween((double)Int32.MinValue, Int32.MaxValue))
                throw new OverflowException();
            return (Int32)timeStamp;
        }
    }
}

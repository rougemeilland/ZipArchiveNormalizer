using System;

namespace Utility
{
    public static class DateTimeExtensions
    {
        public static DateTime FromDosDateTimeToDateTime(this (UInt16 dosDate, UInt16 dosTime) dosDateTimeValue, DateTimeKind kind)
        {
            var second = (dosDateTimeValue.dosTime & 0x1f) << 1;
            if (second > 59)
                second = 59;
            var minute = (dosDateTimeValue.dosTime >> 5) & 0x3f;
            if (minute > 59)
                minute = 59;
            var hour = (dosDateTimeValue.dosTime >> 11) & 0x1f;
            if (hour > 23)
                hour = 23;
            var month = (dosDateTimeValue.dosDate >> 5) & 0xf;
            if (month < 1)
                month = 1;
            else if (month > 12)
                month = 12;
            else
            {
            }
            var year = ((dosDateTimeValue.dosDate >> 9) & 0x7f) + 1980;
            var day = dosDateTimeValue.dosDate & 0x1f;
            if (day < 1)
                day = 1;
            else
            {
                var maximumDayValue = DateTime.DaysInMonth(year, month);
                if (day > maximumDayValue)
                    day = maximumDayValue;
            }
            return new DateTime(year, month, day, hour, minute, second, kind);
        }
    }
}

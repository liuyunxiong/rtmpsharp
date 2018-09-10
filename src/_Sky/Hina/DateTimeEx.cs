using System;

// csharp: hina/datetimeex.cs [snipped]
namespace Hina
{
    partial class DateTimeEx
    {
        public static DateTime CreateUtc(int year, int month = 1, int day = 1, int hour = 0, int minute = 0, int second = 0, int millisecond = 0)
            => new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);
    }
}

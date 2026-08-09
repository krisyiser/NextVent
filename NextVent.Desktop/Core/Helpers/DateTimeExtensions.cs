using System;

namespace NextVent.Core.Helpers;

public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo LocalBusinessTimeZone = 
        TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    public static DateTime ToBusinessLocalTime(this DateTime utcDate)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDate, LocalBusinessTimeZone);
    }

    public static DateTime ToBusinessUtcTime(this DateTime localDate)
    {
        return TimeZoneInfo.ConvertTimeToUtc(localDate, LocalBusinessTimeZone);
    }
}

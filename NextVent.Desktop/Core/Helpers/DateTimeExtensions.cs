using System;

namespace NextVent.Core.Helpers;

public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo LocalBusinessTimeZone = 
        TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    public static DateTime ToBusinessLocalTime(this DateTime utcDate)
    {
        var utc = DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, LocalBusinessTimeZone);
    }

    public static DateTime ToBusinessUtcTime(this DateTime localDate)
    {
        var unspecified = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, LocalBusinessTimeZone);
    }
}

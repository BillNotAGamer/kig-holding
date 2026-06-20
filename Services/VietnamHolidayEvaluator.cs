using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace KIGHolding.Services;

public static class VietnamHolidayEvaluator
{
    private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

    private static readonly FrozenSet<DateOnly> Holidays = new[]
    {
        // 2026 Holidays
        new DateOnly(2026, 1, 1),   // New Year's Day
        new DateOnly(2026, 2, 16),  // Lunar New Year (Tet Eve)
        new DateOnly(2026, 2, 17),  // Lunar New Year 1
        new DateOnly(2026, 2, 18),  // Lunar New Year 2
        new DateOnly(2026, 2, 19),  // Lunar New Year 3
        new DateOnly(2026, 2, 20),  // Lunar New Year 4
        new DateOnly(2026, 4, 27),  // Hung Kings Commemoration (Compensatory)
        new DateOnly(2026, 4, 30),  // Reunification Day
        new DateOnly(2026, 5, 1),   // International Labor Day
        new DateOnly(2026, 9, 2),   // National Day
        new DateOnly(2026, 9, 3),   // National Day Day 2

        // 2027 Holidays
        new DateOnly(2027, 1, 1),   // New Year's Day
        new DateOnly(2027, 2, 5),   // Lunar New Year (Tet Eve)
        new DateOnly(2027, 2, 8),   // Lunar New Year 2
        new DateOnly(2027, 2, 9),   // Lunar New Year 3
        new DateOnly(2027, 2, 10),  // Lunar New Year 4
        new DateOnly(2027, 2, 11),  // Lunar New Year 5
        new DateOnly(2027, 4, 16),  // Hung Kings Commemoration
        new DateOnly(2027, 4, 30),  // Reunification Day
        new DateOnly(2027, 5, 3),   // International Labor Day (Compensatory)
        new DateOnly(2027, 9, 2),   // National Day
        new DateOnly(2027, 9, 3),   // National Day Day 2

        // 2028 Holidays
        new DateOnly(2028, 1, 3),   // New Year's Day (Compensatory)
        new DateOnly(2028, 1, 25),  // Lunar New Year (Tet Eve)
        new DateOnly(2028, 1, 26),  // Lunar New Year 1
        new DateOnly(2028, 1, 27),  // Lunar New Year 2
        new DateOnly(2028, 1, 28),  // Lunar New Year 3
        new DateOnly(2028, 1, 31),  // Lunar New Year (Compensatory)
        new DateOnly(2028, 4, 4),   // Hung Kings Commemoration
        new DateOnly(2028, 5, 1),   // International Labor Day
        new DateOnly(2028, 5, 2),   // Reunification Day (Compensatory)
        new DateOnly(2028, 9, 4)    // National Day (Compensatory)
    }.ToFrozenSet();

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    public static DateTimeOffset GetVietnamNow()
    {
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, VietnamTimeZone);
    }

    public static DateOnly GetVietnamToday()
    {
        return DateOnly.FromDateTime(GetVietnamNow().DateTime);
    }

    public static bool IsRestrictedDate(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return true;
        }

        return Holidays.Contains(date);
    }
}

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace KIGHolding.Services;

public static class VietnamHolidayEvaluator
{
    public const int FirstConfiguredHolidayYear = 2026;
    public const int LastConfiguredHolidayYear = 2028;

    public static DateOnly MaximumOpenReservationDate { get; } = new(LastConfiguredHolidayYear, 12, 31);

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
        return GetVietnamNow(TimeProvider.System);
    }

    public static DateTimeOffset GetVietnamNow(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        return TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), VietnamTimeZone);
    }

    public static DateOnly GetVietnamToday()
    {
        return GetVietnamToday(TimeProvider.System);
    }

    public static DateOnly GetVietnamToday(TimeProvider timeProvider)
    {
        return DateOnly.FromDateTime(GetVietnamNow(timeProvider).DateTime);
    }

    public static ReservationDatePolicyResult EvaluateReservationDate(DateOnly reservationDate, DateOnly vietnamToday)
    {
        if (reservationDate < vietnamToday)
        {
            return new ReservationDatePolicyResult(ReservationDatePolicyStatus.PastDate);
        }

        if (reservationDate > MaximumOpenReservationDate)
        {
            return new ReservationDatePolicyResult(ReservationDatePolicyStatus.BookingCalendarClosed);
        }

        if (reservationDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return new ReservationDatePolicyResult(ReservationDatePolicyStatus.Weekend);
        }

        if (Holidays.Contains(reservationDate))
        {
            return new ReservationDatePolicyResult(ReservationDatePolicyStatus.Holiday);
        }

        return new ReservationDatePolicyResult(ReservationDatePolicyStatus.Allowed);
    }

    public static IReadOnlyList<DateOnly> GetConfiguredHolidayDates()
    {
        return Holidays.OrderBy(date => date).ToArray();
    }

    public static IReadOnlyList<int> GetConfiguredHolidayYears()
    {
        return Holidays
            .Select(date => date.Year)
            .Distinct()
            .OrderBy(year => year)
            .ToArray();
    }

    public static string GetReservationDatePolicyMessage(ReservationDatePolicyStatus status)
    {
        return status switch
        {
            ReservationDatePolicyStatus.PastDate =>
                "Ngày đến không được sớm hơn hôm nay.",
            ReservationDatePolicyStatus.Weekend =>
                "Nhà hàng không nhận đặt bàn vào thứ Bảy và Chủ nhật. Vui lòng chọn ngày khác.",
            ReservationDatePolicyStatus.Holiday =>
                "Hệ thống không nhận đặt bàn vào Thứ Bảy, Chủ Nhật và các ngày Lễ Tết.",
            ReservationDatePolicyStatus.BookingCalendarClosed =>
                "Hệ thống chưa mở lịch đặt bàn cho thời gian này. Vui lòng chọn ngày khác hoặc liên hệ nhà hàng.",
            _ => string.Empty
        };
    }
}

public sealed record ReservationDatePolicyResult(ReservationDatePolicyStatus Status)
{
    public bool IsAllowed => Status == ReservationDatePolicyStatus.Allowed;
}

public enum ReservationDatePolicyStatus
{
    Allowed,
    PastDate,
    BookingCalendarClosed,
    Weekend,
    Holiday
}

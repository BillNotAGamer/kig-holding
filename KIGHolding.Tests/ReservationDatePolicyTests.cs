using KIGHolding.Services;

namespace KIGHolding.Tests;

public sealed class ReservationDatePolicyTests
{
    private static readonly DateOnly AuditToday = new(2026, 8, 6);

    [Theory]
    [InlineData("2026-08-08", ReservationDatePolicyStatus.Weekend)]
    [InlineData("2026-08-09", ReservationDatePolicyStatus.Weekend)]
    [InlineData("2026-08-10", ReservationDatePolicyStatus.Allowed)]
    [InlineData("2026-08-05", ReservationDatePolicyStatus.PastDate)]
    [InlineData("2028-12-31", ReservationDatePolicyStatus.Weekend)]
    [InlineData("2029-01-01", ReservationDatePolicyStatus.BookingCalendarClosed)]
    [InlineData("2030-01-02", ReservationDatePolicyStatus.BookingCalendarClosed)]
    public void EvaluateReservationDate_ClassifiesIncidentWeekendDates(string dateValue, ReservationDatePolicyStatus expectedStatus)
    {
        var date = DateOnly.ParseExact(dateValue, "yyyy-MM-dd");

        var result = VietnamHolidayEvaluator.EvaluateReservationDate(date, AuditToday);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(dateValue, date.ToString("yyyy-MM-dd"));
    }

    [Theory]
    [InlineData("2026-01-01")]
    [InlineData("2027-01-01")]
    [InlineData("2028-01-03")]
    public void EvaluateReservationDate_RejectsConfiguredHolidayFromEachConfiguredYear(string dateValue)
    {
        var result = VietnamHolidayEvaluator.EvaluateReservationDate(
            DateOnly.ParseExact(dateValue, "yyyy-MM-dd"),
            new DateOnly(2026, 1, 1));

        Assert.Equal(ReservationDatePolicyStatus.Holiday, result.Status);
    }

    [Theory]
    [InlineData("2026-08-10")]
    [InlineData("2027-01-04")]
    [InlineData("2028-01-04")]
    public void EvaluateReservationDate_AllowsOrdinaryWeekdayFromEachConfiguredYear(string dateValue)
    {
        var result = VietnamHolidayEvaluator.EvaluateReservationDate(
            DateOnly.ParseExact(dateValue, "yyyy-MM-dd"),
            new DateOnly(2025, 12, 31));

        Assert.Equal(ReservationDatePolicyStatus.Allowed, result.Status);
    }

    [Fact]
    public void EvaluateReservationDate_UsesDocumentedPrecedence()
    {
        Assert.Equal(
            ReservationDatePolicyStatus.PastDate,
            VietnamHolidayEvaluator.EvaluateReservationDate(new DateOnly(2026, 8, 8), new DateOnly(2026, 8, 9)).Status);
        Assert.Equal(
            ReservationDatePolicyStatus.BookingCalendarClosed,
            VietnamHolidayEvaluator.EvaluateReservationDate(new DateOnly(2029, 1, 6), AuditToday).Status);
    }

    [Fact]
    public void EvaluateReservationDate_ResultDoesNotDependOnHostTimezoneForDateOnly()
    {
        var previousTimezone = Environment.GetEnvironmentVariable("TZ");

        try
        {
            Environment.SetEnvironmentVariable("TZ", "UTC");
            var utcResult = VietnamHolidayEvaluator.EvaluateReservationDate(new DateOnly(2026, 8, 9), AuditToday);

            Environment.SetEnvironmentVariable("TZ", "Asia/Ho_Chi_Minh");
            var vietnamResult = VietnamHolidayEvaluator.EvaluateReservationDate(new DateOnly(2026, 8, 9), AuditToday);

            Assert.Equal(ReservationDatePolicyStatus.Weekend, utcResult.Status);
            Assert.Equal(ReservationDatePolicyStatus.Weekend, vietnamResult.Status);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TZ", previousTimezone);
        }
    }

    [Fact]
    public void GetVietnamToday_UsesTimeProviderAndVietnamLocalDate()
    {
        var timeProvider = new FixedTimeProvider(DateTimeOffset.Parse("2026-08-05T18:00:00+00:00"));

        Assert.Equal(new DateOnly(2026, 8, 6), VietnamHolidayEvaluator.GetVietnamToday(timeProvider));
    }

    [Fact]
    public void GetConfiguredHolidayYears_DocumentsOpenCalendarBoundary()
    {
        Assert.Equal([2026, 2027, 2028], VietnamHolidayEvaluator.GetConfiguredHolidayYears());
        Assert.Equal(new DateOnly(2028, 12, 31), VietnamHolidayEvaluator.MaximumOpenReservationDate);
    }
}

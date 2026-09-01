using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Tests;

public sealed class ReservationDatePolicyTests
{
    private static readonly DateOnly AuditToday = new(2026, 9, 1);

    [Theory]
    [InlineData("2026-09-02")]
    [InlineData("2026-09-03")]
    [InlineData("2027-01-01")]
    [InlineData("2028-01-03")]
    public async Task EvaluateReservationDate_BlocksOnlyDatesStoredInDatabase(string dateValue)
    {
        await using var dbContext = CreateDbContext();
        var date = DateOnly.ParseExact(dateValue, "yyyy-MM-dd");
        dbContext.BlockedReservationDates.Add(new BlockedReservationDate { Date = date });
        await dbContext.SaveChangesAsync();
        var service = new ReservationBlockedDateService(dbContext);

        var result = await service.EvaluateReservationDateAsync(date, AuditToday);

        Assert.Equal(ReservationDatePolicyStatus.BlockedDate, result.Status);
    }

    [Theory]
    [InlineData("2026-08-08")]
    [InlineData("2026-08-09")]
    [InlineData("2026-09-02")]
    [InlineData("2026-09-05")]
    [InlineData("2026-09-06")]
    [InlineData("2029-01-01")]
    [InlineData("2030-01-02")]
    public async Task EvaluateReservationDate_AllowsFutureDatesWhenDatabaseDoesNotBlockThem(string dateValue)
    {
        await using var dbContext = CreateDbContext();
        var service = new ReservationBlockedDateService(dbContext);

        var result = await service.EvaluateReservationDateAsync(
            DateOnly.ParseExact(dateValue, "yyyy-MM-dd"),
            new DateOnly(2026, 8, 6));

        Assert.Equal(ReservationDatePolicyStatus.Allowed, result.Status);
    }

    [Fact]
    public async Task EvaluateReservationDate_UsesPastDateBeforeBlockedDate()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BlockedReservationDates.Add(new BlockedReservationDate { Date = new DateOnly(2026, 8, 31) });
        await dbContext.SaveChangesAsync();
        var service = new ReservationBlockedDateService(dbContext);

        var result = await service.EvaluateReservationDateAsync(new DateOnly(2026, 8, 31), AuditToday);

        Assert.Equal(ReservationDatePolicyStatus.PastDate, result.Status);
    }

    [Fact]
    public async Task EvaluateReservationDate_ResultDoesNotDependOnHostTimezoneForDateOnly()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BlockedReservationDates.Add(new BlockedReservationDate { Date = new DateOnly(2026, 9, 2) });
        await dbContext.SaveChangesAsync();
        var service = new ReservationBlockedDateService(dbContext);
        var previousTimezone = Environment.GetEnvironmentVariable("TZ");

        try
        {
            Environment.SetEnvironmentVariable("TZ", "UTC");
            var utcResult = await service.EvaluateReservationDateAsync(new DateOnly(2026, 9, 2), AuditToday);

            Environment.SetEnvironmentVariable("TZ", "Asia/Ho_Chi_Minh");
            var vietnamResult = await service.EvaluateReservationDateAsync(new DateOnly(2026, 9, 2), AuditToday);

            Assert.Equal(ReservationDatePolicyStatus.BlockedDate, utcResult.Status);
            Assert.Equal(ReservationDatePolicyStatus.BlockedDate, vietnamResult.Status);
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

        Assert.Equal(new DateOnly(2026, 8, 6), VietnamClock.GetVietnamToday(timeProvider));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }
}

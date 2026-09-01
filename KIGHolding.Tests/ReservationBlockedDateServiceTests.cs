using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KIGHolding.Tests;

public sealed class ReservationBlockedDateServiceTests
{
    private static readonly DateOnly VietnamToday = new(2026, 9, 1);

    [Fact]
    public async Task CleanupPastDatesAsync_RemovesOnlyDatesBeforeVietnamTodayAndIsIdempotent()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BlockedReservationDates.AddRange(
            new BlockedReservationDate { Date = new DateOnly(2026, 8, 30) },
            new BlockedReservationDate { Date = new DateOnly(2026, 8, 31) },
            new BlockedReservationDate { Date = new DateOnly(2026, 9, 1) },
            new BlockedReservationDate { Date = new DateOnly(2026, 9, 2) },
            new BlockedReservationDate { Date = new DateOnly(2027, 1, 1) });
        await dbContext.SaveChangesAsync();
        var service = new ReservationBlockedDateService(dbContext);

        var firstCleanupCount = await service.CleanupPastDatesAsync(VietnamToday);
        var secondCleanupCount = await service.CleanupPastDatesAsync(VietnamToday);

        Assert.Equal(2, firstCleanupCount);
        Assert.Equal(0, secondCleanupCount);
        Assert.Equal(
            [
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 9, 2),
                new DateOnly(2027, 1, 1)
            ],
            await dbContext.BlockedReservationDates
                .OrderBy(x => x.Date)
                .Select(x => x.Date)
                .ToListAsync());
    }

    [Fact]
    public async Task ReplaceActiveBlockedDatesAsync_ReconcilesFutureSetAndPreventsPastDatesBecomingActive()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BlockedReservationDates.AddRange(
            new BlockedReservationDate { Date = new DateOnly(2026, 8, 31) },
            new BlockedReservationDate { Date = new DateOnly(2026, 9, 2) },
            new BlockedReservationDate { Date = new DateOnly(2027, 1, 1) });
        await dbContext.SaveChangesAsync();
        var service = new ReservationBlockedDateService(dbContext);

        await service.ReplaceActiveBlockedDatesAsync(
            [
                new DateOnly(2026, 8, 30),
                new DateOnly(2026, 9, 2),
                new DateOnly(2026, 9, 2),
                new DateOnly(2026, 9, 3)
            ],
            VietnamToday);

        Assert.Equal(
            [
                new DateOnly(2026, 9, 2),
                new DateOnly(2026, 9, 3)
            ],
            await dbContext.BlockedReservationDates
                .OrderBy(x => x.Date)
                .Select(x => x.Date)
                .ToListAsync());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }
}

using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using KIGHolding.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KIGHolding.Tests;

public sealed class HistoricalReservationDeletionServiceTests
{
    private static readonly TimeProvider TimeProvider =
        new FixedTimeProvider(DateTimeOffset.Parse("2026-11-06T05:00:00+00:00")); // 12:00 in Vietnam

    [Theory]
    [InlineData(ReservationStatus.Confirmed)]
    [InlineData(ReservationStatus.Arrived)]
    public async Task SoftDelete_PastProcessedReservation_SucceedsAndRecordsActor(ReservationStatus status)
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateAdmin("SuperAdmin");
        var reservation = CreateReservation(status, new DateOnly(2026, 11, 6), new TimeOnly(11, 59));
        dbContext.AddRange(actor, reservation);
        await dbContext.SaveChangesAsync();
        var expectedVersion = reservation.Version;

        var result = await CreateService(dbContext).SoftDeleteAsync(
            new HistoricalReservationDeletionRequest(reservation.Id, expectedVersion, actor.Id));

        Assert.True(result.Succeeded);
        Assert.Empty(await dbContext.Reservations.ToListAsync());
        var deleted = await dbContext.Reservations.IgnoreQueryFilters().SingleAsync();
        Assert.True(deleted.IsDeleted);
        Assert.Equal(actor.Id, deleted.DeletedByAdminUserId);
        Assert.Equal(TimeProvider.GetUtcNow(), deleted.DeletedAt);
        Assert.NotEqual(expectedVersion, deleted.Version);
        Assert.NotEqual(Guid.Empty, deleted.Version);
    }

    [Theory]
    [InlineData(ReservationStatus.Pending)]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.NoShow)]
    public async Task SoftDelete_IneligibleStatus_IsRejected(ReservationStatus status)
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateAdmin("SuperAdmin");
        var reservation = CreateReservation(status, new DateOnly(2026, 11, 5), new TimeOnly(18, 0));
        dbContext.AddRange(actor, reservation);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).SoftDeleteAsync(
            new HistoricalReservationDeletionRequest(reservation.Id, reservation.Version, actor.Id));

        Assert.Equal(HistoricalReservationDeletionStatus.NotEligible, result.Status);
        Assert.False((await dbContext.Reservations.SingleAsync()).IsDeleted);
    }

    [Fact]
    public async Task SoftDelete_FutureAndExactScheduledInstant_AreRejected()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateAdmin("SuperAdmin");
        var exactNow = CreateReservation(ReservationStatus.Confirmed, new DateOnly(2026, 11, 6), new TimeOnly(12, 0));
        var futureToday = CreateReservation(ReservationStatus.Arrived, new DateOnly(2026, 11, 6), new TimeOnly(12, 1));
        dbContext.AddRange(actor, exactNow, futureToday);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var exactResult = await service.SoftDeleteAsync(
            new HistoricalReservationDeletionRequest(exactNow.Id, exactNow.Version, actor.Id));
        var futureResult = await service.SoftDeleteAsync(
            new HistoricalReservationDeletionRequest(futureToday.Id, futureToday.Version, actor.Id));

        Assert.Equal(HistoricalReservationDeletionStatus.NotEligible, exactResult.Status);
        Assert.Equal(HistoricalReservationDeletionStatus.NotEligible, futureResult.Status);
        Assert.Equal(2, await dbContext.Reservations.CountAsync());
    }

    [Fact]
    public async Task SoftDelete_StaleVersionAndAlreadyDeleted_AreNeverReportedAsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateAdmin("SuperAdmin");
        var reservation = CreateReservation(ReservationStatus.Confirmed, new DateOnly(2026, 11, 5), new TimeOnly(18, 0));
        dbContext.AddRange(actor, reservation);
        await dbContext.SaveChangesAsync();
        var version = reservation.Version;
        var service = CreateService(dbContext);

        var stale = await service.SoftDeleteAsync(
            new HistoricalReservationDeletionRequest(reservation.Id, Guid.NewGuid(), actor.Id));
        var deleted = await service.SoftDeleteAsync(
            new HistoricalReservationDeletionRequest(reservation.Id, version, actor.Id));
        var repeat = await service.SoftDeleteAsync(
            new HistoricalReservationDeletionRequest(reservation.Id, version, actor.Id));

        Assert.Equal(HistoricalReservationDeletionStatus.Conflict, stale.Status);
        Assert.True(deleted.Succeeded);
        Assert.Equal(HistoricalReservationDeletionStatus.AlreadyDeleted, repeat.Status);
    }

    [Fact]
    public async Task SoftDelete_RequiresActiveSuperAdminAndNeverTrustsInvalidActor()
    {
        await using var dbContext = CreateDbContext();
        var nonSuperAdmin = CreateAdmin("Admin");
        var inactiveSuperAdmin = CreateAdmin("SuperAdmin");
        inactiveSuperAdmin.IsActive = false;
        var reservation = CreateReservation(ReservationStatus.Arrived, new DateOnly(2026, 11, 5), new TimeOnly(18, 0));
        dbContext.AddRange(nonSuperAdmin, inactiveSuperAdmin, reservation);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var nonSuperAdminResult = await service.SoftDeleteAsync(
            new HistoricalReservationDeletionRequest(reservation.Id, reservation.Version, nonSuperAdmin.Id));
        var inactiveResult = await service.SoftDeleteAsync(
            new HistoricalReservationDeletionRequest(reservation.Id, reservation.Version, inactiveSuperAdmin.Id));

        Assert.Equal(HistoricalReservationDeletionStatus.Unauthorized, nonSuperAdminResult.Status);
        Assert.Equal(HistoricalReservationDeletionStatus.Unauthorized, inactiveResult.Status);
        Assert.False((await dbContext.Reservations.SingleAsync()).IsDeleted);
    }

    [Fact]
    public async Task SoftDelete_GlobalFilterExcludesDeletedReservationFromOperationalCounts()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateAdmin("SuperAdmin");
        var deletedCandidate = CreateReservation(ReservationStatus.Confirmed, new DateOnly(2026, 11, 5), new TimeOnly(18, 0));
        var activeReservation = CreateReservation(ReservationStatus.Pending, new DateOnly(2026, 11, 7), new TimeOnly(18, 0));
        dbContext.AddRange(actor, deletedCandidate, activeReservation);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).SoftDeleteAsync(
            new HistoricalReservationDeletionRequest(deletedCandidate.Id, deletedCandidate.Version, actor.Id));

        Assert.True(result.Succeeded);
        Assert.Equal(1, await dbContext.Reservations.CountAsync());
        Assert.Single(await dbContext.Reservations.Where(item => item.Status == ReservationStatus.Pending).ToListAsync());
        Assert.Equal(2, await dbContext.Reservations.IgnoreQueryFilters().CountAsync());
    }

    private static HistoricalReservationDeletionService CreateService(AppDbContext dbContext) =>
        new(dbContext, TimeProvider);

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"), new InMemoryDatabaseRoot())
            .Options;
        return new AppDbContext(options);
    }

    private static AdminUser CreateAdmin(string role) => new()
    {
        Username = $"admin-{Guid.NewGuid():N}",
        PasswordHash = "test-only",
        SecurityStamp = Guid.NewGuid().ToString("N"),
        Role = role,
        IsActive = true
    };

    private static Reservation CreateReservation(ReservationStatus status, DateOnly date, TimeOnly time) => new()
    {
        BranchId = Guid.NewGuid(),
        CustomerName = "Historical deletion test",
        PhoneNumber = Guid.NewGuid().ToString("N"),
        GuestCount = 2,
        ReservationDate = date,
        ReservationTime = time,
        Status = status,
        Version = Guid.NewGuid()
    };
}

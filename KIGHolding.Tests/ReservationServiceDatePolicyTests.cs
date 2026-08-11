using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using KIGHolding.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIGHolding.Tests;

public sealed class ReservationServiceDatePolicyTests
{
    private static readonly TimeProvider AuditTimeProvider =
        new FixedTimeProvider(DateTimeOffset.Parse("2026-08-05T17:00:00+00:00"));

    [Theory]
    [InlineData("2026-08-08", ReservationDatePolicyStatus.Weekend)]
    [InlineData("2026-08-09", ReservationDatePolicyStatus.Weekend)]
    [InlineData("2027-01-01", ReservationDatePolicyStatus.Holiday)]
    public async Task CreateReservationAsync_RejectedPolicyDate_DoesNotPersistOrNotify(
        string dateValue,
        ReservationDatePolicyStatus expectedPolicyStatus)
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateReservableBranch();
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();

        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var service = CreateService(dbContext, cache, notifier);
        var request = CreateRequest(branch.Id, DateOnly.ParseExact(dateValue, "yyyy-MM-dd"));

        var result = await service.CreateReservationAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.FieldName == nameof(ReservationCreateRequest.ReservationDate));
        Assert.Equal(0, await dbContext.Reservations.CountAsync());
        Assert.Equal(0, notifier.CallCount);
        Assert.Equal(expectedPolicyStatus, VietnamHolidayEvaluator.EvaluateReservationDate(request.ReservationDate, new DateOnly(2025, 12, 31)).Status);
    }

    [Theory]
    [InlineData("2026-08-08", ReservationDatePolicyStatus.Weekend)]
    [InlineData("2026-08-09", ReservationDatePolicyStatus.Weekend)]
    [InlineData("2029-01-01", ReservationDatePolicyStatus.BookingCalendarClosed)]
    public async Task CreateReservationAsync_RejectedPolicyDate_ReturnsBeforeDatabaseWorkOrSideEffects(
        string dateValue,
        ReservationDatePolicyStatus expectedPolicyStatus)
    {
        await using var dbContext = CreateProviderlessDbContext();
        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var service = CreateService(dbContext, cache, notifier);

        var request = CreateRequest(Guid.NewGuid(), DateOnly.ParseExact(dateValue, "yyyy-MM-dd"));
        var result = await service.CreateReservationAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.FieldName == nameof(ReservationCreateRequest.ReservationDate) &&
            error.Message == VietnamHolidayEvaluator.GetReservationDatePolicyMessage(expectedPolicyStatus));
        Assert.Equal(0, dbContext.SaveChangesAsyncCallCount);
        Assert.Equal(0, notifier.CallCount);
        var phoneLockKey = IdentityNormalizer.PhoneLockKey(IdentityNormalizer.NormalizePhone(request.PhoneNumber));
        Assert.False(cache.TryGetValue(phoneLockKey, out _));
    }

    [Fact]
    public async Task CreateReservationAsync_ValidMonday_HasNoDatePolicyErrorWhenLaterBranchRuleFails()
    {
        await using var dbContext = CreateDbContext();
        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var service = CreateService(dbContext, cache, notifier);

        var result = await service.CreateReservationAsync(CreateRequest(Guid.NewGuid(), new DateOnly(2026, 8, 10)));

        Assert.False(result.Succeeded);
        Assert.DoesNotContain(result.Errors, error => error.FieldName == nameof(ReservationCreateRequest.ReservationDate));
        Assert.Contains(result.Errors, error => error.FieldName == nameof(ReservationCreateRequest.BranchId));
        Assert.Equal(0, await dbContext.Reservations.CountAsync());
        Assert.Equal(0, notifier.CallCount);
    }

    private static ReservationService CreateService(
        AppDbContext dbContext,
        IMemoryCache cache,
        IAdminReservationNotifier notifier)
    {
        return new ReservationService(
            dbContext,
            cache,
            notifier,
            NullLogger<ReservationService>.Instance,
            AuditTimeProvider);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static CountingAppDbContext CreateProviderlessDbContext()
    {
        return new CountingAppDbContext(new DbContextOptionsBuilder<AppDbContext>().Options);
    }

    private static Branch CreateReservableBranch()
    {
        return new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Audit Branch",
            Slug = "audit-branch",
            Address = "Audit",
            District = "Audit",
            City = "Ho Chi Minh City",
            Hotline = "0900000000",
            Email = "audit@example.test",
            OpeningTime = new TimeOnly(10, 0),
            ClosingTime = new TimeOnly(22, 0),
            Capacity = 100,
            IsActive = true,
            AllowsReservations = true
        };
    }

    private static ReservationCreateRequest CreateRequest(Guid branchId, DateOnly reservationDate)
    {
        return new ReservationCreateRequest
        {
            CustomerName = "Nguyen Van A",
            PhoneNumber = "0900000000",
            BranchId = branchId,
            GuestCount = 2,
            ReservationDate = reservationDate,
            ReservationTime = new TimeOnly(18, 30)
        };
    }

    private sealed class CapturingReservationNotifier : IAdminReservationNotifier
    {
        public int CallCount { get; private set; }

        public Task NotifyReservationCreatedAsync(
            AdminReservationCreatedNotification notification,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class CountingAppDbContext : AppDbContext
    {
        public CountingAppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public int SaveChangesAsyncCallCount { get; private set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesAsyncCallCount++;
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            SaveChangesAsyncCallCount++;
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}

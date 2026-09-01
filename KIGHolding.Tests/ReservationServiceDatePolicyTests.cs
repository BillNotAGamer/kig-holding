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
    [InlineData("2026-08-08")]
    [InlineData("2026-08-09")]
    [InlineData("2026-09-02")]
    public async Task CreateReservationAsync_BlockedDatabaseDate_DoesNotPersistOrNotify(string dateValue)
    {
        await using var dbContext = CreateDbContext();
        var blockedDate = DateOnly.ParseExact(dateValue, "yyyy-MM-dd");
        dbContext.BlockedReservationDates.Add(new BlockedReservationDate { Date = blockedDate });
        dbContext.Branches.Add(CreateReservableBranch());
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var service = CreateService(dbContext, cache, notifier);
        var request = CreateRequest(Guid.NewGuid(), blockedDate);

        var result = await service.CreateReservationAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.FieldName == nameof(ReservationCreateRequest.ReservationDate) &&
            error.Message == ReservationBlockedDateService.BlockedDateMessage);
        Assert.Equal(0, await dbContext.Reservations.CountAsync());
        Assert.Equal(0, notifier.CallCount);
        AssertRateLimitNotStamped(cache, request);
    }

    [Theory]
    [InlineData("2026-08-08")]
    [InlineData("2026-08-09")]
    [InlineData("2026-09-02")]
    [InlineData("2029-01-01")]
    [InlineData("2030-01-02")]
    public async Task CreateReservationAsync_UnblockedFutureDate_HasNoDatePolicyErrorWhenLaterBranchRuleFails(string dateValue)
    {
        await using var dbContext = CreateDbContext();
        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var service = CreateService(dbContext, cache, notifier);

        var result = await service.CreateReservationAsync(
            CreateRequest(Guid.NewGuid(), DateOnly.ParseExact(dateValue, "yyyy-MM-dd")));

        Assert.False(result.Succeeded);
        Assert.DoesNotContain(result.Errors, error => error.FieldName == nameof(ReservationCreateRequest.ReservationDate));
        Assert.Contains(result.Errors, error => error.FieldName == nameof(ReservationCreateRequest.BranchId));
        Assert.Equal(0, await dbContext.Reservations.CountAsync());
        Assert.Equal(0, notifier.CallCount);
    }

    [Fact]
    public async Task CreateReservationAsync_PastDate_ReturnsBeforeDatabaseWorkOrSideEffects()
    {
        await using var dbContext = CreateProviderlessDbContext();
        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var service = CreateService(dbContext, cache, notifier);

        var request = CreateRequest(Guid.NewGuid(), new DateOnly(2026, 8, 5));
        var result = await service.CreateReservationAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.FieldName == nameof(ReservationCreateRequest.ReservationDate) &&
            error.Message == ReservationBlockedDateService.PastDateMessage);
        Assert.Equal(0, dbContext.SaveChangesAsyncCallCount);
        Assert.Equal(0, notifier.CallCount);
        AssertRateLimitNotStamped(cache, request);
    }

    [Theory]
    [InlineData("2026-08-08")]
    [InlineData("2026-08-09")]
    [InlineData("2026-09-02")]
    public async Task CreateReservationAsync_BlockedDate_ReturnsBeforeBranchQueryOrSideEffects(string dateValue)
    {
        await using var dbContext = CreateProviderlessDbContext();
        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var service = CreateService(
            dbContext,
            cache,
            notifier,
            new StubBlockedDateService(ReservationDatePolicyStatus.BlockedDate));

        var request = CreateRequest(Guid.NewGuid(), DateOnly.ParseExact(dateValue, "yyyy-MM-dd"));
        var result = await service.CreateReservationAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.FieldName == nameof(ReservationCreateRequest.ReservationDate) &&
            error.Message == ReservationBlockedDateService.BlockedDateMessage);
        Assert.Equal(0, dbContext.SaveChangesAsyncCallCount);
        Assert.Equal(0, notifier.CallCount);
        AssertRateLimitNotStamped(cache, request);
    }

    private static ReservationService CreateService(
        AppDbContext dbContext,
        IMemoryCache cache,
        IAdminReservationNotifier notifier,
        IReservationBlockedDateService? blockedDateService = null)
    {
        return new ReservationService(
            dbContext,
            cache,
            notifier,
            blockedDateService ?? new ReservationBlockedDateService(dbContext),
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

    private static void AssertRateLimitNotStamped(IMemoryCache cache, ReservationCreateRequest request)
    {
        var phoneLockKey = IdentityNormalizer.PhoneLockKey(IdentityNormalizer.NormalizePhone(request.PhoneNumber));
        Assert.False(cache.TryGetValue(phoneLockKey, out _));
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

    private sealed class StubBlockedDateService : IReservationBlockedDateService
    {
        private readonly ReservationDatePolicyStatus _status;

        public StubBlockedDateService(ReservationDatePolicyStatus status)
        {
            _status = status;
        }

        public Task<ReservationDatePolicyResult> EvaluateReservationDateAsync(
            DateOnly reservationDate,
            DateOnly vietnamToday,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ReservationDatePolicyResult(_status));
        }

        public Task<bool> IsBlockedAsync(DateOnly date, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_status == ReservationDatePolicyStatus.BlockedDate);
        }

        public Task<IReadOnlyList<DateOnly>> GetActiveBlockedDatesAsync(
            DateOnly vietnamToday,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DateOnly>>([]);
        }

        public Task ReplaceActiveBlockedDatesAsync(
            IReadOnlyCollection<DateOnly> dates,
            DateOnly vietnamToday,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<int> CleanupPastDatesAsync(DateOnly vietnamToday, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
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

using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using KIGHolding.Services;
using KIGHolding.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

    [Fact]
    public async Task CreateReservationAsync_BranchClosed_DoesNotPersistOrNotify()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateReservableBranch();
        var date = new DateOnly(2026, 8, 10);
        dbContext.Branches.Add(branch);
        dbContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
        {
            BranchId = branch.Id,
            Date = date,
            RuleType = BranchReservationRuleType.Closed
        });
        await dbContext.SaveChangesAsync();
        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var request = CreateRequest(branch.Id, date);

        var result = await CreateService(dbContext, cache, notifier).CreateReservationAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.FieldName == nameof(ReservationCreateRequest.ReservationDate) &&
            error.Message == ReservationPolicyMessages.BranchClosedDate);
        Assert.Empty(await dbContext.Reservations.ToListAsync());
        Assert.Equal(0, notifier.CallCount);
        AssertRateLimitNotStamped(cache, request);
    }

    [Fact]
    public async Task CreateReservationAsync_MinimumGuestsNotMet_DoesNotPersistOrNotify()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateReservableBranch();
        var date = new DateOnly(2026, 8, 10);
        dbContext.Branches.Add(branch);
        dbContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
        {
            BranchId = branch.Id,
            Date = date,
            RuleType = BranchReservationRuleType.MinimumGuests,
            MinGuests = 6
        });
        await dbContext.SaveChangesAsync();
        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var request = CreateRequest(branch.Id, date);
        request.GuestCount = 5;

        var result = await CreateService(dbContext, cache, notifier).CreateReservationAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.FieldName == nameof(ReservationCreateRequest.GuestCount) &&
            error.Message.Contains("6", StringComparison.Ordinal));
        Assert.Empty(await dbContext.Reservations.ToListAsync());
        Assert.Equal(0, notifier.CallCount);
        AssertRateLimitNotStamped(cache, request);
    }

    [Fact]
    public async Task CreateReservationAsync_FinalCheckSeesBranchPolicyCommittedBeforeDateLockReturns()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateReservableBranch();
        var date = new DateOnly(2026, 8, 10);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        var lockService = new CallbackPolicyLockService(async lockedDate =>
        {
            dbContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
            {
                BranchId = branch.Id,
                Date = lockedDate,
                RuleType = BranchReservationRuleType.Closed
            });
            await dbContext.SaveChangesAsync();
        });
        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var request = CreateRequest(branch.Id, date);

        var result = await CreateService(dbContext, cache, notifier, policyLockService: lockService)
            .CreateReservationAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Message == ReservationPolicyMessages.BranchClosedDate);
        Assert.Equal([date], lockService.AcquiredDates);
        Assert.Empty(await dbContext.Reservations.ToListAsync());
        Assert.Equal(0, notifier.CallCount);
        AssertRateLimitNotStamped(cache, request);
    }

    [Fact]
    public async Task CreateReservationAsync_FinalCheckSeesGlobalPolicyCommittedBeforeDateLockReturns()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateReservableBranch();
        var date = new DateOnly(2026, 8, 10);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        var lockService = new CallbackPolicyLockService(async lockedDate =>
        {
            dbContext.BlockedReservationDates.Add(new BlockedReservationDate { Date = lockedDate });
            await dbContext.SaveChangesAsync();
        });
        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var request = CreateRequest(branch.Id, date);

        var result = await CreateService(dbContext, cache, notifier, policyLockService: lockService)
            .CreateReservationAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Message == ReservationBlockedDateService.BlockedDateMessage);
        Assert.Empty(await dbContext.Reservations.ToListAsync());
        Assert.Equal(0, notifier.CallCount);
        AssertRateLimitNotStamped(cache, request);
    }

    [Fact]
    public async Task CreateReservationAsync_FinalCheckSeesNewMinimumGuestsPolicy()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateReservableBranch();
        var date = new DateOnly(2026, 8, 10);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        var lockService = new CallbackPolicyLockService(async lockedDate =>
        {
            dbContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
            {
                BranchId = branch.Id,
                Date = lockedDate,
                RuleType = BranchReservationRuleType.MinimumGuests,
                MinGuests = 6
            });
            await dbContext.SaveChangesAsync();
        });
        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var request = CreateRequest(branch.Id, date);
        request.GuestCount = 5;

        var result = await CreateService(dbContext, cache, notifier, policyLockService: lockService)
            .CreateReservationAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.FieldName == nameof(ReservationCreateRequest.GuestCount) &&
            error.Message.Contains("6", StringComparison.Ordinal));
        Assert.Empty(await dbContext.Reservations.ToListAsync());
        Assert.Equal(0, notifier.CallCount);
        AssertRateLimitNotStamped(cache, request);
    }

    [Fact]
    public async Task CreateReservationAsync_WeekdayDuringLunchBreak_FailsWithLunchBreakError()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateReservableBranch();
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();

        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var service = CreateService(dbContext, cache, notifier);

        // 2026-08-10 is a Monday (weekday)
        var request = CreateRequest(branch.Id, new DateOnly(2026, 8, 10));
        request.ReservationTime = new TimeOnly(15, 0); // inside 14:30 - 16:30

        var result = await service.CreateReservationAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.FieldName == nameof(ReservationCreateRequest.ReservationTime) &&
            error.Message.Contains("nghỉ trưa", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateReservationAsync_WeekendDuringLunchBreak_NormalBranch_Succeeds()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateReservableBranch();
        branch.LunchBreakAppliesOnWeekend = false;
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();

        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var service = CreateService(dbContext, cache, notifier);

        // 2026-08-09 is a Sunday (weekend)
        var request = CreateRequest(branch.Id, new DateOnly(2026, 8, 9));
        request.ReservationTime = new TimeOnly(15, 0); // 15:00 on weekend for normal branch is allowed

        var result = await service.CreateReservationAsync(request);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task CreateReservationAsync_WeekendDuringLunchBreak_BranchWithLunchBreakAppliesOnWeekend_Fails()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateReservableBranch();
        branch.LunchBreakAppliesOnWeekend = true;
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();

        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var service = CreateService(dbContext, cache, notifier);

        // 2026-08-09 is a Sunday (weekend)
        var request = CreateRequest(branch.Id, new DateOnly(2026, 8, 9));
        request.ReservationTime = new TimeOnly(15, 0); // 15:00 on weekend for branch with weekend lunch break must fail

        var result = await service.CreateReservationAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error =>
            error.FieldName == nameof(ReservationCreateRequest.ReservationTime) &&
            error.Message.Contains("nghỉ trưa", StringComparison.OrdinalIgnoreCase) &&
            error.Message.Contains("tất cả các ngày trong tuần", StringComparison.OrdinalIgnoreCase));
    }

    private static ReservationService CreateService(
        AppDbContext dbContext,
        IMemoryCache cache,
        IAdminReservationNotifier notifier,
        IReservationBlockedDateService? blockedDateService = null,
        IReservationPolicyLockService? policyLockService = null)
    {
        policyLockService ??= new TestReservationPolicyLockService();
        var globalPolicyService = blockedDateService
            ?? new ReservationBlockedDateService(dbContext, policyLockService);
        var branchPolicyService = new BranchReservationDatePolicyService(
            dbContext,
            policyLockService,
            NullLogger<BranchReservationDatePolicyService>.Instance,
            AuditTimeProvider);
        var policyEvaluator = new ReservationPolicyEvaluator(
            dbContext,
            globalPolicyService,
            branchPolicyService);

        return new ReservationService(
            dbContext,
            cache,
            notifier,
            globalPolicyService,
            branchPolicyService,
            policyEvaluator,
            policyLockService,
            NullLogger<ReservationService>.Instance,
            AuditTimeProvider);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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

    private sealed class CallbackPolicyLockService : IReservationPolicyLockService
    {
        private readonly Func<DateOnly, Task> _onDateLock;

        public CallbackPolicyLockService(Func<DateOnly, Task> onDateLock)
        {
            _onDateLock = onDateLock;
        }

        public List<DateOnly> AcquiredDates { get; } = [];

        public async Task AcquireDateLockAsync(DateOnly date, CancellationToken cancellationToken = default)
        {
            AcquiredDates.Add(date);
            await _onDateLock(date);
        }

        public async Task AcquireDateLocksAsync(
            IEnumerable<DateOnly> dates,
            CancellationToken cancellationToken = default)
        {
            foreach (var date in dates.Distinct().OrderBy(date => date))
            {
                await AcquireDateLockAsync(date, cancellationToken);
            }
        }

        public Task AcquireGlobalMutationLockAsync(CancellationToken cancellationToken = default)
        {
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

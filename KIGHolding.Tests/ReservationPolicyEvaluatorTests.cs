using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using KIGHolding.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIGHolding.Tests;

public sealed class ReservationPolicyEvaluatorTests
{
    private static readonly DateOnly VietnamToday = new(2026, 10, 19);
    private static readonly TimeProvider TimeProvider =
        new FixedTimeProvider(DateTimeOffset.Parse("2026-10-18T17:00:00+00:00"));

    [Fact]
    public async Task Evaluate_NoGlobalOrBranchPolicy_ReturnsAllowed()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();

        var result = await CreateEvaluator(dbContext).EvaluateAsync(
            branch.Id,
            new DateOnly(2026, 10, 20),
            2,
            VietnamToday);

        Assert.Equal(ReservationPolicyEvaluationStatus.Allowed, result.Status);
    }

    [Fact]
    public async Task Evaluate_GlobalBlockWinsOverBranchMinimumGuests()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        var date = new DateOnly(2026, 10, 20);
        dbContext.Branches.Add(branch);
        dbContext.BlockedReservationDates.Add(new BlockedReservationDate { Date = date });
        dbContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
        {
            BranchId = branch.Id,
            Date = date,
            RuleType = BranchReservationRuleType.MinimumGuests,
            MinGuests = 20
        });
        await dbContext.SaveChangesAsync();

        var result = await CreateEvaluator(dbContext).EvaluateAsync(
            branch.Id,
            date,
            25,
            VietnamToday);

        Assert.Equal(ReservationPolicyEvaluationStatus.GlobalBlockedDate, result.Status);
    }

    [Fact]
    public async Task Evaluate_GlobalBlockWinsWithoutBranchPolicyOrValidBranch()
    {
        await using var dbContext = CreateDbContext();
        var date = new DateOnly(2026, 10, 20);
        dbContext.BlockedReservationDates.Add(new BlockedReservationDate { Date = date });
        await dbContext.SaveChangesAsync();

        var result = await CreateEvaluator(dbContext).EvaluateAsync(
            Guid.NewGuid(),
            date,
            2,
            VietnamToday);

        Assert.Equal(ReservationPolicyEvaluationStatus.GlobalBlockedDate, result.Status);
    }

    [Fact]
    public async Task Evaluate_InvalidBranchPrecedesInvalidGuestCount()
    {
        await using var dbContext = CreateDbContext();

        var result = await CreateEvaluator(dbContext).EvaluateAsync(
            Guid.NewGuid(),
            new DateOnly(2026, 10, 20),
            0,
            VietnamToday);

        Assert.Equal(ReservationPolicyEvaluationStatus.InvalidBranch, result.Status);
    }

    [Fact]
    public async Task Evaluate_BranchClosed_RejectsOnlyThatBranch()
    {
        await using var dbContext = CreateDbContext();
        var closedBranch = CreateBranch();
        var openBranch = CreateBranch();
        var date = new DateOnly(2026, 10, 24);
        dbContext.Branches.AddRange(closedBranch, openBranch);
        dbContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
        {
            BranchId = closedBranch.Id,
            Date = date,
            RuleType = BranchReservationRuleType.Closed
        });
        await dbContext.SaveChangesAsync();

        var evaluator = CreateEvaluator(dbContext);
        var closedResult = await evaluator.EvaluateAsync(closedBranch.Id, date, 2, VietnamToday);
        var openResult = await evaluator.EvaluateAsync(openBranch.Id, date, 2, VietnamToday);

        Assert.Equal(ReservationPolicyEvaluationStatus.BranchClosedDate, closedResult.Status);
        Assert.Equal(ReservationPolicyEvaluationStatus.Allowed, openResult.Status);
    }

    [Theory]
    [InlineData(5, ReservationPolicyEvaluationStatus.MinimumGuestsNotMet)]
    [InlineData(6, ReservationPolicyEvaluationStatus.Allowed)]
    [InlineData(10, ReservationPolicyEvaluationStatus.Allowed)]
    public async Task Evaluate_MinimumGuests_UsesPersistedGuestCount(
        int guestCount,
        ReservationPolicyEvaluationStatus expectedStatus)
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        var date = new DateOnly(2026, 10, 20);
        dbContext.Branches.Add(branch);
        dbContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
        {
            BranchId = branch.Id,
            Date = date,
            RuleType = BranchReservationRuleType.MinimumGuests,
            MinGuests = 6
        });
        await dbContext.SaveChangesAsync();

        var result = await CreateEvaluator(dbContext).EvaluateAsync(
            branch.Id,
            date,
            guestCount,
            VietnamToday);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedStatus == ReservationPolicyEvaluationStatus.MinimumGuestsNotMet ? 6 : null, result.MinGuests);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Evaluate_InvalidGuestCount_ReturnsTypedFailure(int guestCount)
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();

        var result = await CreateEvaluator(dbContext).EvaluateAsync(
            branch.Id,
            new DateOnly(2026, 10, 20),
            guestCount,
            VietnamToday);

        Assert.Equal(ReservationPolicyEvaluationStatus.InvalidGuestCount, result.Status);
    }

    [Fact]
    public async Task Evaluate_PastDateWinsBeforeBranchLookup()
    {
        await using var dbContext = CreateDbContext();

        var result = await CreateEvaluator(dbContext).EvaluateAsync(
            Guid.NewGuid(),
            VietnamToday.AddDays(-1),
            2,
            VietnamToday);

        Assert.Equal(ReservationPolicyEvaluationStatus.PastDate, result.Status);
    }

    [Theory]
    [InlineData("2026-10-24")]
    [InlineData("2026-10-25")]
    [InlineData("2028-01-01")]
    [InlineData("2029-01-01")]
    public async Task Evaluate_UnblockedWeekendAndFutureYear_ReturnsAllowed(string dateValue)
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();

        var result = await CreateEvaluator(dbContext).EvaluateAsync(
            branch.Id,
            DateOnly.ParseExact(dateValue, "yyyy-MM-dd"),
            2,
            VietnamToday);

        Assert.Equal(ReservationPolicyEvaluationStatus.Allowed, result.Status);
    }

    [Theory]
    [InlineData("2026-10-24")]
    [InlineData("2026-10-25")]
    public async Task Evaluate_ClosedWeekend_ReturnsBranchClosed(string dateValue)
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        var date = DateOnly.ParseExact(dateValue, "yyyy-MM-dd");
        dbContext.Branches.Add(branch);
        dbContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
        {
            BranchId = branch.Id,
            Date = date,
            RuleType = BranchReservationRuleType.Closed
        });
        await dbContext.SaveChangesAsync();

        var result = await CreateEvaluator(dbContext).EvaluateAsync(branch.Id, date, 2, VietnamToday);

        Assert.Equal(ReservationPolicyEvaluationStatus.BranchClosedDate, result.Status);
    }

    private static ReservationPolicyEvaluator CreateEvaluator(AppDbContext dbContext)
    {
        var lockService = new TestReservationPolicyLockService();
        var globalService = new ReservationBlockedDateService(dbContext, lockService);
        var branchService = new BranchReservationDatePolicyService(
            dbContext,
            lockService,
            NullLogger<BranchReservationDatePolicyService>.Instance,
            TimeProvider);
        return new ReservationPolicyEvaluator(dbContext, globalService, branchService);
    }

    private static Branch CreateBranch()
    {
        return new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Policy Branch",
            Slug = $"policy-{Guid.NewGuid():N}",
            IsActive = true,
            AllowsReservations = true
        };
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }
}

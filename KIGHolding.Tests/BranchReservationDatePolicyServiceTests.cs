using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using KIGHolding.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace KIGHolding.Tests;

public sealed class BranchReservationDatePolicyServiceTests
{
    private static readonly DateOnly VietnamToday = new(2026, 10, 19);
    private static readonly TimeProvider TimeProvider =
        new FixedTimeProvider(DateTimeOffset.Parse("2026-10-18T17:00:00+00:00"));

    [Fact]
    public async Task SetPolicy_CreateUpdateAndStaleUpdate_UseExpectedVersion()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        var policyLock = new TestReservationPolicyLockService();
        var service = CreateService(dbContext, policyLock);
        var date = VietnamToday.AddDays(1);

        var created = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id,
            date,
            BranchReservationRuleType.Closed,
            null,
            ExpectedVersion: null));
        var duplicateCreate = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id,
            date,
            BranchReservationRuleType.MinimumGuests,
            6,
            ExpectedVersion: null));
        var updated = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id,
            date,
            BranchReservationRuleType.MinimumGuests,
            6,
            ExpectedVersion: created.Policy!.Version));
        var staleUpdate = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id,
            date,
            BranchReservationRuleType.Closed,
            null,
            ExpectedVersion: created.Policy.Version));

        Assert.True(created.Succeeded);
        Assert.NotEqual(Guid.Empty, created.Policy.Version);
        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, duplicateCreate.Status);
        Assert.True(updated.Succeeded);
        Assert.NotEqual(Guid.Empty, updated.Policy?.Version);
        Assert.NotEqual(created.Policy.Version, updated.Policy?.Version);
        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, staleUpdate.Status);
        Assert.Equal([date, date, date, date], policyLock.AcquiredDates);

        var persisted = await service.GetPolicyAsync(branch.Id, date);
        Assert.NotNull(persisted);
        Assert.Equal(BranchReservationRuleType.MinimumGuests, persisted.RuleType);
        Assert.Equal(6, persisted.MinGuests);
        Assert.Equal(updated.Policy?.Version, persisted.Version);
    }

    [Fact]
    public async Task RemovePolicy_MatchingRevisionDeletesAndStaleRevisionConflicts()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        var date = VietnamToday.AddDays(1);
        var currentVersion = Guid.NewGuid();
        var staleVersion = Guid.NewGuid();
        dbContext.Branches.Add(branch);
        dbContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
        {
            BranchId = branch.Id,
            Date = date,
            RuleType = BranchReservationRuleType.Closed,
            Version = currentVersion
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var stale = await service.RemovePolicyAsync(branch.Id, date, expectedVersion: staleVersion);
        var removed = await service.RemovePolicyAsync(branch.Id, date, expectedVersion: currentVersion);
        var alreadyRemoved = await service.RemovePolicyAsync(branch.Id, date, expectedVersion: currentVersion);

        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, stale.Status);
        Assert.True(removed.Succeeded);
        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, alreadyRemoved.Status);
        Assert.Null(await service.GetPolicyAsync(branch.Id, date));
    }

    [Fact]
    public async Task RecreatePolicy_GeneratesRevisionDifferentFromDeletedIncarnation()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        var date = VietnamToday.AddDays(1);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var created = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));
        var firstVersion = created.Policy!.Version;
        var removed = await service.RemovePolicyAsync(branch.Id, date, firstVersion);
        var recreated = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id, date, BranchReservationRuleType.MinimumGuests, 6, ExpectedVersion: null));

        Assert.True(created.Succeeded);
        Assert.True(removed.Succeeded);
        Assert.True(recreated.Succeeded);
        Assert.NotEqual(Guid.Empty, firstVersion);
        Assert.NotEqual(Guid.Empty, recreated.Policy!.Version);
        Assert.NotEqual(firstVersion, recreated.Policy.Version);
    }

    [Fact]
    public async Task RecreatePolicy_StaleRevisionCannotUpdateNewIncarnation()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        var date = VietnamToday.AddDays(1);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var original = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));
        var staleVersion = original.Policy!.Version;
        await service.RemovePolicyAsync(branch.Id, date, staleVersion);
        var recreated = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id, date, BranchReservationRuleType.MinimumGuests, 6, ExpectedVersion: null));

        var staleUpdate = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: staleVersion));

        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, staleUpdate.Status);
        var persisted = await service.GetPolicyAsync(branch.Id, date);
        Assert.Equal(recreated.Policy!.Version, persisted!.Version);
        Assert.Equal(BranchReservationRuleType.MinimumGuests, persisted.RuleType);
        Assert.Equal(6, persisted.MinGuests);
    }

    [Fact]
    public async Task RecreatePolicy_StaleRevisionCannotDeleteNewIncarnation()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        var date = VietnamToday.AddDays(1);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var original = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));
        var staleVersion = original.Policy!.Version;
        await service.RemovePolicyAsync(branch.Id, date, staleVersion);
        var recreated = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id, date, BranchReservationRuleType.MinimumGuests, 6, ExpectedVersion: null));

        var staleDelete = await service.RemovePolicyAsync(branch.Id, date, staleVersion);

        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, staleDelete.Status);
        var persisted = await service.GetPolicyAsync(branch.Id, date);
        Assert.Equal(recreated.Policy!.Version, persisted!.Version);
    }

    [Fact]
    public async Task SetPolicy_ConcurrencyConflict_DetachesOnlyFailedPolicyAndAllowsDifferentBranch()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var databaseRoot = new InMemoryDatabaseRoot();
        await using var firstContext = CreateDbContext(databaseName, databaseRoot);
        await using var competingContext = CreateDbContext(databaseName, databaseRoot);
        var branchA = CreateBranch();
        var branchB = CreateBranch();
        var branchC = CreateBranch();
        var date = VietnamToday.AddDays(1);
        var unrelatedDate = date.AddDays(1);
        var unrelatedPolicy = CreatePolicy(branchC.Id, unrelatedDate);
        firstContext.Branches.AddRange(branchA, branchB, branchC);
        firstContext.BranchReservationDatePolicies.Add(unrelatedPolicy);
        await firstContext.SaveChangesAsync();
        var firstService = CreateService(firstContext);
        var competingService = CreateService(competingContext);

        var created = await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));
        var competingUpdate = await competingService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id,
            date,
            BranchReservationRuleType.MinimumGuests,
            6,
            ExpectedVersion: created.Policy!.Version));

        var staleUpdate = await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id,
            date,
            BranchReservationRuleType.MinimumGuests,
            8,
            ExpectedVersion: created.Policy.Version));

        Assert.True(competingUpdate.Succeeded);
        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, staleUpdate.Status);
        Assert.DoesNotContain(
            firstContext.ChangeTracker.Entries<BranchReservationDatePolicy>(),
            entry => entry.Entity.BranchId == branchA.Id && entry.Entity.Date == date);
        Assert.Equal(EntityState.Unchanged, firstContext.Entry(unrelatedPolicy).State);
        Assert.Equal(EntityState.Unchanged, firstContext.Entry(branchB).State);

        var unrelatedMutation = await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchB.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));

        Assert.True(unrelatedMutation.Succeeded);
        await using var verificationContext = CreateDbContext(databaseName, databaseRoot);
        var persistedA = await verificationContext.BranchReservationDatePolicies
            .AsNoTracking()
            .SingleAsync(policy => policy.BranchId == branchA.Id && policy.Date == date);
        Assert.Equal(competingUpdate.Policy!.Version, persistedA.Version);
        Assert.Equal(6, persistedA.MinGuests);
        Assert.True(await verificationContext.BranchReservationDatePolicies
            .AsNoTracking()
            .AnyAsync(policy => policy.BranchId == branchB.Id && policy.Date == date));
    }

    [Fact]
    public async Task RemovePolicy_ConcurrencyConflict_DetachesOnlyFailedPolicyAndAllowsDifferentBranch()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var databaseRoot = new InMemoryDatabaseRoot();
        await using var firstContext = CreateDbContext(databaseName, databaseRoot);
        await using var competingContext = CreateDbContext(databaseName, databaseRoot);
        var branchA = CreateBranch();
        var branchB = CreateBranch();
        var date = VietnamToday.AddDays(1);
        firstContext.Branches.AddRange(branchA, branchB);
        await firstContext.SaveChangesAsync();
        var firstService = CreateService(firstContext);
        var competingService = CreateService(competingContext);
        var created = await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));
        var competingUpdate = await competingService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id,
            date,
            BranchReservationRuleType.MinimumGuests,
            6,
            ExpectedVersion: created.Policy!.Version));

        var staleDelete = await firstService.RemovePolicyAsync(branchA.Id, date, created.Policy.Version);

        Assert.True(competingUpdate.Succeeded);
        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, staleDelete.Status);
        Assert.DoesNotContain(
            firstContext.ChangeTracker.Entries<BranchReservationDatePolicy>(),
            entry => entry.Entity.BranchId == branchA.Id && entry.Entity.Date == date);
        Assert.Equal(EntityState.Unchanged, firstContext.Entry(branchB).State);

        var unrelatedMutation = await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchB.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));

        Assert.True(unrelatedMutation.Succeeded);
        await using var verificationContext = CreateDbContext(databaseName, databaseRoot);
        Assert.True(await verificationContext.BranchReservationDatePolicies
            .AsNoTracking()
            .AnyAsync(policy => policy.BranchId == branchA.Id && policy.Date == date));
        Assert.True(await verificationContext.BranchReservationDatePolicies
            .AsNoTracking()
            .AnyAsync(policy => policy.BranchId == branchB.Id && policy.Date == date));
    }

    [Fact]
    public async Task SetPolicy_DuplicateCreateConflict_AllowsDifferentBranchInSameScope()
    {
        await using var dbContext = CreateDbContext();
        var branchA = CreateBranch();
        var branchB = CreateBranch();
        var date = VietnamToday.AddDays(1);
        dbContext.Branches.AddRange(branchA, branchB);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var created = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));

        var duplicate = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, date, BranchReservationRuleType.MinimumGuests, 6, ExpectedVersion: null));
        var unrelatedMutation = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchB.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));

        Assert.True(created.Succeeded);
        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, duplicate.Status);
        Assert.True(unrelatedMutation.Succeeded);
        Assert.Equal(2, await dbContext.BranchReservationDatePolicies.CountAsync());
    }

    [Fact]
    public async Task SetPolicy_DatabaseUniqueViolation_DetachesFailedInsertAndAllowsDifferentBranch()
    {
        await using var dbContext = CreateUniqueViolationDbContext();
        var branchA = CreateBranch();
        var branchB = CreateBranch();
        var date = VietnamToday.AddDays(1);
        dbContext.Branches.AddRange(branchA, branchB);
        await dbContext.SaveChangesAsync();
        dbContext.ThrowUniqueViolationOnNextSave = true;
        var service = CreateService(dbContext);

        var duplicate = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));

        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, duplicate.Status);
        Assert.DoesNotContain(
            dbContext.ChangeTracker.Entries<BranchReservationDatePolicy>(),
            entry => entry.Entity.BranchId == branchA.Id && entry.Entity.Date == date);
        Assert.Equal(EntityState.Unchanged, dbContext.Entry(branchB).State);

        var unrelatedMutation = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchB.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));

        Assert.True(unrelatedMutation.Succeeded);
        Assert.Single(await dbContext.BranchReservationDatePolicies.AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData(BranchReservationRuleType.Closed, 6)]
    [InlineData(BranchReservationRuleType.MinimumGuests, null)]
    [InlineData(BranchReservationRuleType.MinimumGuests, 0)]
    [InlineData(BranchReservationRuleType.MinimumGuests, 101)]
    public async Task SetPolicy_InvalidRuleCombination_ReturnsValidationFailure(
        BranchReservationRuleType ruleType,
        int? minGuests)
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            Guid.NewGuid(),
            VietnamToday.AddDays(1),
            ruleType,
            minGuests,
            ExpectedVersion: null));

        Assert.Equal(BranchReservationPolicyMutationStatus.ValidationFailed, result.Status);
        Assert.Empty(dbContext.BranchReservationDatePolicies);
    }

    [Fact]
    public async Task SetPolicy_EmptyExpectedRevision_ReturnsValidationFailure()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            Guid.NewGuid(),
            VietnamToday.AddDays(1),
            BranchReservationRuleType.Closed,
            null,
            ExpectedVersion: Guid.Empty));

        Assert.Equal(BranchReservationPolicyMutationStatus.ValidationFailed, result.Status);
        Assert.Empty(dbContext.BranchReservationDatePolicies);
    }

    [Fact]
    public async Task RemovePolicy_EmptyExpectedRevision_ReturnsValidationFailure()
    {
        await using var dbContext = CreateDbContext();
        var policyLock = new TestReservationPolicyLockService();
        var service = CreateService(dbContext, policyLock);

        var result = await service.RemovePolicyAsync(
            Guid.NewGuid(),
            VietnamToday.AddDays(1),
            Guid.Empty);

        Assert.Equal(BranchReservationPolicyMutationStatus.ValidationFailed, result.Status);
        Assert.Empty(policyLock.AcquiredDates);
    }

    [Fact]
    public async Task SetPolicy_UnknownRuleType_ReturnsValidationFailure()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            Guid.NewGuid(),
            VietnamToday.AddDays(1),
            (BranchReservationRuleType)999,
            null,
            ExpectedVersion: null));

        Assert.Equal(BranchReservationPolicyMutationStatus.ValidationFailed, result.Status);
    }

    [Fact]
    public async Task SetPolicy_InactiveOrNonReservableBranch_IsRejected()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        branch.AllowsReservations = false;
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id,
            VietnamToday.AddDays(1),
            BranchReservationRuleType.Closed,
            null,
            ExpectedVersion: null));

        Assert.Equal(BranchReservationPolicyMutationStatus.BranchNotReservable, result.Status);
    }

    [Fact]
    public async Task SetPolicy_ForBranchAOnSameDate_CannotModifyBranchB()
    {
        await using var dbContext = CreateDbContext();
        var branchA = CreateBranch();
        var branchB = CreateBranch();
        var date = VietnamToday.AddDays(1);
        var branchAVersion = Guid.NewGuid();
        var branchBVersion = Guid.NewGuid();
        dbContext.Branches.AddRange(branchA, branchB);
        dbContext.BranchReservationDatePolicies.AddRange(
            new BranchReservationDatePolicy
            {
                BranchId = branchA.Id,
                Date = date,
                RuleType = BranchReservationRuleType.Closed,
                Version = branchAVersion
            },
            new BranchReservationDatePolicy
            {
                BranchId = branchB.Id,
                Date = date,
                RuleType = BranchReservationRuleType.MinimumGuests,
                MinGuests = 6,
                Version = branchBVersion
            });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id,
            date,
            BranchReservationRuleType.MinimumGuests,
            8,
            ExpectedVersion: branchAVersion));

        Assert.True(result.Succeeded);
        var policyA = await service.GetPolicyAsync(branchA.Id, date);
        var policyB = await service.GetPolicyAsync(branchB.Id, date);
        Assert.Equal(BranchReservationRuleType.MinimumGuests, policyA?.RuleType);
        Assert.Equal(8, policyA?.MinGuests);
        Assert.NotEqual(branchAVersion, policyA?.Version);
        Assert.Equal(BranchReservationRuleType.MinimumGuests, policyB?.RuleType);
        Assert.Equal(6, policyB?.MinGuests);
        Assert.Equal(branchBVersion, policyB?.Version);
    }

    [Fact]
    public async Task ExistingReservationCounts_PreserveEveryStatus()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        var date = VietnamToday.AddDays(1);
        dbContext.Branches.Add(branch);
        dbContext.Reservations.AddRange(
            CreateReservation(branch.Id, date, ReservationStatus.Pending),
            CreateReservation(branch.Id, date, ReservationStatus.Pending),
            CreateReservation(branch.Id, date, ReservationStatus.Confirmed),
            CreateReservation(branch.Id, date, ReservationStatus.Cancelled));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var counts = await service.GetExistingReservationCountsAsync(branch.Id, date);

        Assert.Equal(3, counts.Count);
        Assert.Contains(counts, count => count.Status == ReservationStatus.Pending && count.Count == 2);
        Assert.Contains(counts, count => count.Status == ReservationStatus.Confirmed && count.Count == 1);
        Assert.Contains(counts, count => count.Status == ReservationStatus.Cancelled && count.Count == 1);
    }

    [Fact]
    public async Task SetPolicy_DoesNotModifyExistingReservations()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        var date = VietnamToday.AddDays(1);
        var reservation = CreateReservation(branch.Id, date, ReservationStatus.Confirmed);
        var originalUpdatedAt = reservation.UpdatedAt;
        dbContext.Branches.Add(branch);
        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync();
        originalUpdatedAt = reservation.UpdatedAt;
        var service = CreateService(dbContext);

        var result = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id,
            date,
            BranchReservationRuleType.Closed,
            null,
            ExpectedVersion: null));

        Assert.True(result.Succeeded);
        var persistedReservation = await dbContext.Reservations.AsNoTracking().SingleAsync();
        Assert.Equal(reservation.Id, persistedReservation.Id);
        Assert.Equal(ReservationStatus.Confirmed, persistedReservation.Status);
        Assert.Equal(2, persistedReservation.GuestCount);
        Assert.Equal(originalUpdatedAt, persistedReservation.UpdatedAt);
    }

    [Fact]
    public async Task SetPolicy_SaveFailure_ReturnsTypedFailureAndDoesNotPersistPolicy()
    {
        await using var dbContext = CreateThrowingDbContext();
        var branch = CreateBranch();
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        dbContext.ThrowOnSave = true;
        var service = CreateService(dbContext);

        var result = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id,
            VietnamToday.AddDays(1),
            BranchReservationRuleType.Closed,
            null,
            ExpectedVersion: null));

        Assert.Equal(BranchReservationPolicyMutationStatus.PersistenceFailure, result.Status);
        Assert.DoesNotContain(
            dbContext.ChangeTracker.Entries<BranchReservationDatePolicy>(),
            entry => entry.Entity.BranchId == branch.Id && entry.Entity.Date == VietnamToday.AddDays(1));
        Assert.Equal(EntityState.Unchanged, dbContext.Entry(branch).State);
        Assert.Empty(await dbContext.BranchReservationDatePolicies.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task CleanupPastPolicies_RemovesOnlyPastRowsAndIsIdempotent()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        dbContext.Branches.Add(branch);
        dbContext.BranchReservationDatePolicies.AddRange(
            CreatePolicy(branch.Id, VietnamToday.AddDays(-1)),
            CreatePolicy(branch.Id, VietnamToday),
            CreatePolicy(branch.Id, VietnamToday.AddDays(1)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var first = await service.CleanupPastPoliciesAsync(VietnamToday);
        var second = await service.CleanupPastPoliciesAsync(VietnamToday);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(
            [VietnamToday, VietnamToday.AddDays(1)],
            await dbContext.BranchReservationDatePolicies.OrderBy(policy => policy.Date).Select(policy => policy.Date).ToListAsync());
    }

    private static BranchReservationDatePolicyService CreateService(
        AppDbContext dbContext,
        IReservationPolicyLockService? lockService = null)
    {
        return new BranchReservationDatePolicyService(
            dbContext,
            lockService ?? new TestReservationPolicyLockService(),
            NullLogger<BranchReservationDatePolicyService>.Instance,
            TimeProvider);
    }

    private static Branch CreateBranch()
    {
        return new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Mutation Branch",
            Slug = $"mutation-{Guid.NewGuid():N}",
            IsActive = true,
            AllowsReservations = true
        };
    }

    private static BranchReservationDatePolicy CreatePolicy(Guid branchId, DateOnly date)
    {
        return new BranchReservationDatePolicy
        {
            BranchId = branchId,
            Date = date,
            RuleType = BranchReservationRuleType.Closed
        };
    }

    private static Reservation CreateReservation(Guid branchId, DateOnly date, ReservationStatus status)
    {
        return new Reservation
        {
            BranchId = branchId,
            CustomerName = "Policy Audit",
            PhoneNumber = Guid.NewGuid().ToString("N"),
            GuestCount = 2,
            ReservationDate = date,
            ReservationTime = new TimeOnly(18, 0),
            Status = status
        };
    }

    private static AppDbContext CreateDbContext()
    {
        return CreateDbContext(Guid.NewGuid().ToString("N"), new InMemoryDatabaseRoot());
    }

    private static AppDbContext CreateDbContext(string databaseName, InMemoryDatabaseRoot databaseRoot)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static ThrowingAppDbContext CreateThrowingDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ThrowingAppDbContext(options);
    }

    private static UniqueViolationAppDbContext CreateUniqueViolationDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new UniqueViolationAppDbContext(options);
    }

    private sealed class ThrowingAppDbContext : AppDbContext
    {
        public ThrowingAppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public bool ThrowOnSave { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return ThrowOnSave
                ? throw new DbUpdateException("Simulated policy persistence failure.")
                : base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class UniqueViolationAppDbContext : AppDbContext
    {
        public UniqueViolationAppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public bool ThrowUniqueViolationOnNextSave { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (!ThrowUniqueViolationOnNextSave)
            {
                return base.SaveChangesAsync(cancellationToken);
            }

            ThrowUniqueViolationOnNextSave = false;
            throw new DbUpdateException(
                "Simulated duplicate branch reservation policy.",
                new PostgresException(
                    "duplicate key value violates unique constraint",
                    "ERROR",
                    "ERROR",
                    PostgresErrorCodes.UniqueViolation));
        }
    }
}

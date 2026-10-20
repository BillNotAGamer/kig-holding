using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using KIGHolding.Services;
using KIGHolding.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace KIGHolding.Tests.Postgres;

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public sealed class BranchPolicyPostgresCollection
{
    public const string CollectionName = "BranchPolicyPostgresIntegration";
}

[Collection(BranchPolicyPostgresCollection.CollectionName)]
public sealed class BranchReservationPolicyPostgresIntegrationTests
{
    private static readonly DateOnly VietnamToday = new(2035, 6, 10);
    private static readonly TimeProvider TimeProvider =
        new FixedTimeProvider(new DateTimeOffset(2035, 6, 9, 17, 0, 0, TimeSpan.Zero));

    [PostgresIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public async Task OriginalChangeTrackerRegression_StaleUpdateThenUnrelatedMutationSucceeds()
    {
        var target = PostgresIntegrationTarget.FromEnvironment();
        await VerifyTargetAndSchemaAsync(target);
        await VerifyOriginalChangeTrackerRegressionAsync(target);
    }

    [PostgresIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public Task MigrationAndSchemaContract() =>
        VerifyTargetAndSchemaAsync(PostgresIntegrationTarget.FromEnvironment());

    [PostgresIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public Task ChangeTrackerRecovery_StaleDeleteUniqueViolationAndPersistenceFailure() =>
        VerifyAdditionalChangeTrackerRecoveryAsync(PostgresIntegrationTarget.FromEnvironment());

    [PostgresIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public Task AdvisoryLockContentionAndRelease() =>
        VerifyAdvisoryLockContentionAndReleaseAsync(PostgresIntegrationTarget.FromEnvironment());

    [PostgresIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public Task GlobalPolicyRace() =>
        VerifyGlobalPolicyRaceAsync(PostgresIntegrationTarget.FromEnvironment());

    [PostgresIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public Task BranchClosedRace() =>
        VerifyBranchClosedRaceAsync(PostgresIntegrationTarget.FromEnvironment());

    [PostgresIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public Task MinimumGuestsRace() =>
        VerifyMinimumGuestsRaceAsync(PostgresIntegrationTarget.FromEnvironment());

    [PostgresIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public Task BookingFirstOrdering() =>
        VerifyBookingFirstOrderingAsync(PostgresIntegrationTarget.FromEnvironment());

    [PostgresIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public Task UuidConcurrencyAndAba() =>
        VerifyUuidConcurrencyAndAbaAsync(PostgresIntegrationTarget.FromEnvironment());

    [PostgresIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public Task DatabaseConstraints() =>
        VerifyDatabaseConstraintsAsync(PostgresIntegrationTarget.FromEnvironment());

    [PostgresIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public Task CleanupBoundaries() =>
        VerifyCleanupBoundariesAsync(PostgresIntegrationTarget.FromEnvironment());

    [PostgresIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public Task FailureAndRollbackSafety() =>
        VerifyFailureAndRollbackSafetyAsync(PostgresIntegrationTarget.FromEnvironment());

    private static async Task VerifyTargetAndSchemaAsync(PostgresIntegrationTarget target)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();

        await using (var identityCommand = connection.CreateCommand())
        {
            identityCommand.CommandText =
                "SELECT current_database(), current_user, current_setting('cluster_name'), current_setting('transaction_isolation')";
            await using var reader = await identityCommand.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(target.Database, reader.GetString(0));
            Assert.Equal(target.Username, reader.GetString(1));
            Assert.Equal(target.ClusterName, reader.GetString(2));
            Assert.Equal("read committed", reader.GetString(3));
        }

        Assert.Equal(
            1L,
            await ExecuteScalarInt64Async(
                connection,
                "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20261019052426_AddBranchReservationDatePolicies'"));
        Assert.Equal(
            1L,
            await ExecuteScalarInt64Async(
                connection,
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'BranchReservationDatePolicies'"));

        await using (var columnsCommand = connection.CreateCommand())
        {
            columnsCommand.CommandText =
                """
                SELECT column_name, data_type, is_nullable, column_default
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'BranchReservationDatePolicies'
                ORDER BY ordinal_position
                """;
            await using var reader = await columnsCommand.ExecuteReaderAsync();
            var columns = new Dictionary<string, (string Type, string Nullable, string? Default)>(StringComparer.Ordinal);
            while (await reader.ReadAsync())
            {
                columns.Add(
                    reader.GetString(0),
                    (reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
            }

            Assert.Equal("uuid", columns["BranchId"].Type);
            Assert.Equal("date", columns["Date"].Type);
            Assert.Equal("character varying", columns["RuleType"].Type);
            Assert.Equal("integer", columns["MinGuests"].Type);
            Assert.Equal(("uuid", "NO", null), columns["Version"]);
            Assert.Equal("timestamp with time zone", columns["CreatedAt"].Type);
            Assert.Equal("timestamp with time zone", columns["UpdatedAt"].Type);
        }

        var constraints = await ReadConstraintDefinitionsAsync(connection);
        Assert.Contains("PRIMARY KEY (\"BranchId\", \"Date\")", constraints["PK_BranchReservationDatePolicies"], StringComparison.Ordinal);
        Assert.Contains("CHECK", constraints["CK_BranchReservationDatePolicies_Rule"], StringComparison.Ordinal);
        Assert.Contains("FOREIGN KEY", constraints["FK_BranchReservationDatePolicies_Branches_BranchId"], StringComparison.Ordinal);
        Assert.Contains("ON DELETE RESTRICT", constraints["FK_BranchReservationDatePolicies_Branches_BranchId"], StringComparison.Ordinal);

        await using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText =
            "SELECT indexdef FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_BranchReservationDatePolicies_Date'";
        var indexDefinition = Assert.IsType<string>(await indexCommand.ExecuteScalarAsync());
        Assert.Contains("(\"Date\")", indexDefinition, StringComparison.Ordinal);
    }

    private static async Task VerifyOriginalChangeTrackerRegressionAsync(PostgresIntegrationTarget target)
    {
        var branchA = await CreateBranchAsync(target, "tracker-update-a");
        var branchB = await CreateBranchAsync(target, "tracker-update-b");
        var date = VietnamToday.AddDays(5);
        await using var firstContext = target.CreateContext(UniqueApplicationName("tracker-update-first"));
        await using var competingContext = target.CreateContext(UniqueApplicationName("tracker-update-competing"));
        var firstService = CreateBranchPolicyService(firstContext);
        var competingService = CreateBranchPolicyService(competingContext);
        var unrelatedBranch = await firstContext.Branches.SingleAsync(branch => branch.Id == branchB.Id);

        var created = await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));
        var versionX = created.Policy!.Version;
        var competingUpdate = await competingService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, date, BranchReservationRuleType.MinimumGuests, 6, versionX));

        var staleUpdate = await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, date, BranchReservationRuleType.Closed, null, versionX));

        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, staleUpdate.Status);
        Assert.DoesNotContain(
            firstContext.ChangeTracker.Entries<BranchReservationDatePolicy>(),
            entry => entry.Entity.BranchId == branchA.Id && entry.Entity.Date == date);
        Assert.Equal(EntityState.Unchanged, firstContext.Entry(unrelatedBranch).State);

        var unrelatedMutation = await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchB.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));

        Assert.True(unrelatedMutation.Succeeded);
        await using var verificationContext = target.CreateContext(UniqueApplicationName("tracker-update-verify"));
        var persistedA = await verificationContext.BranchReservationDatePolicies.AsNoTracking()
            .SingleAsync(policy => policy.BranchId == branchA.Id && policy.Date == date);
        Assert.Equal(competingUpdate.Policy!.Version, persistedA.Version);
        Assert.Equal(6, persistedA.MinGuests);
        Assert.True(await verificationContext.BranchReservationDatePolicies.AsNoTracking()
            .AnyAsync(policy => policy.BranchId == branchB.Id && policy.Date == date));
    }

    private static async Task VerifyAdditionalChangeTrackerRecoveryAsync(PostgresIntegrationTarget target)
    {
        await VerifyStaleDeleteRecoveryAsync(target);
        await VerifyUniqueViolationRecoveryAsync(target);
        await VerifyGenericPersistenceRecoveryAsync(target);
    }

    private static async Task VerifyStaleDeleteRecoveryAsync(PostgresIntegrationTarget target)
    {
        var branchA = await CreateBranchAsync(target, "tracker-delete-a");
        var branchB = await CreateBranchAsync(target, "tracker-delete-b");
        var date = VietnamToday.AddDays(6);
        await using var firstContext = target.CreateContext(UniqueApplicationName("tracker-delete-first"));
        await using var competingContext = target.CreateContext(UniqueApplicationName("tracker-delete-competing"));
        var firstService = CreateBranchPolicyService(firstContext);
        var competingService = CreateBranchPolicyService(competingContext);
        var unrelatedBranch = await firstContext.Branches.SingleAsync(branch => branch.Id == branchB.Id);
        var created = await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));
        var versionX = created.Policy!.Version;
        var competingUpdate = await competingService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, date, BranchReservationRuleType.MinimumGuests, 6, versionX));

        var staleDelete = await firstService.RemovePolicyAsync(branchA.Id, date, versionX);

        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, staleDelete.Status);
        Assert.DoesNotContain(
            firstContext.ChangeTracker.Entries<BranchReservationDatePolicy>(),
            entry => entry.Entity.BranchId == branchA.Id && entry.Entity.Date == date);
        Assert.Equal(EntityState.Unchanged, firstContext.Entry(unrelatedBranch).State);
        Assert.True((await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchB.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null))).Succeeded);

        await using var verificationContext = target.CreateContext(UniqueApplicationName("tracker-delete-verify"));
        Assert.Equal(
            competingUpdate.Policy!.Version,
            (await verificationContext.BranchReservationDatePolicies.AsNoTracking()
                .SingleAsync(policy => policy.BranchId == branchA.Id && policy.Date == date)).Version);
    }

    private static async Task VerifyUniqueViolationRecoveryAsync(PostgresIntegrationTarget target)
    {
        var branchA = await CreateBranchAsync(target, "tracker-unique-a");
        var branchB = await CreateBranchAsync(target, "tracker-unique-b");
        var date = VietnamToday.AddDays(7);
        var collision = new PolicyInsertCollisionInterceptor(target.ConnectionString, branchA.Id, date);
        await using var context = target.CreateContext(UniqueApplicationName("tracker-unique"), collision);
        var unrelatedBranch = await context.Branches.SingleAsync(branch => branch.Id == branchB.Id);
        var service = CreateBranchPolicyService(context);

        var duplicate = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));

        Assert.True(collision.Triggered);
        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, duplicate.Status);
        Assert.DoesNotContain(
            context.ChangeTracker.Entries<BranchReservationDatePolicy>(),
            entry => entry.Entity.BranchId == branchA.Id && entry.Entity.Date == date);
        Assert.Equal(EntityState.Unchanged, context.Entry(unrelatedBranch).State);
        Assert.True((await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchB.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null))).Succeeded);
    }

    private static async Task VerifyGenericPersistenceRecoveryAsync(PostgresIntegrationTarget target)
    {
        var branchA = await CreateBranchAsync(target, "tracker-failure-a");
        var branchB = await CreateBranchAsync(target, "tracker-failure-b");
        var date = VietnamToday.AddDays(8);
        var invalidRule = new PolicyCheckViolationInterceptor();
        await using var context = target.CreateContext(UniqueApplicationName("tracker-failure"), invalidRule);
        var unrelatedBranch = await context.Branches.SingleAsync(branch => branch.Id == branchB.Id);
        var service = CreateBranchPolicyService(context);

        var failed = await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null));

        Assert.True(invalidRule.Triggered);
        Assert.Equal(BranchReservationPolicyMutationStatus.PersistenceFailure, failed.Status);
        Assert.DoesNotContain(
            context.ChangeTracker.Entries<BranchReservationDatePolicy>(),
            entry => entry.Entity.BranchId == branchA.Id && entry.Entity.Date == date);
        Assert.Equal(EntityState.Unchanged, context.Entry(unrelatedBranch).State);
        Assert.True((await service.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchB.Id, date, BranchReservationRuleType.Closed, null, ExpectedVersion: null))).Succeeded);

        await using var verificationContext = target.CreateContext(UniqueApplicationName("tracker-failure-verify"));
        Assert.False(await verificationContext.BranchReservationDatePolicies.AsNoTracking()
            .AnyAsync(policy => policy.BranchId == branchA.Id && policy.Date == date));
    }

    private static async Task VerifyAdvisoryLockContentionAndReleaseAsync(PostgresIntegrationTarget target)
    {
        var date = VietnamToday.AddDays(10);
        await VerifyLockReleaseAsync(target, date, commitFirstTransaction: true);
        await VerifyLockReleaseAsync(target, date.AddDays(1), commitFirstTransaction: false);

        var firstApplication = UniqueApplicationName("different-a");
        var secondApplication = UniqueApplicationName("different-b");
        await using var firstContext = target.CreateContext(firstApplication);
        await using var secondContext = target.CreateContext(secondApplication);
        await using var firstTransaction = await firstContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var secondTransaction = await secondContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var firstBackendPid = await GetBackendPidAsync(firstContext);
        var secondBackendPid = await GetBackendPidAsync(secondContext);
        Assert.NotEqual(firstBackendPid, secondBackendPid);
        Console.WriteLine($"PostgreSQL different-date backend PIDs: {firstBackendPid}, {secondBackendPid}");

        await new ReservationPolicyLockService(firstContext).AcquireDateLockAsync(date.AddDays(2));
        await new ReservationPolicyLockService(secondContext)
            .AcquireDateLockAsync(date.AddDays(3))
            .WaitAsync(TimeSpan.FromSeconds(3));

        await secondTransaction.RollbackAsync();
        await firstTransaction.RollbackAsync();
    }

    private static async Task VerifyLockReleaseAsync(
        PostgresIntegrationTarget target,
        DateOnly date,
        bool commitFirstTransaction)
    {
        var firstApplication = UniqueApplicationName("holder");
        var secondApplication = UniqueApplicationName(commitFirstTransaction ? "commit-waiter" : "rollback-waiter");
        await using var firstContext = target.CreateContext(firstApplication);
        await using var secondContext = target.CreateContext(secondApplication);
        await using var firstTransaction = await firstContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var secondTransaction = await secondContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var firstBackendPid = await GetBackendPidAsync(firstContext);
        var secondBackendPid = await GetBackendPidAsync(secondContext);
        Assert.NotEqual(firstBackendPid, secondBackendPid);
        Console.WriteLine($"PostgreSQL same-date backend PIDs: {firstBackendPid}, {secondBackendPid}");

        await AssertReadCommittedAsync(firstContext);
        await AssertReadCommittedAsync(secondContext);
        await new ReservationPolicyLockService(firstContext).AcquireDateLockAsync(date);
        var waitingLock = new ReservationPolicyLockService(secondContext).AcquireDateLockAsync(date);
        await WaitForAdvisoryWaitAsync(target, secondApplication);
        Assert.False(waitingLock.IsCompleted);

        if (commitFirstTransaction)
        {
            await firstTransaction.CommitAsync();
        }
        else
        {
            await firstTransaction.RollbackAsync();
        }

        await waitingLock.WaitAsync(TimeSpan.FromSeconds(5));
        await secondTransaction.RollbackAsync();
    }

    private static async Task VerifyGlobalPolicyRaceAsync(PostgresIntegrationTarget target)
    {
        var date = VietnamToday.AddDays(20);
        var branch = await CreateBranchAsync(target, "global-race");

        await using (var setupContext = target.CreateContext(UniqueApplicationName("global-setup")))
        {
            setupContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
            {
                BranchId = branch.Id,
                Date = date,
                RuleType = BranchReservationRuleType.MinimumGuests,
                MinGuests = 1
            });
            await setupContext.SaveChangesAsync();
        }

        await using var adminContext = target.CreateContext(UniqueApplicationName("global-admin"));
        await using var adminTransaction = await adminContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await new ReservationPolicyLockService(adminContext).AcquireDateLockAsync(date);
        adminContext.BlockedReservationDates.Add(new BlockedReservationDate { Date = date });
        await adminContext.SaveChangesAsync();

        var bookingApplication = UniqueApplicationName("global-booking");
        await using var bookingContext = target.CreateContext(bookingApplication);
        using var cache = CreateCache();
        var notifier = new CapturingNotifier();
        var service = CreateReservationService(bookingContext, cache, notifier);
        var request = CreateRequest(branch.Id, date, 8, "0000000020");
        var bookingTask = service.CreateReservationAsync(request);

        await WaitForAdvisoryWaitAsync(target, bookingApplication);
        await adminTransaction.CommitAsync();
        var result = await bookingTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Message == ReservationBlockedDateService.BlockedDateMessage);
        Assert.Equal(0, notifier.CallCount);
        Assert.False(cache.TryGetValue(IdentityNormalizer.PhoneLockKey("0000000020"), out _));
        Assert.Equal(0, await CountReservationsAsync(target, branch.Id, date));
    }

    private static async Task VerifyBranchClosedRaceAsync(PostgresIntegrationTarget target)
    {
        var date = VietnamToday.AddDays(30);
        var branchA = await CreateBranchAsync(target, "branch-closed-a");
        var branchB = await CreateBranchAsync(target, "branch-closed-b");

        await using var adminContext = target.CreateContext(UniqueApplicationName("branch-admin"));
        await using var adminTransaction = await adminContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await new ReservationPolicyLockService(adminContext).AcquireDateLockAsync(date);
        adminContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
        {
            BranchId = branchA.Id,
            Date = date,
            RuleType = BranchReservationRuleType.Closed
        });
        await adminContext.SaveChangesAsync();

        var branchAApplication = UniqueApplicationName("branch-a-booking");
        var branchBApplication = UniqueApplicationName("branch-b-booking");
        await using var branchAContext = target.CreateContext(branchAApplication);
        await using var branchBContext = target.CreateContext(branchBApplication);
        using var cacheA = CreateCache();
        using var cacheB = CreateCache();
        var notifierA = new CapturingNotifier();
        var notifierB = new CapturingNotifier();
        var bookingA = CreateReservationService(branchAContext, cacheA, notifierA)
            .CreateReservationAsync(CreateRequest(branchA.Id, date, 2, "0000000031"));
        var bookingB = CreateReservationService(branchBContext, cacheB, notifierB)
            .CreateReservationAsync(CreateRequest(branchB.Id, date, 2, "0000000032"));

        await WaitForAdvisoryWaitAsync(target, branchAApplication);
        await WaitForAdvisoryWaitAsync(target, branchBApplication);
        await adminTransaction.CommitAsync();

        var resultA = await bookingA.WaitAsync(TimeSpan.FromSeconds(5));
        var resultB = await bookingB.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(resultA.Succeeded);
        Assert.Contains(resultA.Errors, error => error.Message == ReservationPolicyMessages.BranchClosedDate);
        Assert.True(resultB.Succeeded);
        Assert.Equal(0, notifierA.CallCount);
        Assert.Equal(1, await CountReservationsAsync(target, branchB.Id, date));
    }

    private static async Task VerifyMinimumGuestsRaceAsync(PostgresIntegrationTarget target)
    {
        var date = VietnamToday.AddDays(40);
        var branch = await CreateBranchAsync(target, "minimum-race");

        await using var adminContext = target.CreateContext(UniqueApplicationName("minimum-admin"));
        await using var adminTransaction = await adminContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await new ReservationPolicyLockService(adminContext).AcquireDateLockAsync(date);
        adminContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
        {
            BranchId = branch.Id,
            Date = date,
            RuleType = BranchReservationRuleType.MinimumGuests,
            MinGuests = 6
        });
        await adminContext.SaveChangesAsync();

        var bookingApplication = UniqueApplicationName("minimum-booking");
        await using var bookingContext = target.CreateContext(bookingApplication);
        using var cache = CreateCache();
        var notifier = new CapturingNotifier();
        var bookingTask = CreateReservationService(bookingContext, cache, notifier)
            .CreateReservationAsync(CreateRequest(branch.Id, date, 5, "0000000040"));

        await WaitForAdvisoryWaitAsync(target, bookingApplication);
        await adminTransaction.CommitAsync();
        var bookingResult = await bookingTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(bookingResult.Succeeded);
        Assert.Contains(bookingResult.Errors, error =>
            error.FieldName == nameof(ReservationCreateRequest.GuestCount) &&
            error.Message.Contains("6", StringComparison.Ordinal));
        Assert.Equal(0, notifier.CallCount);
        Assert.Equal(0, await CountReservationsAsync(target, branch.Id, date));

        await using var evaluationContext = target.CreateContext(UniqueApplicationName("minimum-evaluation"));
        var policyService = CreateBranchPolicyService(evaluationContext);
        Assert.Equal(
            BranchReservationPolicyEvaluationStatus.MinimumGuestsNotMet,
            (await policyService.EvaluateAsync(branch.Id, date, 5)).Status);
        Assert.Equal(
            BranchReservationPolicyEvaluationStatus.Allowed,
            (await policyService.EvaluateAsync(branch.Id, date, 6)).Status);
        Assert.Equal(
            BranchReservationPolicyEvaluationStatus.Allowed,
            (await policyService.EvaluateAsync(branch.Id, date, 7)).Status);
    }

    private static async Task VerifyBookingFirstOrderingAsync(PostgresIntegrationTarget target)
    {
        var date = VietnamToday.AddDays(50);
        var branch = await CreateBranchAsync(target, "booking-first");
        var reservation = CreateReservation(branch.Id, date, 6, "0000000050");

        await using var bookingContext = target.CreateContext(UniqueApplicationName("booking-first-holder"));
        await using var bookingTransaction = await bookingContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await new ReservationPolicyLockService(bookingContext).AcquireDateLockAsync(date);
        var evaluator = CreateEvaluator(bookingContext);
        Assert.Equal(
            ReservationPolicyEvaluationStatus.Allowed,
            (await evaluator.EvaluateAsync(branch.Id, date, reservation.GuestCount, VietnamToday)).Status);
        bookingContext.Reservations.Add(reservation);
        await bookingContext.SaveChangesAsync();

        var adminApplication = UniqueApplicationName("booking-first-admin");
        await using var adminContext = target.CreateContext(adminApplication);
        var adminTask = CreateBranchPolicyService(adminContext).SetPolicyAsync(new BranchReservationPolicyMutation(
            branch.Id,
            date,
            BranchReservationRuleType.Closed,
            null,
            ExpectedVersion: null));

        await WaitForAdvisoryWaitAsync(target, adminApplication);
        await bookingTransaction.CommitAsync();
        var adminResult = await adminTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(adminResult.Succeeded);

        await using var verificationContext = target.CreateContext(UniqueApplicationName("booking-first-verify"));
        var persisted = await verificationContext.Reservations.AsNoTracking().SingleAsync(row => row.Id == reservation.Id);
        Assert.Equal(ReservationStatus.Pending, persisted.Status);
        Assert.Equal(6, persisted.GuestCount);
        Assert.Equal(BranchReservationRuleType.Closed,
            (await verificationContext.BranchReservationDatePolicies.AsNoTracking()
                .SingleAsync(policy => policy.BranchId == branch.Id && policy.Date == date)).RuleType);
    }

    private static async Task VerifyUuidConcurrencyAndAbaAsync(PostgresIntegrationTarget target)
    {
        var branchA = await CreateBranchAsync(target, "uuid-a");
        var branchB = await CreateBranchAsync(target, "uuid-b");
        var updateDate = VietnamToday.AddDays(60);
        await using var firstContext = target.CreateContext(UniqueApplicationName("uuid-first"));
        await using var secondContext = target.CreateContext(UniqueApplicationName("uuid-second"));
        var firstService = CreateBranchPolicyService(firstContext);
        var secondService = CreateBranchPolicyService(secondContext);

        var created = await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, updateDate, BranchReservationRuleType.Closed, null, ExpectedVersion: null));
        var versionX = created.Policy!.Version;
        var updated = await secondService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, updateDate, BranchReservationRuleType.MinimumGuests, 6, versionX));
        var versionY = updated.Policy!.Version;
        var staleUpdate = await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, updateDate, BranchReservationRuleType.Closed, null, versionX));
        Assert.NotEqual(Guid.Empty, versionX);
        Assert.NotEqual(versionX, versionY);
        Assert.Equal(BranchReservationPolicyMutationStatus.Conflict, staleUpdate.Status);

        var crossBranch = await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchB.Id, updateDate, BranchReservationRuleType.Closed, null, ExpectedVersion: null));
        Assert.True(crossBranch.Succeeded);
        Assert.Equal(versionY, (await secondService.GetPolicyAsync(branchA.Id, updateDate))!.Version);

        var abaDate = VietnamToday.AddDays(61);
        var original = await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, abaDate, BranchReservationRuleType.Closed, null, ExpectedVersion: null));
        var staleAbaVersion = original.Policy!.Version;
        Assert.True((await secondService.RemovePolicyAsync(branchA.Id, abaDate, staleAbaVersion)).Succeeded);
        var recreated = await secondService.SetPolicyAsync(new BranchReservationPolicyMutation(
            branchA.Id, abaDate, BranchReservationRuleType.MinimumGuests, 7, ExpectedVersion: null));
        Assert.NotEqual(staleAbaVersion, recreated.Policy!.Version);
        Assert.Equal(
            BranchReservationPolicyMutationStatus.Conflict,
            (await firstService.SetPolicyAsync(new BranchReservationPolicyMutation(
                branchA.Id, abaDate, BranchReservationRuleType.Closed, null, staleAbaVersion))).Status);
        Assert.Equal(
            BranchReservationPolicyMutationStatus.Conflict,
            (await firstService.RemovePolicyAsync(branchA.Id, abaDate, staleAbaVersion)).Status);
        var recreatedPersisted = await secondService.GetPolicyAsync(branchA.Id, abaDate);
        Assert.Equal(recreated.Policy.Version, recreatedPersisted!.Version);
        Assert.Equal(7, recreatedPersisted.MinGuests);

        await VerifyActualConcurrencyPredicateAsync(target, branchA.Id, VietnamToday.AddDays(62), delete: false);
        await VerifyActualConcurrencyPredicateAsync(target, branchA.Id, VietnamToday.AddDays(63), delete: true);

        Assert.True((await secondService.RemovePolicyAsync(branchA.Id, updateDate, versionY)).Succeeded);
        Assert.Equal(
            BranchReservationPolicyMutationStatus.Conflict,
            (await firstService.RemovePolicyAsync(branchA.Id, updateDate, versionY)).Status);
    }

    private static async Task VerifyActualConcurrencyPredicateAsync(
        PostgresIntegrationTarget target,
        Guid branchId,
        DateOnly date,
        bool delete)
    {
        await using (var setupContext = target.CreateContext(UniqueApplicationName("predicate-setup")))
        {
            setupContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
            {
                BranchId = branchId,
                Date = date,
                RuleType = BranchReservationRuleType.Closed
            });
            await setupContext.SaveChangesAsync();
        }

        var interceptor = new MutationCommandCaptureInterceptor();
        await using var staleContext = target.CreateContext(UniqueApplicationName("predicate-stale"), interceptor);
        await using var winningContext = target.CreateContext(UniqueApplicationName("predicate-winning"));
        var stale = await staleContext.BranchReservationDatePolicies
            .SingleAsync(policy => policy.BranchId == branchId && policy.Date == date);
        var originalVersion = stale.Version;
        var winning = await winningContext.BranchReservationDatePolicies
            .SingleAsync(policy => policy.BranchId == branchId && policy.Date == date);
        winning.Version = Guid.NewGuid();
        await winningContext.SaveChangesAsync();

        if (delete)
        {
            staleContext.BranchReservationDatePolicies.Remove(stale);
        }
        else
        {
            stale.RuleType = BranchReservationRuleType.MinimumGuests;
            stale.MinGuests = 6;
            stale.Version = Guid.NewGuid();
        }

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleContext.SaveChangesAsync());
        var command = Assert.Single(interceptor.MutationCommands);
        Assert.Contains(delete ? "DELETE" : "UPDATE", command.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"BranchId\"", command.Sql, StringComparison.Ordinal);
        Assert.Contains("\"Date\"", command.Sql, StringComparison.Ordinal);
        Assert.Contains("\"Version\"", command.Sql, StringComparison.Ordinal);
        Assert.Contains(originalVersion, command.ParameterValues.OfType<Guid>());
    }

    private static async Task VerifyDatabaseConstraintsAsync(PostgresIntegrationTarget target)
    {
        var branch = await CreateBranchAsync(target, "constraints");
        var date = VietnamToday.AddDays(70);

        await AssertPolicyInsertAcceptedAsync(target, branch.Id, date, "Closed", null);
        await AssertPolicyInsertRejectedAsync(target, branch.Id, date.AddDays(1), "Closed", 6, PostgresErrorCodes.CheckViolation);
        await AssertPolicyInsertAcceptedAsync(target, branch.Id, date.AddDays(2), "MinimumGuests", 6);
        await AssertPolicyInsertRejectedAsync(target, branch.Id, date.AddDays(3), "MinimumGuests", null, PostgresErrorCodes.CheckViolation);
        await AssertPolicyInsertRejectedAsync(target, branch.Id, date.AddDays(4), "MinimumGuests", 0, PostgresErrorCodes.CheckViolation);
        await AssertPolicyInsertRejectedAsync(target, branch.Id, date.AddDays(5), "MinimumGuests", 101, PostgresErrorCodes.CheckViolation);
        await AssertPolicyInsertRejectedAsync(target, branch.Id, date.AddDays(6), "Unknown", null, PostgresErrorCodes.CheckViolation);
        await AssertPolicyInsertRejectedAsync(target, Guid.NewGuid(), date.AddDays(7), "Closed", null, PostgresErrorCodes.ForeignKeyViolation);

        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await InsertPolicyAsync(connection, transaction, branch.Id, date.AddDays(8), "Closed", null);
        var duplicate = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertPolicyAsync(connection, transaction, branch.Id, date.AddDays(8), "Closed", null));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
        await transaction.RollbackAsync();

        var restrictedDate = date.AddDays(9);
        await using (var setupContext = target.CreateContext(UniqueApplicationName("fk-restrict-setup")))
        {
            setupContext.BranchReservationDatePolicies.Add(new BranchReservationDatePolicy
            {
                BranchId = branch.Id,
                Date = restrictedDate,
                RuleType = BranchReservationRuleType.Closed
            });
            await setupContext.SaveChangesAsync();
        }

        await using var deleteConnection = new NpgsqlConnection(target.ConnectionString);
        await deleteConnection.OpenAsync();
        await using var deleteCommand = deleteConnection.CreateCommand();
        deleteCommand.CommandText = "DELETE FROM \"Branches\" WHERE \"Id\" = @branchId";
        deleteCommand.Parameters.AddWithValue("branchId", branch.Id);
        var restricted = await Assert.ThrowsAsync<PostgresException>(() => deleteCommand.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, restricted.SqlState);
    }

    private static async Task VerifyCleanupBoundariesAsync(PostgresIntegrationTarget target)
    {
        var branch = await CreateBranchAsync(target, "cleanup");
        await using var context = target.CreateContext(UniqueApplicationName("cleanup"));
        await context.BlockedReservationDates.ExecuteDeleteAsync();
        await context.BranchReservationDatePolicies.ExecuteDeleteAsync();
        context.BlockedReservationDates.AddRange(
            new BlockedReservationDate { Date = VietnamToday.AddDays(-1) },
            new BlockedReservationDate { Date = VietnamToday },
            new BlockedReservationDate { Date = VietnamToday.AddDays(1) });
        context.BranchReservationDatePolicies.AddRange(
            CreatePolicy(branch.Id, VietnamToday.AddDays(-1)),
            CreatePolicy(branch.Id, VietnamToday),
            CreatePolicy(branch.Id, VietnamToday.AddDays(1)));
        await context.SaveChangesAsync();

        var lockService = new ReservationPolicyLockService(context);
        var globalService = new ReservationBlockedDateService(context, lockService);
        var branchService = CreateBranchPolicyService(context);
        Assert.Equal(1, await globalService.CleanupPastDatesAsync(VietnamToday));
        Assert.Equal(1, await branchService.CleanupPastPoliciesAsync(VietnamToday));
        Assert.Equal(0, await globalService.CleanupPastDatesAsync(VietnamToday));
        Assert.Equal(0, await branchService.CleanupPastPoliciesAsync(VietnamToday));
        Assert.Equal(
            [VietnamToday, VietnamToday.AddDays(1)],
            await context.BlockedReservationDates.AsNoTracking().OrderBy(row => row.Date).Select(row => row.Date).ToListAsync());
        Assert.Equal(
            [VietnamToday, VietnamToday.AddDays(1)],
            await context.BranchReservationDatePolicies.AsNoTracking().OrderBy(row => row.Date).Select(row => row.Date).ToListAsync());
    }

    private static async Task VerifyFailureAndRollbackSafetyAsync(PostgresIntegrationTarget target)
    {
        var branch = await CreateBranchAsync(target, "failure-safety");
        var rollbackDate = VietnamToday.AddDays(80);
        var rollbackReservation = CreateReservation(branch.Id, rollbackDate, 2, "0000000080");
        await using (var rollbackContext = target.CreateContext(UniqueApplicationName("rollback")))
        await using (var transaction = await rollbackContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            rollbackContext.Reservations.Add(rollbackReservation);
            await rollbackContext.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        Assert.Equal(0, await CountReservationsAsync(target, branch.Id, rollbackDate));

        var lockDate = VietnamToday.AddDays(81);
        await using var holderContext = target.CreateContext(UniqueApplicationName("failure-holder"));
        await using var holderTransaction = await holderContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await new ReservationPolicyLockService(holderContext).AcquireDateLockAsync(lockDate);

        var cancellationApplication = UniqueApplicationName("cancelled-booking");
        await using (var cancelledContext = target.CreateContext(cancellationApplication))
        using (var cache = CreateCache())
        using (var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500)))
        {
            var cancelledService = CreateReservationService(cancelledContext, cache, new CapturingNotifier());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledService.CreateReservationAsync(
                CreateRequest(branch.Id, lockDate, 2, "0000000081"), cancellation.Token));
        }
        Assert.Equal(0, await CountReservationsAsync(target, branch.Id, lockDate));

        await using (var timeoutContext = target.CreateContext(UniqueApplicationName("lock-timeout")))
        await using (var timeoutTransaction = await timeoutContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            await timeoutContext.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '300ms'");
            var timeoutException = await Assert.ThrowsAsync<PostgresException>(() =>
                new ReservationPolicyLockService(timeoutContext).AcquireDateLockAsync(lockDate));
            Assert.Equal(PostgresErrorCodes.LockNotAvailable, timeoutException.SqlState);
            await timeoutTransaction.RollbackAsync();
        }
        await holderTransaction.RollbackAsync();

        var invalidReservation = CreateReservation(branch.Id, VietnamToday.AddDays(82), 0, "0000000082");
        await using (var invalidContext = target.CreateContext(UniqueApplicationName("save-failure")))
        await using (var invalidTransaction = await invalidContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            invalidContext.Reservations.Add(invalidReservation);
            var saveException = await Assert.ThrowsAsync<DbUpdateException>(() => invalidContext.SaveChangesAsync());
            Assert.Equal(PostgresErrorCodes.CheckViolation, (saveException.InnerException as PostgresException)?.SqlState);
            await invalidTransaction.RollbackAsync();
        }
        Assert.Equal(0, await CountReservationsAsync(target, branch.Id, invalidReservation.ReservationDate));

        var lookupFailureDate = VietnamToday.AddDays(83);
        await RenamePolicyTableAsync(target, "BranchReservationDatePolicies", "BranchReservationDatePolicies_it_hidden");
        try
        {
            await using var failedLookupContext = target.CreateContext(UniqueApplicationName("lookup-failure"));
            using var cache = CreateCache();
            var service = CreateReservationService(failedLookupContext, cache, new CapturingNotifier());
            await Assert.ThrowsAsync<PostgresException>(() => service.CreateReservationAsync(
                CreateRequest(branch.Id, lookupFailureDate, 2, "0000000083")));
        }
        finally
        {
            await RenamePolicyTableAsync(target, "BranchReservationDatePolicies_it_hidden", "BranchReservationDatePolicies");
        }
        Assert.Equal(0, await CountReservationsAsync(target, branch.Id, lookupFailureDate));
    }

    private static ReservationService CreateReservationService(
        AppDbContext context,
        IMemoryCache cache,
        IAdminReservationNotifier notifier)
    {
        var lockService = new ReservationPolicyLockService(context);
        var globalService = new ReservationBlockedDateService(context, lockService);
        var branchService = CreateBranchPolicyService(context);
        return new ReservationService(
            context,
            cache,
            notifier,
            globalService,
            branchService,
            new ReservationPolicyEvaluator(context, globalService, branchService),
            lockService,
            NullLogger<ReservationService>.Instance,
            TimeProvider);
    }

    private static BranchReservationDatePolicyService CreateBranchPolicyService(AppDbContext context)
    {
        return new BranchReservationDatePolicyService(
            context,
            new ReservationPolicyLockService(context),
            NullLogger<BranchReservationDatePolicyService>.Instance,
            TimeProvider);
    }

    private static ReservationPolicyEvaluator CreateEvaluator(AppDbContext context)
    {
        var lockService = new ReservationPolicyLockService(context);
        var globalService = new ReservationBlockedDateService(context, lockService);
        return new ReservationPolicyEvaluator(context, globalService, CreateBranchPolicyService(context));
    }

    private static async Task<Branch> CreateBranchAsync(PostgresIntegrationTarget target, string label)
    {
        var branch = new Branch
        {
            Name = $"Integration {label}",
            Slug = $"it-{label}-{Guid.NewGuid():N}",
            Address = "Synthetic integration fixture",
            District = "Synthetic",
            City = "Synthetic",
            Hotline = "0000000000",
            Email = $"{Guid.NewGuid():N}@example.invalid",
            OpeningTime = new TimeOnly(0, 0),
            ClosingTime = new TimeOnly(23, 59),
            Capacity = 0,
            Description = "Disposable PostgreSQL integration fixture",
            ThumbnailUrl = string.Empty,
            GoogleMapUrl = string.Empty,
            IsActive = true,
            AllowsReservations = true
        };

        await using var context = target.CreateContext(UniqueApplicationName("branch-fixture"));
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private static ReservationCreateRequest CreateRequest(
        Guid branchId,
        DateOnly date,
        int guestCount,
        string phoneNumber)
    {
        return new ReservationCreateRequest
        {
            CustomerName = "Synthetic Integration Customer",
            PhoneNumber = phoneNumber,
            BranchId = branchId,
            GuestCount = guestCount,
            ReservationDate = date,
            ReservationTime = new TimeOnly(18, 0),
            Note = "Disposable PostgreSQL integration fixture"
        };
    }

    private static Reservation CreateReservation(
        Guid branchId,
        DateOnly date,
        int guestCount,
        string phoneNumber)
    {
        return new Reservation
        {
            BranchId = branchId,
            CustomerName = "Synthetic Integration Customer",
            PhoneNumber = phoneNumber,
            GuestCount = guestCount,
            ReservationDate = date,
            ReservationTime = new TimeOnly(18, 0),
            Status = ReservationStatus.Pending,
            Source = ReservationSource.Website
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

    private static MemoryCache CreateCache()
    {
        return new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
    }

    private static async Task WaitForAdvisoryWaitAsync(
        PostgresIntegrationTarget target,
        string applicationName)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_stat_activity
                WHERE application_name = @applicationName
                  AND wait_event_type = 'Lock'
                  AND wait_event = 'advisory')
            """;
        command.Parameters.AddWithValue("applicationName", applicationName);

        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (Assert.IsType<bool>(await command.ExecuteScalarAsync()))
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException($"Backend {applicationName} did not enter an advisory-lock wait.");
    }

    private static async Task AssertReadCommittedAsync(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "SHOW transaction_isolation";
        Assert.Equal("read committed", Assert.IsType<string>(await command.ExecuteScalarAsync()));
    }

    private static async Task<int> GetBackendPidAsync(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "SELECT pg_backend_pid()";
        return Assert.IsType<int>(await command.ExecuteScalarAsync());
    }

    private static async Task<int> CountReservationsAsync(
        PostgresIntegrationTarget target,
        Guid branchId,
        DateOnly date)
    {
        await using var context = target.CreateContext(UniqueApplicationName("reservation-count"));
        return await context.Reservations.AsNoTracking()
            .CountAsync(reservation => reservation.BranchId == branchId && reservation.ReservationDate == date);
    }

    private static async Task AssertPolicyInsertAcceptedAsync(
        PostgresIntegrationTarget target,
        Guid branchId,
        DateOnly date,
        string ruleType,
        int? minGuests)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        Assert.Equal(1, await InsertPolicyAsync(connection, transaction, branchId, date, ruleType, minGuests));
        await transaction.RollbackAsync();
    }

    private static async Task AssertPolicyInsertRejectedAsync(
        PostgresIntegrationTarget target,
        Guid branchId,
        DateOnly date,
        string ruleType,
        int? minGuests,
        string expectedSqlState)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertPolicyAsync(connection, transaction, branchId, date, ruleType, minGuests));
        Assert.Equal(expectedSqlState, exception.SqlState);
        await transaction.RollbackAsync();
    }

    private static async Task<int> InsertPolicyAsync(
        NpgsqlConnection connection,
        DbTransaction transaction,
        Guid branchId,
        DateOnly date,
        string ruleType,
        int? minGuests)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction)transaction;
        command.CommandText =
            """
            INSERT INTO "BranchReservationDatePolicies"
                ("BranchId", "Date", "RuleType", "MinGuests", "Version", "CreatedAt", "UpdatedAt")
            VALUES
                (@branchId, @date, @ruleType, @minGuests, @version, now(), now())
            """;
        command.Parameters.AddWithValue("branchId", branchId);
        command.Parameters.AddWithValue("date", date);
        command.Parameters.AddWithValue("ruleType", ruleType);
        command.Parameters.AddWithValue("minGuests", minGuests.HasValue ? minGuests.Value : DBNull.Value);
        command.Parameters.AddWithValue("version", Guid.NewGuid());
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task RenamePolicyTableAsync(
        PostgresIntegrationTarget target,
        string currentName,
        string newName)
    {
        if (!Regex.IsMatch(currentName, "^[A-Za-z0-9_]+$") || !Regex.IsMatch(newName, "^[A-Za-z0-9_]+$"))
        {
            throw new InvalidOperationException("Unsafe integration table identifier.");
        }

        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER TABLE \"{currentName}\" RENAME TO \"{newName}\"";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> ExecuteScalarInt64Async(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Assert.IsType<long>(await command.ExecuteScalarAsync());
    }

    private static async Task<Dictionary<string, string>> ReadConstraintDefinitionsAsync(NpgsqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT conname, pg_get_constraintdef(oid)
            FROM pg_constraint
            WHERE conrelid = '"BranchReservationDatePolicies"'::regclass
            """;
        await using var reader = await command.ExecuteReaderAsync();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetString(0), reader.GetString(1));
        }

        return result;
    }

    private static string UniqueApplicationName(string prefix)
    {
        return $"kig-it-{prefix}-{Guid.NewGuid():N}";
    }

    private sealed class CapturingNotifier : IAdminReservationNotifier
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

    private sealed class PolicyInsertCollisionInterceptor(
        string connectionString,
        Guid branchId,
        DateOnly date) : DbCommandInterceptor
    {
        private int _triggered;

        public bool Triggered => _triggered != 0;

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (!command.CommandText.Contains(
                    "INSERT INTO \"BranchReservationDatePolicies\"",
                    StringComparison.Ordinal) ||
                Interlocked.Exchange(ref _triggered, 1) != 0)
            {
                return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            }

            await using var competingConnection = new NpgsqlConnection(connectionString);
            await competingConnection.OpenAsync(cancellationToken);
            await using var competingCommand = competingConnection.CreateCommand();
            competingCommand.CommandText =
                """
                INSERT INTO "BranchReservationDatePolicies"
                    ("BranchId", "Date", "RuleType", "MinGuests", "Version", "CreatedAt", "UpdatedAt")
                VALUES
                    (@branchId, @date, 'Closed', NULL, @version, now(), now())
                """;
            competingCommand.Parameters.AddWithValue("branchId", branchId);
            competingCommand.Parameters.AddWithValue("date", date);
            competingCommand.Parameters.AddWithValue("version", Guid.NewGuid());
            Assert.Equal(1, await competingCommand.ExecuteNonQueryAsync(cancellationToken));

            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class PolicyCheckViolationInterceptor : DbCommandInterceptor
    {
        private int _triggered;

        public bool Triggered => _triggered != 0;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains(
                    "INSERT INTO \"BranchReservationDatePolicies\"",
                    StringComparison.Ordinal) &&
                Interlocked.Exchange(ref _triggered, 1) == 0)
            {
                var ruleTypeParameter = command.Parameters
                    .Cast<DbParameter>()
                    .Single(parameter => string.Equals(parameter.Value as string, "Closed", StringComparison.Ordinal));
                ruleTypeParameter.Value = "UnsupportedIntegrationRule";
            }

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class MutationCommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<CapturedMutationCommand> MutationCommands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Capture(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void Capture(DbCommand command)
        {
            if (!command.CommandText.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) &&
                !command.CommandText.TrimStart().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            MutationCommands.Add(new CapturedMutationCommand(
                command.CommandText,
                command.Parameters.Cast<DbParameter>().Select(parameter => parameter.Value).ToArray()));
        }
    }

    private sealed record CapturedMutationCommand(string Sql, IReadOnlyList<object?> ParameterValues);

    private sealed record PostgresIntegrationTarget(
        string ConnectionString,
        string Database,
        string Username,
        string ClusterName)
    {
        public static PostgresIntegrationTarget FromEnvironment()
        {
            var connectionString = RequiredEnvironmentVariable("KIG_POSTGRES_IT_CONNECTION");
            var runId = RequiredEnvironmentVariable("KIG_POSTGRES_IT_RUN_ID");
            var clusterName = RequiredEnvironmentVariable("KIG_POSTGRES_IT_CLUSTER");
            if (!Regex.IsMatch(runId, "^[a-f0-9]{12}$"))
            {
                throw new InvalidOperationException("The PostgreSQL integration run identifier is invalid.");
            }

            var expectedDatabase = $"kig_branch_policy_it_{runId}";
            var expectedUsername = $"kig_branch_policy_it_{runId}";
            var expectedCluster = $"kig-branch-policy-it-{runId}";
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            if (!string.Equals(builder.Host, "127.0.0.1", StringComparison.Ordinal) ||
                !string.Equals(builder.Database, expectedDatabase, StringComparison.Ordinal) ||
                !string.Equals(builder.Username, expectedUsername, StringComparison.Ordinal) ||
                !string.Equals(clusterName, expectedCluster, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The PostgreSQL integration target failed the disposable loopback identity guard.");
            }

            return new PostgresIntegrationTarget(connectionString, expectedDatabase, expectedUsername, expectedCluster);
        }

        public AppDbContext CreateContext(string applicationName, params IInterceptor[] interceptors)
        {
            var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
            {
                ApplicationName = applicationName
            };
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(builder.ConnectionString)
                .AddInterceptors(interceptors)
                .Options;
            return new AppDbContext(options);
        }

        private static string RequiredEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name)
                ?? throw new InvalidOperationException($"Required integration environment variable {name} is missing.");
        }
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgresIntegrationFactAttribute : FactAttribute
{
    public PostgresIntegrationFactAttribute()
    {
        if (!string.Equals(
            Environment.GetEnvironmentVariable("RUN_KIG_POSTGRES_IT"),
            "true",
            StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set RUN_KIG_POSTGRES_IT=true with a verified disposable KIG PostgreSQL target to run this test.";
        }
    }
}

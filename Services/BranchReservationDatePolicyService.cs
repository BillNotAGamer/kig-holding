using System.Data;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KIGHolding.Services;

public sealed class BranchReservationDatePolicyService : IBranchReservationDatePolicyService
{
    private readonly AppDbContext _dbContext;
    private readonly IReservationPolicyLockService _policyLockService;
    private readonly ILogger<BranchReservationDatePolicyService> _logger;
    private readonly TimeProvider _timeProvider;

    public BranchReservationDatePolicyService(
        AppDbContext dbContext,
        IReservationPolicyLockService policyLockService,
        ILogger<BranchReservationDatePolicyService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _policyLockService = policyLockService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<BranchReservationDatePolicy?> GetPolicyAsync(
        Guid branchId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.BranchReservationDatePolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                policy => policy.BranchId == branchId && policy.Date == date,
                cancellationToken);
    }

    public async Task<IReadOnlyList<BranchReservationDatePolicy>> GetPoliciesAsync(
        Guid branchId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            throw new ArgumentException("BranchId is required.", nameof(branchId));
        }

        if (startDate > endDate)
        {
            throw new ArgumentException("The start date must not be after the end date.", nameof(startDate));
        }

        return await _dbContext.BranchReservationDatePolicies
            .AsNoTracking()
            .Where(policy =>
                policy.BranchId == branchId &&
                policy.Date >= startDate &&
                policy.Date <= endDate)
            .OrderBy(policy => policy.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<BranchReservationPolicyEvaluationResult> EvaluateAsync(
        Guid branchId,
        DateOnly date,
        int guestCount,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            return new BranchReservationPolicyEvaluationResult(
                BranchReservationPolicyEvaluationStatus.InvalidBranch);
        }

        if (guestCount is < 1 or > 100)
        {
            return new BranchReservationPolicyEvaluationResult(
                BranchReservationPolicyEvaluationStatus.InvalidGuestCount);
        }

        var policy = await GetPolicyAsync(branchId, date, cancellationToken);
        if (policy is null)
        {
            return new BranchReservationPolicyEvaluationResult(
                BranchReservationPolicyEvaluationStatus.Allowed);
        }

        return policy.RuleType switch
        {
            BranchReservationRuleType.Closed => new BranchReservationPolicyEvaluationResult(
                BranchReservationPolicyEvaluationStatus.BranchClosedDate),
            BranchReservationRuleType.MinimumGuests when policy.MinGuests.HasValue && guestCount < policy.MinGuests.Value =>
                new BranchReservationPolicyEvaluationResult(
                    BranchReservationPolicyEvaluationStatus.MinimumGuestsNotMet,
                    policy.MinGuests),
            BranchReservationRuleType.MinimumGuests when policy.MinGuests.HasValue =>
                new BranchReservationPolicyEvaluationResult(
                    BranchReservationPolicyEvaluationStatus.Allowed),
            _ => throw new InvalidOperationException(
                $"Branch reservation policy {policy.BranchId}/{policy.Date:yyyy-MM-dd} contains an unsupported rule state.")
        };
    }

    public async Task<BranchReservationPolicyMutationResult> SetPolicyAsync(
        BranchReservationPolicyMutation mutation,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateMutation(mutation);
        if (validationMessage is not null)
        {
            return Failed(BranchReservationPolicyMutationStatus.ValidationFailed, validationMessage);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        try
        {
            await _policyLockService.AcquireDateLockAsync(mutation.Date, cancellationToken);

            var branch = await _dbContext.Branches
                .AsNoTracking()
                .Where(candidate => candidate.Id == mutation.BranchId)
                .Select(candidate => new { candidate.IsActive, candidate.AllowsReservations })
                .FirstOrDefaultAsync(cancellationToken);

            if (branch is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failed(BranchReservationPolicyMutationStatus.BranchNotFound, "Không tìm thấy chi nhánh.");
            }

            if (!branch.IsActive || !branch.AllowsReservations)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failed(
                    BranchReservationPolicyMutationStatus.BranchNotReservable,
                    ReservationPolicyMessages.InvalidBranch);
            }

            var current = await _dbContext.BranchReservationDatePolicies
                .FirstOrDefaultAsync(
                    policy => policy.BranchId == mutation.BranchId && policy.Date == mutation.Date,
                    cancellationToken);

            if (mutation.ExpectedVersion is null)
            {
                if (current is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Conflict();
                }

                var now = DateTimeOffset.UtcNow;
                current = new BranchReservationDatePolicy
                {
                    BranchId = mutation.BranchId,
                    Date = mutation.Date,
                    RuleType = mutation.RuleType,
                    MinGuests = mutation.MinGuests,
                    Version = CreateVersion(),
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _dbContext.BranchReservationDatePolicies.Add(current);
            }
            else
            {
                if (current is null || current.Version != mutation.ExpectedVersion.Value)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Conflict();
                }

                current.RuleType = mutation.RuleType;
                current.MinGuests = mutation.MinGuests;
                current.Version = CreateVersion(current.Version);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new BranchReservationPolicyMutationResult(
                BranchReservationPolicyMutationStatus.Succeeded,
                ToSnapshot(current));
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await RollbackSafelyAsync(transaction, cancellationToken);
            DetachFailedMutationPolicy(mutation.BranchId, mutation.Date);
            _logger.LogInformation(
                exception,
                "A concurrent branch reservation policy edit was detected for branch {BranchId} and date {PolicyDate}.",
                mutation.BranchId,
                mutation.Date);
            return Conflict();
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await RollbackSafelyAsync(transaction, cancellationToken);
            DetachFailedMutationPolicy(mutation.BranchId, mutation.Date);
            _logger.LogInformation(
                exception,
                "A concurrent branch reservation policy insert was detected for branch {BranchId} and date {PolicyDate}.",
                mutation.BranchId,
                mutation.Date);
            return Conflict();
        }
        catch (DbUpdateException exception)
        {
            await RollbackSafelyAsync(transaction, cancellationToken);
            DetachFailedMutationPolicy(mutation.BranchId, mutation.Date);
            _logger.LogWarning(
                exception,
                "Unable to persist branch reservation policy for branch {BranchId} and date {PolicyDate}.",
                mutation.BranchId,
                mutation.Date);
            return Failed(
                BranchReservationPolicyMutationStatus.PersistenceFailure,
                "Không thể lưu chính sách đặt bàn lúc này. Vui lòng tải lại và thử lại.");
        }
    }

    public async Task<BranchReservationPolicyMutationResult> RemovePolicyAsync(
        Guid branchId,
        DateOnly date,
        Guid expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty || expectedVersion == Guid.Empty)
        {
            return Failed(
                BranchReservationPolicyMutationStatus.ValidationFailed,
                "Thông tin chính sách cần xóa không hợp lệ.");
        }

        if (date < VietnamClock.GetVietnamToday(_timeProvider))
        {
            return Failed(
                BranchReservationPolicyMutationStatus.ValidationFailed,
                "Không thể thay đổi chính sách của ngày đã qua.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        try
        {
            await _policyLockService.AcquireDateLockAsync(date, cancellationToken);

            if (!await _dbContext.Branches.AsNoTracking().AnyAsync(branch => branch.Id == branchId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failed(BranchReservationPolicyMutationStatus.BranchNotFound, "Không tìm thấy chi nhánh.");
            }

            var current = await _dbContext.BranchReservationDatePolicies
                .FirstOrDefaultAsync(
                    policy => policy.BranchId == branchId && policy.Date == date,
                    cancellationToken);

            if (current is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict();
            }

            if (current.Version != expectedVersion)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict();
            }

            _dbContext.BranchReservationDatePolicies.Remove(current);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new BranchReservationPolicyMutationResult(BranchReservationPolicyMutationStatus.Succeeded);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await RollbackSafelyAsync(transaction, cancellationToken);
            DetachFailedMutationPolicy(branchId, date);
            _logger.LogInformation(
                exception,
                "A concurrent branch reservation policy delete was detected for branch {BranchId} and date {PolicyDate}.",
                branchId,
                date);
            return Conflict();
        }
        catch (DbUpdateException exception)
        {
            await RollbackSafelyAsync(transaction, cancellationToken);
            DetachFailedMutationPolicy(branchId, date);
            _logger.LogWarning(
                exception,
                "Unable to remove branch reservation policy for branch {BranchId} and date {PolicyDate}.",
                branchId,
                date);
            return Failed(
                BranchReservationPolicyMutationStatus.PersistenceFailure,
                "Không thể xóa chính sách đặt bàn lúc này. Vui lòng tải lại và thử lại.");
        }
    }

    public async Task<IReadOnlyList<ReservationStatusCount>> GetExistingReservationCountsAsync(
        Guid branchId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var statuses = await _dbContext.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.BranchId == branchId && reservation.ReservationDate == date)
            .Select(reservation => reservation.Status)
            .ToListAsync(cancellationToken);

        return statuses
            .GroupBy(status => status)
            .OrderBy(group => group.Key)
            .Select(group => new ReservationStatusCount(group.Key, group.Count()))
            .ToArray();
    }

    public async Task<int> CleanupPastPoliciesAsync(
        DateOnly vietnamToday,
        CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.IsRelational())
        {
            return await _dbContext.BranchReservationDatePolicies
                .Where(policy => policy.Date < vietnamToday)
                .ExecuteDeleteAsync(cancellationToken);
        }

        var expired = await _dbContext.BranchReservationDatePolicies
            .Where(policy => policy.Date < vietnamToday)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return 0;
        }

        _dbContext.BranchReservationDatePolicies.RemoveRange(expired);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return expired.Count;
    }

    private string? ValidateMutation(BranchReservationPolicyMutation mutation)
    {
        if (mutation.BranchId == Guid.Empty)
        {
            return "Vui lòng chọn chi nhánh.";
        }

        if (mutation.Date < VietnamClock.GetVietnamToday(_timeProvider))
        {
            return "Không thể thay đổi chính sách của ngày đã qua.";
        }

        if (!Enum.IsDefined(mutation.RuleType))
        {
            return "Loại chính sách đặt bàn không hợp lệ.";
        }

        if (mutation.ExpectedVersion == Guid.Empty)
        {
            return "Phiên bản chính sách không hợp lệ.";
        }

        return mutation.RuleType switch
        {
            BranchReservationRuleType.Closed when mutation.MinGuests is not null =>
                "Chính sách đóng cửa không được có số khách tối thiểu.",
            BranchReservationRuleType.MinimumGuests when mutation.MinGuests is null or < 1 or > 100 =>
                "Số khách tối thiểu phải từ 1 đến 100.",
            _ => null
        };
    }

    private static BranchReservationPolicySnapshot ToSnapshot(BranchReservationDatePolicy policy)
    {
        return new BranchReservationPolicySnapshot(
            policy.BranchId,
            policy.Date,
            policy.RuleType,
            policy.MinGuests,
            policy.Version,
            policy.CreatedAt,
            policy.UpdatedAt);
    }

    private static BranchReservationPolicyMutationResult Conflict()
    {
        return Failed(
            BranchReservationPolicyMutationStatus.Conflict,
            "Chính sách đã được người khác thay đổi. Vui lòng tải lại dữ liệu mới nhất.");
    }

    private static BranchReservationPolicyMutationResult Failed(
        BranchReservationPolicyMutationStatus status,
        string message)
    {
        return new BranchReservationPolicyMutationResult(status, Message: message);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
    }

    private static Guid CreateVersion(Guid? previousVersion = null)
    {
        Guid version;
        do
        {
            version = Guid.NewGuid();
        }
        while (version == Guid.Empty || version == previousVersion);

        return version;
    }

    private void DetachFailedMutationPolicy(Guid branchId, DateOnly date)
    {
        var failedEntries = _dbContext.ChangeTracker
            .Entries<BranchReservationDatePolicy>()
            .Where(entry => entry.Entity.BranchId == branchId && entry.Entity.Date == date)
            .ToArray();

        foreach (var entry in failedEntries)
        {
            entry.State = EntityState.Detached;
        }
    }

    private static async Task RollbackSafelyAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch
        {
            // Preserve the typed mutation result; the original persistence failure is logged by the caller.
        }
    }
}

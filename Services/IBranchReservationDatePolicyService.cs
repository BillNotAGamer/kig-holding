using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;

namespace KIGHolding.Services;

public interface IBranchReservationDatePolicyService
{
    Task<BranchReservationDatePolicy?> GetPolicyAsync(
        Guid branchId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchReservationDatePolicy>> GetPoliciesAsync(
        Guid branchId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<BranchReservationPolicyEvaluationResult> EvaluateAsync(
        Guid branchId,
        DateOnly date,
        int guestCount,
        CancellationToken cancellationToken = default);

    Task<BranchReservationPolicyMutationResult> SetPolicyAsync(
        BranchReservationPolicyMutation mutation,
        CancellationToken cancellationToken = default);

    Task<BranchReservationPolicyMutationResult> RemovePolicyAsync(
        Guid branchId,
        DateOnly date,
        Guid expectedVersion,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationStatusCount>> GetExistingReservationCountsAsync(
        Guid branchId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<int> CleanupPastPoliciesAsync(
        DateOnly vietnamToday,
        CancellationToken cancellationToken = default);
}

public sealed record BranchReservationPolicyMutation(
    Guid BranchId,
    DateOnly Date,
    BranchReservationRuleType RuleType,
    int? MinGuests,
    Guid? ExpectedVersion);

public sealed record BranchReservationPolicySnapshot(
    Guid BranchId,
    DateOnly Date,
    BranchReservationRuleType RuleType,
    int? MinGuests,
    Guid Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record BranchReservationPolicyMutationResult(
    BranchReservationPolicyMutationStatus Status,
    BranchReservationPolicySnapshot? Policy = null,
    string? Message = null)
{
    public bool Succeeded => Status == BranchReservationPolicyMutationStatus.Succeeded;
}

public enum BranchReservationPolicyMutationStatus
{
    Succeeded,
    ValidationFailed,
    BranchNotFound,
    BranchNotReservable,
    PolicyNotFound,
    Conflict,
    PersistenceFailure
}

public sealed record BranchReservationPolicyEvaluationResult(
    BranchReservationPolicyEvaluationStatus Status,
    int? MinGuests = null)
{
    public bool IsAllowed => Status == BranchReservationPolicyEvaluationStatus.Allowed;
}

public enum BranchReservationPolicyEvaluationStatus
{
    Allowed,
    InvalidBranch,
    InvalidGuestCount,
    BranchClosedDate,
    MinimumGuestsNotMet
}

public sealed record ReservationStatusCount(ReservationStatus Status, int Count);

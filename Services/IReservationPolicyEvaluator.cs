namespace KIGHolding.Services;

public interface IReservationPolicyEvaluator
{
    Task<ReservationPolicyEvaluationResult> EvaluateAsync(
        Guid branchId,
        DateOnly reservationDate,
        int guestCount,
        DateOnly vietnamToday,
        CancellationToken cancellationToken = default);
}

public sealed record ReservationPolicyEvaluationResult(
    ReservationPolicyEvaluationStatus Status,
    int? MinGuests = null)
{
    public bool IsAllowed => Status == ReservationPolicyEvaluationStatus.Allowed;
}

public enum ReservationPolicyEvaluationStatus
{
    Allowed,
    PastDate,
    GlobalBlockedDate,
    InvalidBranch,
    InvalidGuestCount,
    BranchClosedDate,
    MinimumGuestsNotMet
}

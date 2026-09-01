namespace KIGHolding.Services;

public sealed record ReservationDatePolicyResult(ReservationDatePolicyStatus Status)
{
    public bool IsAllowed => Status == ReservationDatePolicyStatus.Allowed;
}

public enum ReservationDatePolicyStatus
{
    Allowed,
    PastDate,
    BlockedDate
}

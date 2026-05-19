namespace KIGHolding.Services;

public class ReservationPolicyResult
{
    public bool IsAllowed => Errors.Count == 0;
    public IReadOnlyList<ReservationPolicyError> Errors { get; init; } = [];

    public static ReservationPolicyResult Success()
    {
        return new ReservationPolicyResult();
    }

    public static ReservationPolicyResult Failed(IReadOnlyList<ReservationPolicyError> errors)
    {
        return new ReservationPolicyResult { Errors = errors };
    }
}

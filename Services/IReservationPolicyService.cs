namespace KIGHolding.Services;

public interface IReservationPolicyService
{
    DateOnly GetVietnamToday();
    Task<ReservationPolicyResult> ValidateAsync(ReservationPolicyRequest request, CancellationToken cancellationToken = default);
}

namespace KIGHolding.Services;

public interface IReservationBlockedDateService
{
    Task<ReservationDatePolicyResult> EvaluateReservationDateAsync(
        DateOnly reservationDate,
        DateOnly vietnamToday,
        CancellationToken cancellationToken = default);

    Task<bool> IsBlockedAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DateOnly>> GetActiveBlockedDatesAsync(
        DateOnly vietnamToday,
        CancellationToken cancellationToken = default);

    Task ReplaceActiveBlockedDatesAsync(
        IReadOnlyCollection<DateOnly> dates,
        DateOnly vietnamToday,
        CancellationToken cancellationToken = default);

    Task<int> CleanupPastDatesAsync(
        DateOnly vietnamToday,
        CancellationToken cancellationToken = default);
}

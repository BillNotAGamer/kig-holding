namespace KIGHolding.Services;

public interface IReservationPolicyLockService
{
    Task AcquireDateLockAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task AcquireDateLocksAsync(
        IEnumerable<DateOnly> dates,
        CancellationToken cancellationToken = default);

    Task AcquireGlobalMutationLockAsync(CancellationToken cancellationToken = default);
}

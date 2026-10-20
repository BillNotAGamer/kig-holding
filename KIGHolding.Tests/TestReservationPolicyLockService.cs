using KIGHolding.Services;

namespace KIGHolding.Tests;

public sealed class TestReservationPolicyLockService : IReservationPolicyLockService
{
    public List<DateOnly> AcquiredDates { get; } = [];
    public int GlobalMutationLockCount { get; private set; }

    public Task AcquireDateLockAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        AcquiredDates.Add(date);
        return Task.CompletedTask;
    }

    public async Task AcquireDateLocksAsync(
        IEnumerable<DateOnly> dates,
        CancellationToken cancellationToken = default)
    {
        foreach (var date in dates.Distinct().OrderBy(date => date))
        {
            await AcquireDateLockAsync(date, cancellationToken);
        }
    }

    public Task AcquireGlobalMutationLockAsync(CancellationToken cancellationToken = default)
    {
        GlobalMutationLockCount++;
        return Task.CompletedTask;
    }
}

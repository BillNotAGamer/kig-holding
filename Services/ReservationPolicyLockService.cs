using KIGHolding.Data;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Services;

public sealed class ReservationPolicyLockService : IReservationPolicyLockService
{
    // Two-int PostgreSQL advisory locks have a key space separate from the existing
    // one-bigint phone lock. The first integer is a fixed KIG policy namespace and
    // the second is DateOnly.DayNumber, which is deterministic across processes.
    public const int PolicyDateLockNamespace = 1_263_092_560;
    public const int GlobalMutationLockNamespace = 1_263_092_563;
    private const int GlobalMutationLockKey = 1;

    private readonly AppDbContext _dbContext;

    public ReservationPolicyLockService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AcquireDateLockAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        EnsureActiveTransaction();

        await _dbContext.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0}, {1})",
            [PolicyDateLockNamespace, date.DayNumber],
            cancellationToken);
    }

    public async Task AcquireDateLocksAsync(
        IEnumerable<DateOnly> dates,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dates);

        foreach (var date in dates.Distinct().OrderBy(date => date))
        {
            await AcquireDateLockAsync(date, cancellationToken);
        }
    }

    public async Task AcquireGlobalMutationLockAsync(CancellationToken cancellationToken = default)
    {
        EnsureActiveTransaction();

        await _dbContext.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0}, {1})",
            [GlobalMutationLockNamespace, GlobalMutationLockKey],
            cancellationToken);
    }

    private void EnsureActiveTransaction()
    {
        if (_dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("A database transaction is required before acquiring a reservation policy lock.");
        }
    }
}

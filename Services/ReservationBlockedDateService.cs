using System.Data;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Services;

public sealed class ReservationBlockedDateService : IReservationBlockedDateService
{
    public const string PastDateMessage = "Ngày đến không được sớm hơn hôm nay.";
    public const string BlockedDateMessage = "Nhà hàng không nhận đặt bàn vào ngày này. Vui lòng chọn ngày khác hoặc liên hệ hotline.";

    private readonly AppDbContext _dbContext;
    private readonly IReservationPolicyLockService _policyLockService;

    public ReservationBlockedDateService(
        AppDbContext dbContext,
        IReservationPolicyLockService policyLockService)
    {
        _dbContext = dbContext;
        _policyLockService = policyLockService;
    }

    public async Task<ReservationDatePolicyResult> EvaluateReservationDateAsync(
        DateOnly reservationDate,
        DateOnly vietnamToday,
        CancellationToken cancellationToken = default)
    {
        if (reservationDate < vietnamToday)
        {
            return new ReservationDatePolicyResult(ReservationDatePolicyStatus.PastDate);
        }

        if (await IsBlockedAsync(reservationDate, cancellationToken))
        {
            return new ReservationDatePolicyResult(ReservationDatePolicyStatus.BlockedDate);
        }

        return new ReservationDatePolicyResult(ReservationDatePolicyStatus.Allowed);
    }

    public Task<bool> IsBlockedAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return _dbContext.BlockedReservationDates
            .AsNoTracking()
            .AnyAsync(x => x.Date == date, cancellationToken);
    }

    public async Task<IReadOnlyList<DateOnly>> GetActiveBlockedDatesAsync(
        DateOnly vietnamToday,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.BlockedReservationDates
            .AsNoTracking()
            .Where(x => x.Date >= vietnamToday)
            .OrderBy(x => x.Date)
            .Select(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceActiveBlockedDatesAsync(
        IReadOnlyCollection<DateOnly> dates,
        DateOnly vietnamToday,
        CancellationToken cancellationToken = default)
    {
        var submittedDates = dates
            .Where(date => date >= vietnamToday)
            .Distinct()
            .ToHashSet();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        await _policyLockService.AcquireGlobalMutationLockAsync(cancellationToken);

        var currentRows = await _dbContext.BlockedReservationDates
            .ToListAsync(cancellationToken);

        var currentActiveDates = currentRows
            .Where(x => x.Date >= vietnamToday)
            .Select(x => x.Date)
            .ToHashSet();
        var affectedDates = currentRows
            .Where(x => x.Date < vietnamToday || !submittedDates.Contains(x.Date))
            .Select(x => x.Date)
            .Concat(submittedDates.Where(date => !currentActiveDates.Contains(date)))
            .ToArray();

        await _policyLockService.AcquireDateLocksAsync(affectedDates, cancellationToken);

        var rowsToRemove = currentRows
            .Where(x => x.Date < vietnamToday || !submittedDates.Contains(x.Date))
            .ToArray();
        var datesToAdd = submittedDates
            .Where(date => !currentActiveDates.Contains(date))
            .Select(date => new BlockedReservationDate { Date = date })
            .ToArray();

        if (rowsToRemove.Length > 0)
        {
            _dbContext.BlockedReservationDates.RemoveRange(rowsToRemove);
        }

        if (datesToAdd.Length > 0)
        {
            _dbContext.BlockedReservationDates.AddRange(datesToAdd);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<int> CleanupPastDatesAsync(
        DateOnly vietnamToday,
        CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.IsRelational())
        {
            return await _dbContext.BlockedReservationDates
                .Where(x => x.Date < vietnamToday)
                .ExecuteDeleteAsync(cancellationToken);
        }

        var expiredRows = await _dbContext.BlockedReservationDates
            .Where(x => x.Date < vietnamToday)
            .ToListAsync(cancellationToken);

        if (expiredRows.Count == 0)
        {
            return 0;
        }

        _dbContext.BlockedReservationDates.RemoveRange(expiredRows);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return expiredRows.Count;
    }

    public static string GetReservationDatePolicyMessage(ReservationDatePolicyStatus status)
    {
        return status switch
        {
            ReservationDatePolicyStatus.PastDate => PastDateMessage,
            ReservationDatePolicyStatus.BlockedDate => BlockedDateMessage,
            _ => string.Empty
        };
    }
}

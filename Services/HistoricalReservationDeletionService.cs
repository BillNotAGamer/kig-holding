using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Services;

public sealed class HistoricalReservationDeletionService : IHistoricalReservationDeletionService
{
    private const string SuperAdminRole = "SuperAdmin";

    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public HistoricalReservationDeletionService(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<HistoricalReservationDeletionEligibility> GetEligibilityAsync(
        Guid reservationId,
        Guid actorAdminUserId,
        CancellationToken cancellationToken = default)
    {
        var status = await EvaluateAsync(reservationId, actorAdminUserId, cancellationToken);
        return new HistoricalReservationDeletionEligibility(status);
    }

    public async Task<HistoricalReservationDeletionResult> SoftDeleteAsync(
        HistoricalReservationDeletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ReservationId == Guid.Empty ||
            request.ExpectedVersion == Guid.Empty ||
            request.ActorAdminUserId == Guid.Empty)
        {
            return new HistoricalReservationDeletionResult(HistoricalReservationDeletionStatus.NotEligible);
        }

        if (!await IsActiveSuperAdminAsync(request.ActorAdminUserId, cancellationToken))
        {
            return new HistoricalReservationDeletionResult(HistoricalReservationDeletionStatus.Unauthorized);
        }

        var reservation = await _dbContext.Reservations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ReservationId, cancellationToken);
        if (reservation is null)
        {
            return new HistoricalReservationDeletionResult(HistoricalReservationDeletionStatus.NotFound);
        }

        if (reservation.IsDeleted)
        {
            return new HistoricalReservationDeletionResult(HistoricalReservationDeletionStatus.AlreadyDeleted);
        }

        if (reservation.Version != request.ExpectedVersion)
        {
            return new HistoricalReservationDeletionResult(HistoricalReservationDeletionStatus.Conflict);
        }

        if (!IsEligible(reservation, VietnamClock.GetVietnamNow(_timeProvider)))
        {
            return new HistoricalReservationDeletionResult(HistoricalReservationDeletionStatus.NotEligible);
        }

        reservation.IsDeleted = true;
        reservation.DeletedAt = _timeProvider.GetUtcNow();
        reservation.DeletedByAdminUserId = request.ActorAdminUserId;
        reservation.Version = Guid.NewGuid();

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new HistoricalReservationDeletionResult(HistoricalReservationDeletionStatus.Succeeded);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(reservation).State = EntityState.Detached;
            return new HistoricalReservationDeletionResult(HistoricalReservationDeletionStatus.Conflict);
        }
    }

    private async Task<HistoricalReservationDeletionStatus> EvaluateAsync(
        Guid reservationId,
        Guid actorAdminUserId,
        CancellationToken cancellationToken)
    {
        if (reservationId == Guid.Empty || actorAdminUserId == Guid.Empty ||
            !await IsActiveSuperAdminAsync(actorAdminUserId, cancellationToken))
        {
            return HistoricalReservationDeletionStatus.Unauthorized;
        }

        var reservation = await _dbContext.Reservations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == reservationId, cancellationToken);
        if (reservation is null)
        {
            return HistoricalReservationDeletionStatus.NotFound;
        }

        if (reservation.IsDeleted)
        {
            return HistoricalReservationDeletionStatus.AlreadyDeleted;
        }

        return IsEligible(reservation, VietnamClock.GetVietnamNow(_timeProvider))
            ? HistoricalReservationDeletionStatus.Succeeded
            : HistoricalReservationDeletionStatus.NotEligible;
    }

    private async Task<bool> IsActiveSuperAdminAsync(Guid actorAdminUserId, CancellationToken cancellationToken)
    {
        return await _dbContext.AdminUsers
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == actorAdminUserId && user.IsActive && user.Role == SuperAdminRole,
                cancellationToken);
    }

    private static bool IsEligible(Reservation reservation, DateTimeOffset vietnamNow)
    {
        if (reservation.Status is not (ReservationStatus.Confirmed or ReservationStatus.Arrived))
        {
            return false;
        }

        var scheduledVietnamLocal = reservation.ReservationDate.ToDateTime(reservation.ReservationTime);
        return scheduledVietnamLocal < vietnamNow.DateTime;
    }
}

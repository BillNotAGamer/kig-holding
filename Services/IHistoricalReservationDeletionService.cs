namespace KIGHolding.Services;

public interface IHistoricalReservationDeletionService
{
    Task<HistoricalReservationDeletionEligibility> GetEligibilityAsync(
        Guid reservationId,
        Guid actorAdminUserId,
        CancellationToken cancellationToken = default);

    Task<HistoricalReservationDeletionResult> SoftDeleteAsync(
        HistoricalReservationDeletionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record HistoricalReservationDeletionRequest(
    Guid ReservationId,
    Guid ExpectedVersion,
    Guid ActorAdminUserId);

public sealed record HistoricalReservationDeletionEligibility(
    HistoricalReservationDeletionStatus Status)
{
    public bool IsEligible => Status == HistoricalReservationDeletionStatus.Succeeded;
}

public sealed record HistoricalReservationDeletionResult(
    HistoricalReservationDeletionStatus Status)
{
    public bool Succeeded => Status == HistoricalReservationDeletionStatus.Succeeded;
}

public enum HistoricalReservationDeletionStatus
{
    Succeeded,
    NotFound,
    NotEligible,
    AlreadyDeleted,
    Conflict,
    Unauthorized
}

namespace KIGHolding.Services;

public class ReservationCreateResult
{
    public Guid? ReservationId { get; init; }
    public IReadOnlyList<ReservationServiceError> Errors { get; init; } = [];

    public bool Succeeded => ReservationId.HasValue && Errors.Count == 0;

    public static ReservationCreateResult Success(Guid reservationId)
    {
        return new ReservationCreateResult { ReservationId = reservationId };
    }

    public static ReservationCreateResult Failed(IReadOnlyList<ReservationServiceError> errors)
    {
        return new ReservationCreateResult { Errors = errors };
    }
}

public class ReservationServiceError
{
    public string FieldName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

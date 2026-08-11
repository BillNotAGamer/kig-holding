namespace KIGHolding.Services.Notifications;

public sealed record AdminReservationCreatedNotification(
    Guid ReservationId,
    string CustomerName,
    string BranchName,
    DateOnly ReservationDate,
    TimeOnly ReservationTime,
    int GuestCount,
    DateTimeOffset CreatedAt,
    string Source);

namespace KIGHolding.Services.Notifications;

public interface IAdminReservationNotifier
{
    Task NotifyReservationCreatedAsync(
        AdminReservationCreatedNotification notification,
        CancellationToken cancellationToken = default);
}

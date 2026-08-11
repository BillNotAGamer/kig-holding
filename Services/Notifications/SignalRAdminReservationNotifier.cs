using KIGHolding.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace KIGHolding.Services.Notifications;

public sealed class SignalRAdminReservationNotifier : IAdminReservationNotifier
{
    private readonly IHubContext<AdminReservationNotificationHub> _hubContext;

    public SignalRAdminReservationNotifier(IHubContext<AdminReservationNotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyReservationCreatedAsync(
        AdminReservationCreatedNotification notification,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.All.SendAsync(
            AdminReservationNotificationEvents.ReservationCreated,
            notification,
            cancellationToken);
    }
}

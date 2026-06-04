namespace KIGHolding.Services;

public interface IEmailService
{
    Task SendReservationNotificationAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default);
}

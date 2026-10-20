using Microsoft.Extensions.Logging;

namespace KIGHolding.Services;

public sealed class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    public NoOpEmailService(ILogger<NoOpEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(OutboundEmailDeliveryPolicy.SuppressedPolicyLogMessage);
        return Task.CompletedTask;
    }

    public Task SendReservationNotificationAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(OutboundEmailDeliveryPolicy.SuppressedPolicyLogMessage);
        return Task.CompletedTask;
    }
}

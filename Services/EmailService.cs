using KIGHolding.Options;
using Microsoft.Extensions.Options;
using Resend;

namespace KIGHolding.Services;

public sealed class EmailService : IEmailService
{
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(15);

    private readonly IResend _resend;
    private readonly ResendSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IResend resend, IOptions<ResendSettings> settings, ILogger<EmailService> logger)
    {
        _resend = resend;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendReservationNotificationAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateConfiguration(recipientEmail, out var warning))
        {
            _logger.LogWarning("Skipping reservation notification email. {Warning}", warning);
            return;
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(SendTimeout);

            var message = CreateMessage(recipientEmail, subject, htmlBody, textBody);
            var response = await _resend.EmailSendAsync(message, timeoutCts.Token);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Reservation notification email via Resend was not accepted for {Recipient}. StatusCode={StatusCode}, ErrorType={ErrorType}, Message={Message}",
                    recipientEmail,
                    response.Exception?.StatusCode,
                    response.Exception?.ErrorType,
                    response.Exception?.Message);
            }
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "Reservation notification email via Resend timed out after {TimeoutSeconds}s for {Recipient}.",
                SendTimeout.TotalSeconds,
                recipientEmail);
        }
        catch (ResendException exception)
        {
            _logger.LogWarning(exception, "Reservation notification email via Resend failed for {Recipient}.", recipientEmail);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Reservation notification email via Resend failed unexpectedly for {Recipient}.", recipientEmail);
        }
    }

    private bool TryValidateConfiguration(string recipientEmail, out string warning)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            warning = "Recipient email is empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            warning = "ResendSettings:ApiKey is missing.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            warning = "ResendSettings:FromEmail is missing.";
            return false;
        }

        warning = string.Empty;
        return true;
    }

    private EmailMessage CreateMessage(string recipientEmail, string subject, string htmlBody, string textBody)
    {
        var fromDisplayName = string.IsNullOrWhiteSpace(_settings.FromName)
            ? _settings.FromEmail.Trim()
            : _settings.FromName.Trim();

        var message = new EmailMessage
        {
            From = $"{fromDisplayName} <{_settings.FromEmail.Trim()}>",
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody
        };

        message.To.Add(recipientEmail.Trim());
        return message;
    }
}

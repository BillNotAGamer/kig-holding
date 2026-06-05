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

    public Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        return SendCoreAsync(recipientEmail, subject, htmlBody, textBody, "generic email", cancellationToken);
    }

    public async Task SendReservationNotificationAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        await SendCoreAsync(recipientEmail, subject, htmlBody, textBody, "reservation notification email", cancellationToken);
    }

    private async Task SendCoreAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string textBody,
        string workflowName,
        CancellationToken cancellationToken)
    {
        if (!TryValidateConfiguration(recipientEmail, subject, htmlBody, textBody, out var warning))
        {
            _logger.LogWarning("Skipping {WorkflowName}. {Warning}", workflowName, warning);
            return;
        }

        var normalizedRecipient = recipientEmail.Trim();
        var normalizedSubject = subject.Trim();
        var normalizedHtmlBody = string.IsNullOrWhiteSpace(htmlBody) ? string.Empty : htmlBody.Trim();
        var normalizedTextBody = string.IsNullOrWhiteSpace(textBody) ? string.Empty : textBody.Trim();

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(SendTimeout);

            var message = CreateMessage(normalizedRecipient, normalizedSubject, normalizedHtmlBody, normalizedTextBody);
            var response = await _resend.EmailSendAsync(message, timeoutCts.Token);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "{WorkflowName} via Resend was not accepted for {Recipient}. StatusCode={StatusCode}, ErrorType={ErrorType}, Message={Message}",
                    workflowName,
                    normalizedRecipient,
                    response.Exception?.StatusCode,
                    response.Exception?.ErrorType,
                    response.Exception?.Message);
            }
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "{WorkflowName} via Resend timed out after {TimeoutSeconds}s for {Recipient}.",
                workflowName,
                SendTimeout.TotalSeconds,
                normalizedRecipient);
        }
        catch (ResendException exception)
        {
            _logger.LogWarning(exception, "{WorkflowName} via Resend failed for {Recipient}.", workflowName, normalizedRecipient);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "{WorkflowName} via Resend failed unexpectedly for {Recipient}.", workflowName, normalizedRecipient);
        }
    }

    private bool TryValidateConfiguration(string recipientEmail, string subject, string htmlBody, string textBody, out string warning)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            warning = "Recipient email is empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            warning = "Email subject is empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(htmlBody) && string.IsNullOrWhiteSpace(textBody))
        {
            warning = "Both HTML and text bodies are empty.";
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

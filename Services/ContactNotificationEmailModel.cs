namespace KIGHolding.Services;

public sealed class ContactNotificationEmailModel
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Subject { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

namespace KIGHolding.Options;

public sealed class ResendSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Truyền Thuyết Champong";
    public string BusinessRecipientEmail { get; set; } = "truyenthuyetchamponghcm@gmail.com";
}

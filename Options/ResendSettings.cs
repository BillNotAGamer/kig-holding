namespace KIGHolding.Options;

public sealed class ResendSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "no-reply@kigholding.vn";
    public string FromName { get; set; } = "Truyền Thuyết Champong";
    public string BusinessRecipientEmail { get; set; } = "truyenthuyetchamponghcm@gmail.com";
    // On by default in Production; set ResendSettings__EnableOutboundEmail=false to turn off.
    public bool EnableOutboundEmail { get; set; } = true;
}

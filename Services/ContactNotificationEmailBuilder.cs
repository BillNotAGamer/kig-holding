using System.Net;
using System.Text;

namespace KIGHolding.Services;

public static class ContactNotificationEmailBuilder
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public static string BuildSubject()
    {
        return "[Truyền Thuyết Champong] Liên hệ mới từ website";
    }

    public static string BuildHtmlBody(ContactNotificationEmailModel model)
    {
        var formattedCreatedAt = FormatCreatedAt(model.CreatedAt);

        return $"""
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="utf-8" />
    <title>{Html(BuildSubject())}</title>
</head>
<body style="margin:0;padding:24px;background:#0b0b0b;color:#f5f5f5;font-family:Arial,Helvetica,sans-serif;">
    <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="max-width:720px;margin:0 auto;background:#111827;border:1px solid rgba(255,255,255,0.12);border-radius:20px;overflow:hidden;">
        <tr>
            <td style="padding:28px 32px;background:linear-gradient(135deg,#7f1d1d 0%,#111827 72%);">
                <div style="font-size:12px;letter-spacing:0.18em;text-transform:uppercase;color:#fca5a5;">Thông báo liên hệ mới</div>
                <h1 style="margin:12px 0 0;font-size:28px;line-height:1.15;color:#ffffff;">Liên hệ mới từ website</h1>
                <p style="margin:12px 0 0;font-size:15px;line-height:1.7;color:#e5e7eb;">Một tin nhắn liên hệ vừa được gửi từ biểu mẫu công khai. Thông tin này đã được lưu trong hệ thống quản trị admin.</p>
            </td>
        </tr>
        <tr>
            <td style="padding:28px 32px;">
                <h2 style="margin:0 0 12px;font-size:16px;color:#ffffff;">Thông tin người gửi</h2>
                <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;">
                    {Row("Mã liên hệ", model.Id.ToString())}
                    {Row("Thời gian gửi", formattedCreatedAt)}
                    {Row("Họ tên", model.FullName)}
                    {Row("Số điện thoại", model.PhoneNumber)}
                    {Row("Email", FallbackEmail(model.Email))}
                    {Row("Chủ đề", FallbackSubject(model.Subject))}
                    {RowHtml("Nội dung", HtmlMultiline(model.Message))}
                </table>
                <p style="margin:18px 0 0;font-size:13px;line-height:1.7;color:#d1d5db;">Ghi chú: Tin nhắn đã được lưu trong hệ thống quản trị admin.</p>
            </td>
        </tr>
    </table>
</body>
</html>
""";
    }

    public static string BuildTextBody(ContactNotificationEmailModel model)
    {
        var builder = new StringBuilder();
        builder.AppendLine("LIÊN HỆ MỚI TỪ WEBSITE");
        builder.AppendLine();
        builder.AppendLine($"Mã liên hệ: {model.Id}");
        builder.AppendLine($"Thời gian gửi: {FormatCreatedAt(model.CreatedAt)}");
        builder.AppendLine($"Họ tên: {model.FullName.Trim()}");
        builder.AppendLine($"Số điện thoại: {model.PhoneNumber.Trim()}");
        builder.AppendLine($"Email: {FallbackEmail(model.Email)}");
        builder.AppendLine($"Chủ đề: {FallbackSubject(model.Subject)}");
        builder.AppendLine("Nội dung:");
        builder.AppendLine(string.IsNullOrWhiteSpace(model.Message) ? "Không có" : model.Message.Trim());
        builder.AppendLine();
        builder.AppendLine("Ghi chú: Tin nhắn đã được lưu trong hệ thống quản trị admin.");
        return builder.ToString();
    }

    private static string Row(string label, string value)
    {
        return $"""
<tr>
    <td style="padding:11px 0;border-bottom:1px solid rgba(255,255,255,0.08);width:180px;font-weight:700;color:#fca5a5;vertical-align:top;">{Html(label)}</td>
    <td style="padding:11px 0;border-bottom:1px solid rgba(255,255,255,0.08);color:#f9fafb;line-height:1.7;">{Html(value)}</td>
</tr>
""";
    }

    private static string RowHtml(string label, string htmlValue)
    {
        return $"""
<tr>
    <td style="padding:11px 0;border-bottom:1px solid rgba(255,255,255,0.08);width:180px;font-weight:700;color:#fca5a5;vertical-align:top;">{Html(label)}</td>
    <td style="padding:11px 0;border-bottom:1px solid rgba(255,255,255,0.08);color:#f9fafb;line-height:1.7;">{htmlValue}</td>
</tr>
""";
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(Fallback(value));
    }

    private static string HtmlMultiline(string? value)
    {
        var encoded = WebUtility.HtmlEncode(Fallback(value));
        return encoded
            .Replace("\r\n", "<br />", StringComparison.Ordinal)
            .Replace("\n", "<br />", StringComparison.Ordinal)
            .Replace("\r", "<br />", StringComparison.Ordinal);
    }

    private static string Fallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Không có" : value.Trim();
    }

    private static string FallbackEmail(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Không cung cấp" : value.Trim();
    }

    private static string FallbackSubject(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Không có" : value.Trim();
    }

    private static string FormatCreatedAt(DateTimeOffset value)
    {
        return value.ToOffset(VietnamOffset).ToString("dd/MM/yyyy HH:mm '(GMT+7)'");
    }
}

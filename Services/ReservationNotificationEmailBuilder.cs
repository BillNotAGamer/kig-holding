using System.Net;
using System.Text;

namespace KIGHolding.Services;

public static class ReservationNotificationEmailBuilder
{
    public static string BuildSubject()
    {
        return "[Truyền Thuyết Champong] Yêu cầu đặt bàn mới";
    }

    public static string BuildHtmlBody(ReservationNotificationEmailModel model)
    {
        var note = string.IsNullOrWhiteSpace(model.Note) ? "Không có" : model.Note.Trim();
        var branchName = string.IsNullOrWhiteSpace(model.BranchName) ? "Chưa cập nhật" : model.BranchName.Trim();
        var branchAddress = string.IsNullOrWhiteSpace(model.BranchAddress) ? "Chưa cập nhật" : model.BranchAddress.Trim();
        var additionalRows = string.Concat(
            OptionalRow("Hình thức dùng bữa", model.DiningOccasionDisplay),
            OptionalRow("Ghi chú hình thức khác", model.DiningOccasionOtherNote));

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
                <div style="font-size:12px;letter-spacing:0.18em;text-transform:uppercase;color:#fca5a5;">Thông báo đặt bàn mới</div>
                <h1 style="margin:12px 0 0;font-size:28px;line-height:1.15;color:#ffffff;">Yêu cầu đặt bàn mới từ website</h1>
                <p style="margin:12px 0 0;font-size:15px;line-height:1.7;color:#e5e7eb;">Một yêu cầu đặt bàn vừa được ghi nhận. Vui lòng kiểm tra và liên hệ khách nếu cần xác nhận thêm.</p>
            </td>
        </tr>
        <tr>
            <td style="padding:28px 32px;">
                <h2 style="margin:0 0 12px;font-size:16px;color:#ffffff;">Thông tin khách hàng</h2>
                <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;">
                    {Row("Mã đặt bàn", model.ReservationId.ToString())}
                    {Row("Họ tên", model.CustomerName)}
                    {Row("Số điện thoại", model.PhoneNumber)}
                    {Row("Số khách", model.GuestCount.ToString())}
                    {Row("Ngày đến", model.ReservationDate.ToString("dd/MM/yyyy"))}
                    {Row("Giờ đến", model.ReservationTime.ToString("HH:mm"))}
                    {Row("Chi nhánh", branchName)}
                    {Row("Địa chỉ chi nhánh", branchAddress)}
                    {additionalRows}
                    {Row("Ghi chú", note)}
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
""";
    }

    public static string BuildTextBody(ReservationNotificationEmailModel model)
    {
        var builder = new StringBuilder();
        builder.AppendLine("YÊU CẦU ĐẶT BÀN MỚI");
        builder.AppendLine();
        builder.AppendLine($"Mã đặt bàn: {model.ReservationId}");
        builder.AppendLine($"Họ tên: {model.CustomerName}");
        builder.AppendLine($"Số điện thoại: {model.PhoneNumber}");
        builder.AppendLine($"Số khách: {model.GuestCount}");
        builder.AppendLine($"Ngày đến: {model.ReservationDate:dd/MM/yyyy}");
        builder.AppendLine($"Giờ đến: {model.ReservationTime:HH\\:mm}");
        builder.AppendLine($"Chi nhánh: {Fallback(model.BranchName)}");
        builder.AppendLine($"Địa chỉ chi nhánh: {Fallback(model.BranchAddress)}");
        AppendOptionalLine(builder, "Hình thức dùng bữa", model.DiningOccasionDisplay);
        AppendOptionalLine(builder, "Ghi chú hình thức khác", model.DiningOccasionOtherNote);
        builder.AppendLine($"Ghi chú: {Fallback(model.Note)}");
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

    private static string OptionalRow(string label, string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : Row(label, value);
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(Fallback(value));
    }

    private static void AppendOptionalLine(StringBuilder builder, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine($"{label}: {value.Trim()}");
        }
    }

    private static string Fallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Không có" : value.Trim();
    }
}

public sealed class ReservationNotificationEmailModel
{
    public Guid ReservationId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public int GuestCount { get; init; }
    public DateOnly ReservationDate { get; init; }
    public TimeOnly ReservationTime { get; init; }
    public string? BranchName { get; init; }
    public string? BranchAddress { get; init; }
    public string? DiningOccasionDisplay { get; init; }
    public string? DiningOccasionOtherNote { get; init; }
    public string? Note { get; init; }
}

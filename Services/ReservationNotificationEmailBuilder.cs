using System.Globalization;
using System.Net;
using System.Text;

namespace KIGHolding.Services;

public static class ReservationNotificationEmailBuilder
{
    public static string BuildSubject(ReservationNotificationEmailModel model)
    {
        var reference = BuildReference(model.ReservationId);
        var reservationDate = model.ReservationDate.ToString("dd/MM", CultureInfo.InvariantCulture);
        var reservationTime = model.ReservationTime.ToString("HH:mm", CultureInfo.InvariantCulture);

        return $"[Champong] Đặt bàn #{reference} • {reservationDate} {reservationTime}";
    }

    public static string BuildHtmlBody(ReservationNotificationEmailModel model)
    {
        var subject = BuildSubject(model);
        var reference = BuildReference(model.ReservationId);
        var shortDate = model.ReservationDate.ToString("dd/MM", CultureInfo.InvariantCulture);
        var fullDate = model.ReservationDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var reservationTime = model.ReservationTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        var reservationDateTime = $"{fullDate} • {reservationTime}";
        var preheader = $"Đặt bàn #{reference} • {shortDate} {reservationTime} • {model.GuestCount} khách";

        var branchName = Fallback(model.BranchName, "Chưa cập nhật");
        var branchAddress = CleanOptional(model.BranchAddress);
        var note = Fallback(model.Note);

        var primaryRows = string.Concat(
            FieldRow("Ngày & giờ", Html(reservationDateTime)),
            FieldRow("Chi nhánh", BuildBranchValueHtml(branchName, branchAddress)),
            FieldRow("Số khách", Html($"{model.GuestCount} khách")));

        var customerRows = string.Concat(
            FieldRow("Khách hàng", Html(Fallback(model.CustomerName))),
            FieldRow("Số điện thoại", BuildPhoneValueHtml(model.PhoneNumber)));

        var additionalRows = string.Concat(
            OptionalFieldRow("Dịp dùng bữa", model.DiningOccasionDisplay),
            OptionalFieldRow("Chi tiết dịp dùng bữa", model.DiningOccasionOtherNote),
            FieldRow("Ghi chú", Html(note)));

        return $"""
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="x-apple-disable-message-reformatting" />
    <title>{Html(subject)}</title>
</head>
<body style="margin:0;padding:0;background:#0b0b0b;">
    <div style="display:none !important;font-size:1px;line-height:1px;color:#0b0b0b;max-height:0;max-width:0;opacity:0;overflow:hidden;mso-hide:all;">
        {Html(preheader)}
    </div>
    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="width:100%;margin:0;padding:0;background:#0b0b0b;">
        <tr>
            <td align="center" style="padding:12px;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="width:100%;max-width:560px;background:#111111;border:1px solid #2a2a2a;border-radius:18px;overflow:hidden;">
                    <tr>
                        <td style="padding:18px 20px;background:#7f1d1d;background-image:linear-gradient(135deg,#7f1d1d 0%,#3f0d0d 100%);">
                            <div style="font-size:11px;line-height:1.4;letter-spacing:0.12em;text-transform:uppercase;color:#fecaca;">
                                THÔNG BÁO ĐẶT BÀN
                            </div>
                            <h1 style="margin:8px 0 0;font-size:24px;line-height:1.2;color:#ffffff;font-weight:700;">
                                Yêu cầu đặt bàn mới
                            </h1>
                            <div style="margin-top:8px;font-size:13px;line-height:1.5;color:#fee2e2;overflow-wrap:anywhere;word-break:break-word;white-space:normal;">
                                #{Html(reference)}
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:16px 20px 18px;background:#111111;">
                            {Section("Thông tin chính", primaryRows)}
                            {Section("Thông tin khách hàng", customerRows)}
                            {Section("Thông tin bổ sung", additionalRows, removeBottomSpacing: true)}
                        </td>
                    </tr>
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
        var reference = BuildReference(model.ReservationId);
        var branchName = Fallback(model.BranchName, "Chưa cập nhật");
        var branchAddress = CleanOptional(model.BranchAddress);

        var builder = new StringBuilder();
        builder.AppendLine("YÊU CẦU ĐẶT BÀN MỚI");
        builder.AppendLine($"Mã tham chiếu: #{reference}");
        builder.AppendLine();
        builder.AppendLine($"Ngày & giờ: {model.ReservationDate:dd/MM/yyyy} {model.ReservationTime:HH\\:mm}");
        builder.AppendLine($"Chi nhánh: {branchName}");

        if (!string.IsNullOrWhiteSpace(branchAddress))
        {
            builder.AppendLine($"Địa chỉ chi nhánh: {branchAddress}");
        }

        builder.AppendLine($"Số khách: {model.GuestCount}");
        builder.AppendLine();
        builder.AppendLine($"Khách hàng: {Fallback(model.CustomerName)}");
        builder.AppendLine($"Điện thoại: {Fallback(model.PhoneNumber)}");
        builder.AppendLine();

        AppendOptionalLine(builder, "Dịp dùng bữa", model.DiningOccasionDisplay);
        AppendOptionalLine(builder, "Chi tiết dịp dùng bữa", model.DiningOccasionOtherNote);
        builder.AppendLine($"Ghi chú: {Fallback(model.Note)}");

        return builder.ToString();
    }

    private static string Section(string title, string rows, bool removeBottomSpacing = false)
    {
        var bottomPadding = removeBottomSpacing ? "0" : "16px";

        return $"""
<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="width:100%;margin:0 0 {bottomPadding};">
    <tr>
        <td style="padding:0 0 8px;font-size:14px;line-height:1.4;font-weight:700;color:#ffffff;">
            {Html(title)}
        </td>
    </tr>
    {rows}
</table>
""";
    }

    private static string FieldRow(string label, string valueHtml, string? valueStyle = null)
    {
        var resolvedValueStyle = string.IsNullOrWhiteSpace(valueStyle)
            ? "margin-top:4px;font-size:15px;line-height:1.55;color:#ffffff;overflow-wrap:anywhere;word-break:break-word;white-space:normal;"
            : valueStyle;

        return $"""
<tr>
    <td style="padding:10px 0;border-bottom:1px solid #2a2a2a;overflow-wrap:anywhere;word-break:break-word;white-space:normal;">
        <div style="font-size:11px;line-height:1.4;letter-spacing:0.08em;text-transform:uppercase;color:#f87171;">
            {Html(label)}
        </div>
        <div style="{resolvedValueStyle}">
            {valueHtml}
        </div>
    </td>
</tr>
""";
    }

    private static string OptionalFieldRow(string label, string? value)
    {
        var resolvedValue = CleanOptional(value);
        return string.IsNullOrWhiteSpace(resolvedValue)
            ? string.Empty
            : FieldRow(label, Html(resolvedValue));
    }

    private static string BuildBranchValueHtml(string branchName, string? branchAddress)
    {
        var encodedName = Html(branchName);
        if (string.IsNullOrWhiteSpace(branchAddress))
        {
            return encodedName;
        }

        return $"""
{encodedName}
<div style="margin-top:4px;font-size:13px;line-height:1.5;color:#d1d5db;overflow-wrap:anywhere;word-break:break-word;white-space:normal;">
    {Html(branchAddress)}
</div>
""";
    }

    private static string BuildPhoneValueHtml(string? phoneNumber)
    {
        var displayValue = Html(Fallback(phoneNumber));
        var telephoneHref = BuildTelephoneHref(phoneNumber);

        if (string.IsNullOrWhiteSpace(telephoneHref))
        {
            return displayValue;
        }

        return $"""<a href="{HtmlAttribute(telephoneHref)}" style="color:#ffffff;text-decoration:none;">{displayValue}</a>""";
    }

    private static string? BuildTelephoneHref(string? phoneNumber)
    {
        var trimmed = CleanOptional(phoneNumber);
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        var builder = new StringBuilder();
        if (trimmed.StartsWith('+'))
        {
            builder.Append('+');
        }

        foreach (var character in trimmed)
        {
            if (char.IsDigit(character))
            {
                builder.Append(character);
            }
        }

        if (builder.Length == 0 || (builder.Length == 1 && builder[0] == '+'))
        {
            return null;
        }

        return $"tel:{builder}";
    }

    private static string BuildReference(Guid reservationId)
    {
        return reservationId.ToString("N")[..8].ToUpperInvariant();
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private static string HtmlAttribute(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private static void AppendOptionalLine(StringBuilder builder, string label, string? value)
    {
        var resolvedValue = CleanOptional(value);
        if (!string.IsNullOrWhiteSpace(resolvedValue))
        {
            builder.AppendLine($"{label}: {resolvedValue}");
        }
    }

    private static string Fallback(string? value, string fallback = "Không có")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
    public DateTimeOffset? SubmittedAt { get; init; }
}

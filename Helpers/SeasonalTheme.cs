using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KIGHolding.Helpers;

/// <summary>
/// Giao diện theo mùa (hiện tại: Giáng Sinh). Tự bật theo ngày và tự tắt sau khi hết hạn,
/// không cần deploy lại để "về giao diện cũ".
///
/// Mặc định: 27/11/2026 -> hết ngày 31/12/2026 (giờ Việt Nam).
/// Có thể chỉnh trong appsettings:
///   "Seasonal": { "Christmas": { "Enabled": true|false, "Start": "2026-11-27", "End": "2026-12-31" } }
///   - Enabled = false  : tắt hẳn bất kể ngày.
///   - Enabled = true   : bật ép buộc (vẫn tôn trọng "End" nếu có).
/// Xem thử: thêm ?noel=1 (bật) hoặc ?noel=0 (tắt) vào URL, chỉ ảnh hưởng request đó.
/// </summary>
public static class SeasonalTheme
{
    public const string ChristmasLogoUrl = "/images/seasonal/champong-logo-santa.webp";

    private static readonly DateOnly DefaultStart = new(2026, 11, 27);
    private static readonly DateOnly DefaultEnd = new(2026, 12, 31);
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public static bool IsChristmas(HttpContext? context)
    {
        if (context is null)
        {
            return false;
        }

        var preview = context.Request.Query["noel"].ToString();
        if (preview == "1")
        {
            return true;
        }

        if (preview == "0")
        {
            return false;
        }

        var section = context.RequestServices.GetService<IConfiguration>()?.GetSection("Seasonal:Christmas");
        var enabledText = section?["Enabled"];
        if (bool.TryParse(enabledText, out var enabled) && !enabled)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow + VietnamOffset);
        var start = ParseDate(section?["Start"], DefaultStart);
        var end = ParseDate(section?["End"], DefaultEnd);

        if (bool.TryParse(enabledText, out enabled) && enabled)
        {
            return today <= end;
        }

        return today >= start && today <= end;
    }

    /// <summary>Thời điểm hết hạn (đầu ngày hôm sau "End", giờ VN) để JS phía client tự kiểm tra lần nữa.</summary>
    public static string EndIso(HttpContext? context)
    {
        var section = context?.RequestServices.GetService<IConfiguration>()?.GetSection("Seasonal:Christmas");
        var end = ParseDate(section?["End"], DefaultEnd).AddDays(1);
        return end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T00:00:00+07:00";
    }

    public static string Logo(HttpContext? context, string normalLogoUrl)
        => IsChristmas(context) ? ChristmasLogoUrl : normalLogoUrl;

    private static DateOnly ParseDate(string? value, DateOnly fallback)
        => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : fallback;
}

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using KIGHolding.Models.Content;

namespace KIGHolding.Services.AutoNews;

public sealed record ComposedArticle(
    string Title,
    string Slug,
    string Excerpt,
    string HtmlContent,
    string Category,
    string SeoTitle,
    string SeoDescription,
    string FocusKeyword,
    string FallbackImagePath);

public sealed record BrandProfile(string Name, string FocusKeyword, string Dishes, string ImagePath);

/// <summary>
/// Turns a Fanpage post into a clean, SEO-structured article while keeping the original
/// meaning: same facts, cleaner wording (no emoji/hashtag noise), title/meta/H2/CTA added.
/// </summary>
public static class SeoArticleComposer
{
    public static readonly BrandProfile Champong = new(
        "Truyền Thuyết Champong", "mì tương đen", "mì tương đen, mì champong, thịt heo chiên xù",
        "/images/home/images/truyen-thuyet-champong.webp");

    public static readonly BrandProfile KbbCook = new(
        "KBB Cook", "buffet nướng hàn quốc", "buffet nướng Hàn Quốc với thịt ủ lên men",
        "/images/home/images/kbb-cook-img.webp");

    public static readonly BrandProfile GogiMaru = new(
        "Gogi Maru", "thịt nướng hàn quốc", "BBQ Hàn Quốc với thịt tươi ủ lên men",
        "/images/home/images/gogimaru-img.webp");

    public static readonly BrandProfile SeoulGukbap = new(
        "Seoul Gukbap", "gukbap", "dwaeji gukbap – canh cơm xương heo kiểu Busan",
        "/images/home/images/seoul-gukbap.webp");

    /// <summary>Absolute site URL: the HTML sanitizer drops relative links.</summary>
    public const string SiteBaseUrl = "https://www.kigholding.vn";

    public const string BookingConditions =
        "Điều kiện đặt bàn: thứ 2 – thứ 6 từ 6 khách, thứ 7 – chủ nhật từ 10 khách; nhóm từ 20 khách cần đặt cọc giữ bàn.";

    public static BrandProfile DetectBrand(string text)
    {
        var value = text.ToLowerInvariant();
        if (value.Contains("kbb") || value.Contains("buffet"))
        {
            return KbbCook;
        }

        if (value.Contains("gogi") || value.Contains("고기"))
        {
            return GogiMaru;
        }

        if (value.Contains("gukbap") || value.Contains("seoul") || value.Contains("국밥"))
        {
            return SeoulGukbap;
        }

        return Champong;
    }

    public static string DetectCategory(string text)
    {
        var value = text.ToLowerInvariant();
        if (ContainsAny(value, "nhượng quyền", "hợp tác kinh doanh"))
        {
            return NewsCategories.NhuongQuyenHopTac;
        }

        if (ContainsAny(value, "khai trương", "chi nhánh mới", "cơ sở mới"))
        {
            return NewsCategories.HeThongChiNhanh;
        }

        if (ContainsAny(value, "giảm", "ưu đãi", "khuyến mãi", "tặng", "voucher", "combo", "sale", "free"))
        {
            return NewsCategories.KhuyenMaiUuDai;
        }

        if (ContainsAny(value, "món mới", "ra mắt", "menu", "thực đơn"))
        {
            return NewsCategories.MenuMonMoi;
        }

        if (ContainsAny(value, "sự kiện", "minigame", "giveaway", "noel", "tết", "halloween"))
        {
            return NewsCategories.SuKienHoatDong;
        }

        if (ContainsAny(value, "tuyển dụng", "thông báo", "lịch nghỉ"))
        {
            return NewsCategories.TinTucThongBao;
        }

        return NewsCategories.MenuMonMoi;
    }

    public static ComposedArticle? Compose(FacebookPagePost post)
    {
        var lines = CleanLines(StripSecondaryParts(post.Message));
        var bodyText = string.Join(' ', lines.Select(PlainText));
        if (bodyText.Length < 40)
        {
            return null;
        }

        var brand = DetectBrand(post.Message);
        var category = DetectCategory(post.Message);
        var title = BuildTitle(lines[0], brand);
        var bodyLines = lines.Count > 1 && NormalizeForCompare(lines[0]).Length <= 120 ? lines.Skip(1).ToList() : lines;
        if (bodyLines.Count == 0)
        {
            bodyLines = lines;
        }

        var description = Shorten(StripTrailingPunctuation(string.Join(' ', bodyLines.Select(PlainText))), 150);
        if (!description.Contains(brand.Name, StringComparison.OrdinalIgnoreCase))
        {
            description = Shorten($"{brand.Name}: {description}", 155);
        }

        var html = new StringBuilder();
        html.Append("<p><strong>")
            .Append(Encode(PlainText(bodyLines[0])))
            .Append("</strong></p>");

        var detailLines = bodyLines.Skip(1).ToList();
        if (detailLines.Count > 0)
        {
            html.Append("<h2>").Append(Encode(BuildSectionHeading(category, brand))).Append("</h2>");
            AppendBody(html, detailLines);
        }

        html.Append("<h2>").Append(Encode($"Về {brand.Name}")).Append("</h2>");
        html.Append("<p>")
            .Append(Encode($"{brand.Name} thuộc hệ thống KIG Holding, phục vụ {brand.Dishes} theo phong cách Hàn Quốc. "))
            .Append("Xem đầy đủ món tại <a href=\"" + SiteBaseUrl + "/thuc-don\">thực đơn</a> và ")
            .Append("<a href=\"" + SiteBaseUrl + "/chi-nhanh\">địa chỉ chi nhánh gần bạn</a>.</p>");

        html.Append("<h2>Đặt bàn trước để giữ chỗ</h2>");
        html.Append("<p>Bạn có thể <a href=\"" + SiteBaseUrl + "/dat-ban\">đặt bàn online</a> hoặc gọi hotline chi nhánh. ")
            .Append(Encode(BookingConditions))
            .Append("</p>");

        var sourceUrl = string.IsNullOrWhiteSpace(post.PermalinkUrl)
            ? $"https://www.facebook.com/{post.Id}"
            : post.PermalinkUrl!;
        html.Append("<p><em>Nguồn: <a href=\"")
            .Append(WebUtility.HtmlEncode(sourceUrl))
            .Append("\" target=\"_blank\" rel=\"nofollow noopener\">Fanpage ")
            .Append(Encode(brand.Name))
            .Append("</a>.</em></p>");

        var seoTitle = Shorten(title.Contains(brand.Name, StringComparison.OrdinalIgnoreCase) ? title : $"{title} | {brand.Name}", 65);
        var slugDate = post.CreatedTime.ToOffset(TimeSpan.FromHours(7)).ToString("ddMMyy", CultureInfo.InvariantCulture);

        return new ComposedArticle(
            title,
            $"{Slugify(Shorten(title, 70))}-{slugDate}",
            Shorten(description, 160),
            html.ToString(),
            category,
            seoTitle,
            Shorten(description, 155),
            brand.FocusKeyword,
            brand.ImagePath);
    }

    /// <summary>Unique marker of the Fanpage post kept in the article (used to skip duplicates).</summary>
    public static string SourceMarker(FacebookPagePost post)
    {
        return string.IsNullOrWhiteSpace(post.PermalinkUrl) ? $"facebook.com/{post.Id}" : post.PermalinkUrl!;
    }

    public static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var slug = Regex.Replace(builder.ToString(), @"[^a-z0-9\s-]", string.Empty);
        slug = Regex.Replace(slug, @"[\s-]+", "-").Trim('-');
        return slug.Length > 80 ? slug[..80].Trim('-') : slug;
    }

    private static void AppendBody(StringBuilder html, IReadOnlyList<string> lines)
    {
        var inList = false;
        foreach (var raw in lines)
        {
            var isListItem = raw.StartsWith("• ", StringComparison.Ordinal);
            var text = isListItem ? raw[2..] : raw;
            if (isListItem)
            {
                if (!inList)
                {
                    html.Append("<ul>");
                    inList = true;
                }

                html.Append("<li>").Append(Encode(text)).Append("</li>");
                continue;
            }

            if (inList)
            {
                html.Append("</ul>");
                inList = false;
            }

            html.Append("<p>").Append(Encode(text)).Append("</p>");
        }

        if (inList)
        {
            html.Append("</ul>");
        }
    }

    /// <summary>
    /// Keeps only the Vietnamese main part of a post: drops the "[English below]" marker and everything
    /// after the first separator line (English translation, repeated contact/footer block).
    /// </summary>
    private static string StripSecondaryParts(string message)
    {
        var text = message.Replace("\r", string.Empty);
        var main = Regex.Split(text, @"\n\s*[-–—_=]{5,}\s*\n")[0];
        main = Regex.Replace(main, @"^\s*\[(english|eng)[^\]]*\]\s*$", string.Empty, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        return main.Trim().Length >= 40 ? main : text;
    }

    private static List<string> CleanLines(string message)
    {
        var result = new List<string>();
        foreach (var rawLine in message.Replace("\r", string.Empty).Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            // Lines that are only hashtags are dropped.
            if (Regex.IsMatch(line, @"^(#[\p{L}\p{N}_]+\s*)+$"))
            {
                continue;
            }

            var startsWithBullet = Regex.IsMatch(line, @"^([\-–•*+>✓✔]|\p{Cs}|\p{So})");
            line = Regex.Replace(line, @"#[\p{L}\p{N}_]+", string.Empty);
            line = Regex.Replace(line, @"[\p{Cs}\p{So}️‍⃣]", " ");
            line = Regex.Replace(line, @"^[\-–•*+>✓✔\s]+", string.Empty);
            line = Regex.Replace(line, @"([!?.])\1+", "$1");
            line = Regex.Replace(line, @"\s{2,}", " ").Trim();
            if (line.Length < 2)
            {
                continue;
            }

            line = char.ToUpperInvariant(line[0]) + line[1..];
            result.Add(startsWithBullet && result.Count > 0 ? "• " + line : line);
        }

        return result;
    }

    private static string BuildTitle(string firstLine, BrandProfile brand)
    {
        var title = StripTrailingPunctuation(firstLine.Replace("• ", string.Empty));
        if (IsMostlyUpper(title))
        {
            title = title.ToLowerInvariant();
            title = char.ToUpperInvariant(title[0]) + title[1..];
        }

        title = Shorten(title, 70);
        if (title.Length < 20)
        {
            title = $"{title} – {brand.Name}";
        }

        return title;
    }

    private static string BuildSectionHeading(string category, BrandProfile brand)
    {
        return category switch
        {
            NewsCategories.KhuyenMaiUuDai => $"Chi tiết ưu đãi tại {brand.Name}",
            NewsCategories.HeThongChiNhanh => $"Thông tin chi nhánh {brand.Name}",
            NewsCategories.SuKienHoatDong => $"Chi tiết sự kiện tại {brand.Name}",
            NewsCategories.NhuongQuyenHopTac => "Thông tin hợp tác nhượng quyền",
            NewsCategories.TinTucThongBao => $"Thông báo từ {brand.Name}",
            _ => $"Món ngon tại {brand.Name}"
        };
    }

    private static bool IsMostlyUpper(string value)
    {
        var letters = value.Where(c => char.IsLetter(c)).ToList();
        return letters.Count >= 8 && letters.Count(c => char.IsUpper(c)) > letters.Count * 0.7;
    }

    private static string Shorten(string value, int max)
    {
        value = value.Trim();
        if (value.Length <= max)
        {
            return value;
        }

        var cut = value[..max];
        var space = cut.LastIndexOf(' ');
        if (space > max / 2)
        {
            cut = cut[..space];
        }

        return StripTrailingPunctuation(cut) + "…";
    }

    private static string StripTrailingPunctuation(string value) => value.Trim().TrimEnd('.', ',', ';', ':', '!', '?', '-', '–', '…').Trim();

    private static string NormalizeForCompare(string value) => value.Trim();

    private static string PlainText(string line) => line.StartsWith("• ", StringComparison.Ordinal) ? line[2..] : line;

    private static bool ContainsAny(string value, params string[] needles) => needles.Any(needle => value.Contains(needle, StringComparison.Ordinal));

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}

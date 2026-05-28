using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace KIGHolding.Models.Content;

public sealed record NewsCategoryDefinition(string Slug, string DisplayName, int Order);

public static class NewsCategories
{
    public const string NhuongQuyenHopTac = "nhuong-quyen-hop-tac";
    public const string HeThongChiNhanh = "he-thong-chi-nhanh";
    public const string SuKienHoatDong = "su-kien-hoat-dong";
    public const string KhuyenMaiUuDai = "khuyen-mai-uu-dai";
    public const string MenuMonMoi = "menu-mon-moi";
    public const string TinTucThongBao = "tin-tuc-thong-bao";

    private static readonly NewsCategoryDefinition[] Definitions =
    [
        new(NhuongQuyenHopTac, "Nhượng quyền – Hợp tác", 1),
        new(HeThongChiNhanh, "Hệ thống chi nhánh", 2),
        new(SuKienHoatDong, "Sự kiện và Hoạt động", 3),
        new(KhuyenMaiUuDai, "Khuyến mãi và Ưu đãi", 4),
        new(MenuMonMoi, "Menu và Món mới", 5),
        new(TinTucThongBao, "Tin tức và Thông báo", 6)
    ];

    private static readonly IReadOnlyDictionary<string, NewsCategoryDefinition> DefinitionsBySlug =
        Definitions.ToDictionary(x => x.Slug, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, string> AliasToSlug =
        CreateAliasLookup();

    private static readonly IReadOnlyDictionary<string, string[]> StorageAliases =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [NhuongQuyenHopTac] = [NhuongQuyenHopTac, "Nhượng quyền – Hợp tác"],
            [HeThongChiNhanh] = [HeThongChiNhanh, "Hệ thống chi nhánh"],
            [SuKienHoatDong] = [SuKienHoatDong, "Sự kiện và Hoạt động", "Sự kiện"],
            [KhuyenMaiUuDai] = [KhuyenMaiUuDai, "Khuyến mãi và Ưu đãi", "Khuyến mãi"],
            [MenuMonMoi] = [MenuMonMoi, "Menu và Món mới", "Món mới", "Ẩm thực", "Câu chuyện ẩm thực"],
            [TinTucThongBao] = [TinTucThongBao, "Tin tức và Thông báo", "Tin tức"]
        };

    public static IReadOnlyList<NewsCategoryDefinition> All => Definitions;

    public static NewsCategoryDefinition Default => DefinitionsBySlug[TinTucThongBao];

    public static bool TryGetBySlug(string? category, out NewsCategoryDefinition? definition)
    {
        var normalizedCategory = NormalizeCategory(category);
        if (string.IsNullOrWhiteSpace(normalizedCategory) ||
            !DefinitionsBySlug.TryGetValue(normalizedCategory, out var matchedDefinition))
        {
            definition = null;
            return false;
        }

        definition = matchedDefinition;
        return true;
    }

    public static bool IsKnownCategory(string? category)
    {
        return !string.IsNullOrWhiteSpace(NormalizeCategory(category));
    }

    public static string? NormalizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        var trimmedCategory = category.Trim();
        if (DefinitionsBySlug.ContainsKey(trimmedCategory))
        {
            return trimmedCategory;
        }

        return AliasToSlug.TryGetValue(trimmedCategory, out var categorySlug)
            ? categorySlug
            : null;
    }

    public static string GetDisplayName(string? category)
    {
        return TryGetBySlug(category, out var definition)
            ? definition!.DisplayName
            : string.IsNullOrWhiteSpace(category) ? string.Empty : category.Trim();
    }

    public static IReadOnlyList<string> GetStorageAliases(string? category)
    {
        var normalizedCategory = NormalizeCategory(category);
        if (string.IsNullOrWhiteSpace(normalizedCategory) ||
            !StorageAliases.TryGetValue(normalizedCategory, out var aliases))
        {
            return string.IsNullOrWhiteSpace(category)
                ? []
                : [category.Trim()];
        }

        return aliases;
    }

    private static IReadOnlyDictionary<string, string> CreateAliasLookup()
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in Definitions)
        {
            AddAlias(aliases, definition.Slug, definition.Slug);
            AddAlias(aliases, definition.DisplayName, definition.Slug);
            AddAlias(aliases, Slugify(definition.DisplayName), definition.Slug);
        }

        AddAlias(aliases, "Tin tức", TinTucThongBao);
        AddAlias(aliases, "tin-tuc", TinTucThongBao);
        AddAlias(aliases, "Khuyến mãi", KhuyenMaiUuDai);
        AddAlias(aliases, "khuyen-mai", KhuyenMaiUuDai);
        AddAlias(aliases, "Món mới", MenuMonMoi);
        AddAlias(aliases, "mon-moi", MenuMonMoi);
        AddAlias(aliases, "Sự kiện", SuKienHoatDong);
        AddAlias(aliases, "su-kien", SuKienHoatDong);
        AddAlias(aliases, "Ẩm thực", MenuMonMoi);
        AddAlias(aliases, "am-thuc", MenuMonMoi);
        AddAlias(aliases, "Câu chuyện ẩm thực", MenuMonMoi);
        AddAlias(aliases, "cau-chuyen-am-thuc", MenuMonMoi);

        return aliases;
    }

    private static void AddAlias(IDictionary<string, string> aliases, string alias, string categorySlug)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return;
        }

        aliases[alias.Trim()] = categorySlug;
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        normalized = builder.ToString().Normalize(NormalizationForm.FormC);
        normalized = normalized.Replace('đ', 'd');
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", string.Empty);
        normalized = Regex.Replace(normalized, @"[\s-]+", "-").Trim('-');

        return normalized;
    }
}

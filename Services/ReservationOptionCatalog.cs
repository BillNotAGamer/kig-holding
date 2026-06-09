namespace KIGHolding.Services;

public static class ReservationOptionCatalog
{
    public const string OtherCode = "other";

    public static IReadOnlyList<ReservationOptionItem> DiningOccasionOptions { get; } =
    [
        new("family", "Gia đình dùng bữa", "가족 식사", "Family dinner"),
        new("friends", "Bạn bè dùng bữa", "친구 식사", "Dinner with friends"),
        new("business", "Đồng nghiệp dùng bữa", "비즈니스 식사", "Business dinner"),
        new("couple", "Cặp đôi dùng bữa", "연인 식사", "Couple dinner"),
        new("birthday", "Tiệc sinh nhật", "생일 파티", "Birthday party"),
        new("anniversary", "Tiệc kỷ niệm", "기념일 식사", "Anniversary dinner"),
        new(OtherCode, "Khác", "기타", "Other")
    ];

    public static ISet<string> AllowedDiningOccasionCodes { get; } =
        new HashSet<string>(DiningOccasionOptions.Select(option => option.Code), StringComparer.Ordinal);

    public static string? NormalizeSingleCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToLowerInvariant();
    }

    public static bool IsAllowedDiningOccasionCode(string? code)
    {
        var normalizedCode = NormalizeSingleCode(code);
        return normalizedCode is not null && AllowedDiningOccasionCodes.Contains(normalizedCode);
    }

    public static IReadOnlyList<string> NormalizeCodes(IEnumerable<string>? codes)
    {
        return codes?
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList()
            ?? [];
    }

    public static string? ToStorageValue(IEnumerable<string>? codes)
    {
        var normalizedCodes = NormalizeCodes(codes);
        return normalizedCodes.Count == 0 ? null : string.Join(';', normalizedCodes);
    }

    public static IReadOnlyList<string> ParseStoredCodes(string? storedCodes)
    {
        if (string.IsNullOrWhiteSpace(storedCodes))
        {
            return [];
        }

        return NormalizeCodes(storedCodes.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    public static bool ContainsCode(IEnumerable<string>? codes, string code)
    {
        if (codes is null)
        {
            return false;
        }

        return codes.Any(value => string.Equals(value?.Trim(), code, StringComparison.OrdinalIgnoreCase));
    }

    public static string FormatSingleCode(string? code, IReadOnlyList<ReservationOptionItem> options)
    {
        var normalizedCode = NormalizeSingleCode(code);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return string.Empty;
        }

        return options.FirstOrDefault(option => option.Code == normalizedCode)?.DisplayLabel ?? string.Empty;
    }

    public static string FormatCodesForDisplay(string? storedCodes, IReadOnlyList<ReservationOptionItem> options)
    {
        return FormatCodesForDisplay(ParseStoredCodes(storedCodes), options);
    }

    public static string FormatCodesForDisplay(IEnumerable<string>? codes, IReadOnlyList<ReservationOptionItem> options)
    {
        var normalizedCodes = NormalizeCodes(codes).ToHashSet(StringComparer.Ordinal);
        if (normalizedCodes.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(", ",
            options
                .Where(option => normalizedCodes.Contains(option.Code))
                .Select(option => option.DisplayLabel));
    }

    public static string FormatDiningOccasionCodesForDisplay(string? storedCodes)
    {
        return FormatCodesForDisplay(storedCodes, DiningOccasionOptions);
    }

    public static string FormatDiningOccasionCode(string? code)
    {
        return FormatSingleCode(code, DiningOccasionOptions);
    }

    public static string FormatDiningOccasionCodesForDisplay(IEnumerable<string>? codes)
    {
        return FormatCodesForDisplay(codes, DiningOccasionOptions);
    }
}

public sealed record ReservationOptionItem(
    string Code,
    string VietnameseLabel,
    string KoreanLabel,
    string EnglishLabel)
{
    public string DisplayLabel => $"{VietnameseLabel} / {KoreanLabel} / {EnglishLabel}";
}

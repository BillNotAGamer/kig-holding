namespace KIGHolding.Services;

public static class ContactSubjectOptions
{
    public const string Feedback = "Feedback";
    public const string Complaint = "Khiếu nại";
    public const string Partnership = "Ngỏ ý hợp tác";

    public static IReadOnlyList<string> Values { get; } =
    [
        Feedback,
        Complaint,
        Partnership
    ];

    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim();
        foreach (var option in Values)
        {
            if (string.Equals(option, candidate, StringComparison.Ordinal))
            {
                normalized = option;
                return true;
            }
        }

        return false;
    }

    public static string? NormalizeOrNull(string? value)
    {
        return TryNormalize(value, out var normalized) ? normalized : null;
    }
}

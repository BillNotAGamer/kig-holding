namespace KIGHolding.Services;

public static class AdminEmailNormalizer
{
    public static string? Normalize(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToUpperInvariant();
    }
}

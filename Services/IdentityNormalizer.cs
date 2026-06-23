using System.Text.RegularExpressions;

namespace KIGHolding.Services;

public static class IdentityNormalizer
{
    private static readonly Regex NonDigits = new(@"[^\d]", RegexOptions.Compiled);
    private static readonly Regex GmailPlusAlias = new(@"\+[^@]*(?=@)", RegexOptions.Compiled);

    /// <summary>
    /// Strips all non-digit characters and normalises Vietnamese country-code prefixes.
    /// "84 909 888 777" → "0909888777", "+84909888777" → "0909888777"
    /// </summary>
    public static string NormalizePhone(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return string.Empty;
        }

        var digits = NonDigits.Replace(phoneNumber.Trim(), "");

        // Convert 84-prefixed numbers (country code) back to local 0-prefix form.
        if (digits.StartsWith("84", StringComparison.Ordinal) && digits.Length >= 11)
        {
            digits = "0" + digits[2..];
        }

        return digits;
    }

    /// <summary>
    /// Lower-cases the address, strips Gmail plus-aliases (+tag), and removes
    /// intra-local-part dots for Gmail accounts (dots are ignored by Google).
    /// "User.Name+alias@Gmail.com" → "username@gmail.com"
    /// Returns empty string when input is null/whitespace.
    /// </summary>
    public static string NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var clean = email.Trim().ToLowerInvariant();

        // Strip plus-alias before @gmail.com or @googlemail.com
        if (clean.EndsWith("@gmail.com", StringComparison.Ordinal)
            || clean.EndsWith("@googlemail.com", StringComparison.Ordinal))
        {
            clean = GmailPlusAlias.Replace(clean, "");

            var atIndex = clean.IndexOf('@');
            if (atIndex > 0)
            {
                var local = clean[..atIndex].Replace(".", "");
                var domain = clean[atIndex..];
                clean = local + domain;
            }
        }

        return clean;
    }

    /// <summary>
    /// Returns the IMemoryCache key used to gate duplicate phone submissions.
    /// </summary>
    public static string PhoneLockKey(string normalizedPhone)
        => $"res_lock:phone:{normalizedPhone}";

    /// <summary>
    /// Returns the IMemoryCache key used to gate duplicate email submissions.
    /// Returns null when there is no email to track.
    /// </summary>
    public static string? EmailLockKey(string normalizedEmail)
        => string.IsNullOrEmpty(normalizedEmail) ? null : $"res_lock:email:{normalizedEmail}";
}

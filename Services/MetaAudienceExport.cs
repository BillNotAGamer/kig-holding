using System.Globalization;
using System.Text;
using KIGHolding.Data;
using KIGHolding.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Services;

/// <summary>
/// Builds the customer list in Meta's value-based custom audience format
/// (email, phone, fn, ln, ct, country, value) from website reservations.
/// One row per phone number; value = total guests booked (proxy for customer value).
/// </summary>
public static class MetaAudienceExport
{
    public static readonly string[] Header = ["email", "phone", "fn", "ln", "ct", "country", "value"];

    public static async Task<IReadOnlyList<string[]>> BuildRowsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var reservations = await dbContext.Reservations
            .AsNoTracking()
            .Where(reservation => !reservation.IsDeleted && reservation.Status != ReservationStatus.Cancelled)
            .Select(reservation => new
            {
                reservation.CustomerName,
                reservation.PhoneNumber,
                reservation.Email,
                reservation.GuestCount,
                reservation.CreatedAt,
                City = reservation.Branch.City
            })
            .ToListAsync(cancellationToken);

        return reservations
            .Select(reservation => new
            {
                Phone = NormalizePhone(reservation.PhoneNumber),
                reservation.CustomerName,
                reservation.Email,
                reservation.GuestCount,
                reservation.CreatedAt,
                reservation.City
            })
            .Where(reservation => reservation.Phone.Length > 0)
            .GroupBy(reservation => reservation.Phone)
            .Select(group =>
            {
                var latest = group.OrderByDescending(item => item.CreatedAt).First();
                var email = group
                    .OrderByDescending(item => item.CreatedAt)
                    .Select(item => (item.Email ?? string.Empty).Trim().ToLowerInvariant())
                    .FirstOrDefault(value => value.Contains('@')) ?? string.Empty;
                var (firstName, lastName) = SplitVietnameseName(latest.CustomerName);

                return new[]
                {
                    email,
                    group.Key,
                    firstName,
                    lastName,
                    NormalizeCity(latest.City),
                    "VN",
                    group.Sum(item => Math.Max(item.GuestCount, 0)).ToString(CultureInfo.InvariantCulture)
                };
            })
            .OrderBy(row => row[1], StringComparer.Ordinal)
            .ToList();
    }

    public static string ToCsv(IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', Header));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', row.Select(EscapeCsv)));
        }

        return builder.ToString();
    }

    public static string NormalizePhone(string? phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return string.Empty;
        }

        if (digits.StartsWith("84", StringComparison.Ordinal) && digits.Length >= 11)
        {
            return "+" + digits;
        }

        if (digits.StartsWith('0') && digits.Length >= 10)
        {
            return "+84" + digits[1..];
        }

        return digits.Length >= 9 ? "+84" + digits : string.Empty;
    }

    public static (string FirstName, string LastName) SplitVietnameseName(string? fullName)
    {
        var parts = (fullName ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length switch
        {
            0 => (string.Empty, string.Empty),
            1 => (parts[0], string.Empty),
            // Vietnamese order: family name first, given name last.
            _ => (parts[^1], parts[0])
        };
    }

    private static string NormalizeCity(string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return string.Empty;
        }

        var decomposed = city.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && char.IsLetter(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny([',', '"', '\n', '\r']) < 0)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}

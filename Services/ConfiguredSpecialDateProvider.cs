using System.Globalization;
using Microsoft.Extensions.Options;

namespace KIGHolding.Services;

public class ConfiguredSpecialDateProvider : ISpecialDateProvider
{
    private readonly HashSet<DateOnly> _specialDates;

    public ConfiguredSpecialDateProvider(
        IOptions<ReservationPolicyOptions> options,
        ILogger<ConfiguredSpecialDateProvider> logger)
    {
        _specialDates = new HashSet<DateOnly>();

        foreach (var value in options.Value.SpecialDates)
        {
            if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ||
                DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                _specialDates.Add(parsed);
            }
            else
            {
                logger.LogWarning("Unable to parse reservation special date '{SpecialDate}'.", value);
            }
        }
    }

    public bool IsSpecialDate(DateOnly date)
    {
        return _specialDates.Contains(date);
    }

    public IReadOnlyCollection<DateOnly> GetSpecialDates()
    {
        return _specialDates;
    }
}

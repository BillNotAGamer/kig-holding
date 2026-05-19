namespace KIGHolding.Services;

public interface ISpecialDateProvider
{
    bool IsSpecialDate(DateOnly date);
    IReadOnlyCollection<DateOnly> GetSpecialDates();
}

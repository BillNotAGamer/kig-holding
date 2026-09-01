namespace KIGHolding.Services;

public static class VietnamClock
{
    private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

    public static DateTimeOffset GetVietnamNow()
    {
        return GetVietnamNow(TimeProvider.System);
    }

    public static DateTimeOffset GetVietnamNow(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        return TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), VietnamTimeZone);
    }

    public static DateOnly GetVietnamToday()
    {
        return GetVietnamToday(TimeProvider.System);
    }

    public static DateOnly GetVietnamToday(TimeProvider timeProvider)
    {
        return DateOnly.FromDateTime(GetVietnamNow(timeProvider).DateTime);
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }
}

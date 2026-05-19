namespace KIGHolding.Services;

public class ReservationPolicyOptions
{
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    public string WindowsTimeZoneId { get; set; } = "SE Asia Standard Time";
    public string BookingStartTime { get; set; } = "10:40";
    public string BookingEndTime { get; set; } = "21:30";
    public string BranchBlackoutStartTime { get; set; } = "14:00";
    public string BranchBlackoutEndTime { get; set; } = "16:00";
    public int WeekdayMinimumGuestCount { get; set; } = 2;
    public int AbsoluteMinimumGuestCount { get; set; } = 1;
    public int MaximumGuestCount { get; set; } = 100;
    public List<string> BlackoutDistricts { get; set; } = [];
    public List<string> SpecialDates { get; set; } = [];
}

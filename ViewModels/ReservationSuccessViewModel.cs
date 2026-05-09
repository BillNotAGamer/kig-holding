namespace KIGHolding.ViewModels;

public class ReservationSuccessViewModel
{
    public Guid ReservationId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public DateOnly ReservationDate { get; set; }
    public TimeOnly ReservationTime { get; set; }
    public int GuestCount { get; set; }
    public string Hotline { get; set; } = "0909 888 777";
}

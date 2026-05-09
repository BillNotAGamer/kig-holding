namespace KIGHolding.Services;

public class ReservationCreateRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Guid BranchId { get; set; }
    public int GuestCount { get; set; }
    public DateOnly ReservationDate { get; set; }
    public TimeOnly ReservationTime { get; set; }
    public string? Note { get; set; }
}

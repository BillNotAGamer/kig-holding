using KIGHolding.Models.Entities;

namespace KIGHolding.Services;

public class ReservationPolicyRequest
{
    public Branch? Branch { get; init; }
    public Guid BranchId { get; init; }
    public string? PhoneNumber { get; init; }
    public int GuestCount { get; init; }
    public DateOnly ReservationDate { get; init; }
    public TimeOnly ReservationTime { get; init; }
}

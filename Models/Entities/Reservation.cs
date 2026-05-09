using KIGHolding.Models.Enums;

namespace KIGHolding.Models.Entities;

public class Reservation : IUpdatedAtEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int GuestCount { get; set; }
    public DateOnly ReservationDate { get; set; }
    public TimeOnly ReservationTime { get; set; }
    public string? Note { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public ReservationSource Source { get; set; } = ReservationSource.Website;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Branch Branch { get; set; } = null!;
}

using KIGHolding.Models.Enums;

namespace KIGHolding.Models.Entities;

public sealed class BranchReservationDatePolicy : IUpdatedAtEntity
{
    public Guid BranchId { get; set; }
    public DateOnly Date { get; set; }
    public BranchReservationRuleType RuleType { get; set; }
    public int? MinGuests { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Branch Branch { get; set; } = null!;
}

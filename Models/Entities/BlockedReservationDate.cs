namespace KIGHolding.Models.Entities;

public sealed class BlockedReservationDate : ICreatedAtEntity
{
    public DateOnly Date { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

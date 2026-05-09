namespace KIGHolding.Models.Entities;

public interface IUpdatedAtEntity : ICreatedAtEntity
{
    DateTimeOffset UpdatedAt { get; set; }
}

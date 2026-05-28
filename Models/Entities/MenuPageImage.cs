namespace KIGHolding.Models.Entities;

public class MenuPageImage : IUpdatedAtEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MenuGroupId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public MenuGroup MenuGroup { get; set; } = null!;
}

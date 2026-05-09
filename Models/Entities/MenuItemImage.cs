namespace KIGHolding.Models.Entities;

public class MenuItemImage : ICreatedAtEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MenuItemId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public MenuItem MenuItem { get; set; } = null!;
}

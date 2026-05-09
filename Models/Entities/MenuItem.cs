namespace KIGHolding.Models.Entities;

public class MenuItem : IUpdatedAtEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? KoreanName { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int SpicyLevel { get; set; }
    public string? ServingSize { get; set; }
    public int? Calories { get; set; }
    public bool IsSignature { get; set; }
    public bool IsBestSeller { get; set; }
    public bool IsNew { get; set; }
    public bool IsCombo { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public MenuCategory Category { get; set; } = null!;
    public ICollection<MenuItemImage> Images { get; } = new List<MenuItemImage>();
}

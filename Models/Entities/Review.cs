namespace KIGHolding.Models.Entities;

public class Review : ICreatedAtEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public bool IsVisible { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Branch? Branch { get; set; }
}

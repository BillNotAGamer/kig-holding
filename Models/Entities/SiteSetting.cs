namespace KIGHolding.Models.Entities;

public class SiteSetting : IUpdatedAtEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SiteName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string Slogan { get; set; } = string.Empty;
    public string Hotline { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FacebookUrl { get; set; } = string.Empty;
    public string ZaloUrl { get; set; } = string.Empty;
    public string TiktokUrl { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string GoogleMapUrl { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string FaviconUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

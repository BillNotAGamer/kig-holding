using KIGHolding.Models.Entities;

namespace KIGHolding.ViewModels.Shared;

public class FoodCardViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? KoreanName { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Url { get; set; } = "#";
    public string ImageUrl { get; set; } = "/images/placeholders/food-card.webp";
    public string ShortDescription { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int SpicyLevel { get; set; }
    public bool IsSignature { get; set; }
    public bool IsBestSeller { get; set; }
    public bool IsNew { get; set; }
    public bool IsCombo { get; set; }

    public static FoodCardViewModel FromMenuItem(MenuItem menuItem)
    {
        return new FoodCardViewModel
        {
            Name = menuItem.Name,
            KoreanName = menuItem.KoreanName,
            Slug = menuItem.Slug,
            Url = $"/thuc-don/{menuItem.Slug}",
            ImageUrl = string.IsNullOrWhiteSpace(menuItem.ThumbnailUrl) ? "/images/placeholders/food-card.webp" : menuItem.ThumbnailUrl,
            ShortDescription = menuItem.ShortDescription,
            CategoryName = menuItem.Category?.Name ?? string.Empty,
            Price = menuItem.Price,
            OriginalPrice = menuItem.OriginalPrice,
            SpicyLevel = menuItem.SpicyLevel,
            IsSignature = menuItem.IsSignature,
            IsBestSeller = menuItem.IsBestSeller,
            IsNew = menuItem.IsNew,
            IsCombo = menuItem.IsCombo
        };
    }
}

using KIGHolding.Models.Entities;
using KIGHolding.ViewModels.Shared;

namespace KIGHolding.ViewModels;

public class MenuDetailViewModel
{
    public MenuItem MenuItem { get; set; } = null!;
    public MenuCategory Category { get; set; } = null!;
    public IReadOnlyList<MenuGalleryImageViewModel> GalleryImages { get; set; } = [];
    public IReadOnlyList<FoodCardViewModel> RelatedItems { get; set; } = [];
    public string SeoTitle { get; set; } = string.Empty;
    public string SeoDescription { get; set; } = string.Empty;
}

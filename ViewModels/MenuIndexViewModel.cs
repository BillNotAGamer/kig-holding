using KIGHolding.ViewModels.Shared;

namespace KIGHolding.ViewModels;

public class MenuIndexViewModel
{
    public IReadOnlyList<MenuCategoryFilterViewModel> Categories { get; set; } = [];
    public string? SelectedCategorySlug { get; set; }
    public IReadOnlyList<FoodCardViewModel> MenuItems { get; set; } = [];
    public FoodCardViewModel? FeaturedItem { get; set; }
    public string PageTitle { get; set; } = "Thực đơn Truyền Thuyết Champong";
    public string SeoTitle { get; set; } = "Thực đơn";
    public string SeoDescription { get; set; } = "Khám phá mì Champong hải sản cay nồng, Korean BBQ, lẩu, combo, panchan và đồ uống tại Truyền Thuyết Champong.";
}

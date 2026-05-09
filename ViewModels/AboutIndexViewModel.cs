using KIGHolding.Models.Entities;
using KIGHolding.ViewModels.Shared;

namespace KIGHolding.ViewModels;

public class AboutIndexViewModel
{
    public SiteSetting? SiteSetting { get; set; }
    public IReadOnlyList<BranchCardViewModel> FeaturedBranches { get; set; } = [];
    public IReadOnlyList<FoodCardViewModel> SignatureMenuItems { get; set; } = [];
    public string SeoTitle { get; set; } = "Giới thiệu";
    public string SeoDescription { get; set; } = "Câu chuyện Truyền Thuyết Champong: mì cay Hàn Quốc, nước dùng đậm vị, BBQ nóng lửa và trải nghiệm ăn uống ấm cúng.";

    public string BrandName => string.IsNullOrWhiteSpace(SiteSetting?.BrandName) ? "Truyền Thuyết Champong" : SiteSetting!.BrandName;
    public string Slogan => string.IsNullOrWhiteSpace(SiteSetting?.Slogan) ? "Chuẩn vị Hàn Quốc trong từng tô mì cay nóng." : SiteSetting!.Slogan;
}

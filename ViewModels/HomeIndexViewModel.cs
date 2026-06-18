using KIGHolding.Models.Entities;
using KIGHolding.ViewModels.Shared;

namespace KIGHolding.ViewModels;

public class HomeIndexViewModel
{
    public SiteSetting? SiteSetting { get; set; }
    public IReadOnlyList<PostCardViewModel> LatestPosts { get; set; } = [];

    public string BrandName => string.IsNullOrWhiteSpace(SiteSetting?.BrandName) ? "Truyền Thuyết Champong" : SiteSetting!.BrandName;
    public string Hotline => string.IsNullOrWhiteSpace(SiteSetting?.Hotline) ? "0909 888 777" : SiteSetting!.Hotline;
}

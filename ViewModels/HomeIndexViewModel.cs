using KIGHolding.Models.Entities;
using KIGHolding.ViewModels.Shared;

namespace KIGHolding.ViewModels;

public class HomeIndexViewModel
{
    public SiteSetting? SiteSetting { get; set; }
    public IReadOnlyList<FoodCardViewModel> FeaturedMenuItems { get; set; } = [];
    public IReadOnlyList<FoodCardViewModel> SignatureMenuItems { get; set; } = [];
    public IReadOnlyList<BranchCardViewModel> Branches { get; set; } = [];
    public IReadOnlyList<HomeReviewViewModel> Reviews { get; set; } = [];
    public IReadOnlyList<PostCardViewModel> LatestPosts { get; set; } = [];
    public BookingMiniFormViewModel ReservationForm { get; set; } = new();

    public string BrandName => string.IsNullOrWhiteSpace(SiteSetting?.BrandName) ? "Truyền Thuyết Champong" : SiteSetting!.BrandName;
    public string Hotline => string.IsNullOrWhiteSpace(SiteSetting?.Hotline) ? "0909 888 777" : SiteSetting!.Hotline;
    public string OpeningHours => Branches.FirstOrDefault()?.OpeningHours ?? "10:00 - 22:30";
    public string BranchHint => Branches.Count > 0
        ? $"{Branches.Count} chi nhánh tại TP.HCM và Hà Nội"
        : "Nhiều chi nhánh trung tâm";
}

namespace KIGHolding.ViewModels.Shared;

public class FooterViewModel
{
    public string BrandName { get; set; } = "Truyền Thuyết Champong";
    public string Description { get; set; } = "Mì cay Hàn Quốc, BBQ nóng lửa và không gian tối hiện đại cho những bữa ăn đậm vị.";
    public string LogoUrl { get; set; } = string.Empty;
    public string Hotline { get; set; } = "0909 888 777";
    public string Email { get; set; } = "hello@truyenthuyetchampong.vn";
    public string FacebookUrl { get; set; } = "#";
    public string ZaloUrl { get; set; } = "#";
    public string TiktokUrl { get; set; } = "#";
    public string OpeningHours { get; set; } = "10:00 - 22:30";
    public IReadOnlyList<NavItemViewModel> QuickLinks { get; set; } = [];
    public IReadOnlyList<FooterBranchViewModel> Branches { get; set; } = [];
}

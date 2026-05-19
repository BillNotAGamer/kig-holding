namespace KIGHolding.ViewModels.Shared;

public class FooterViewModel
{
    public string BrandName { get; set; } = "Truyền Thuyết Champong";
    public string Description { get; set; } = "Mì cay Hàn Quốc, BBQ nóng lửa và không gian tối hiện đại cho những bữa ăn đậm vị.";
    public string LogoUrl { get; set; } = string.Empty;
    public string FooterLogoUrl { get; set; } = "/images/general/kig-no-bg-logo.png";
    public string Address { get; set; } = string.Empty;
    public string GoogleMapUrl { get; set; } = string.Empty;
    public string Hotline { get; set; } = "0909 888 777";
    public string Email { get; set; } = "hello@truyenthuyetchampong.vn";
    public string FacebookUrl { get; set; } = "#";
    public string ZaloUrl { get; set; } = "#";
    public string TiktokUrl { get; set; } = "#";
    public string CompanyLegalName { get; set; } = "CÔNG TY TNHH KIG HOLDING VIỆT NAM";
    public string BusinessRegistrationNumber { get; set; } = "Mã số DN: 0123456789";
    public string QrPlaceholderText { get; set; } = "QR";
    public string AppStorePlaceholderText { get; set; } = "App Store";
    public string GooglePlayPlaceholderText { get; set; } = "Google Play";
    public string OpeningHours { get; set; } = "10:00 - 22:30";
    public IReadOnlyList<NavItemViewModel> QuickLinks { get; set; } = [];
    public IReadOnlyList<FooterBranchViewModel> Branches { get; set; } = [];
}

namespace KIGHolding.ViewModels.Shared;

public class HeaderViewModel
{
    public string BrandName { get; set; } = "Truyền Thuyết Champong";
    public string LogoUrl { get; set; } = "/images/general/kig-no-bg-logo.png";
    public string Hotline { get; set; } = "0922 055 755";
    public string ReservationUrl { get; set; } = "/dat-ban";
    public IReadOnlyList<NavItemViewModel> NavItems { get; set; } = [];
}

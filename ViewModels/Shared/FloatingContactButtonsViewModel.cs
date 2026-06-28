namespace KIGHolding.ViewModels.Shared;

public class FloatingBrandContactLinkViewModel
{
    public string Name { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string FacebookUrl { get; set; } = string.Empty;
    public string ZaloUrl { get; set; } = string.Empty;
    public string ToggleOpenLabel { get; set; } = string.Empty;
    public string ToggleCloseLabel { get; set; } = string.Empty;
    public string LogoClass { get; set; } = string.Empty;
}

public class FloatingContactButtonsViewModel
{
    public string Hotline { get; set; } = "0922 055 755";
    public string FacebookUrl { get; set; } = "https://www.facebook.com/champong.official";
    public string ZaloUrl { get; set; } = "https://oa.zalo.me/3191309080595223416";
    public string ReservationUrl { get; set; } = "/dat-ban";
    public string BrandLogoUrl { get; set; } = "/images/general/kig-no-bg-logo.png";

    public IReadOnlyList<FloatingBrandContactLinkViewModel> Brands { get; set; } = [];

    public string PhoneHref => $"tel:{Hotline.Replace(" ", string.Empty)}";
}

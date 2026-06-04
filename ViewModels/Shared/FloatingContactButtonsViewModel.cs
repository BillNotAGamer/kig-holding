namespace KIGHolding.ViewModels.Shared;

public class FloatingContactButtonsViewModel
{
    public string Hotline { get; set; } = "0909 888 777";
    public string FacebookUrl { get; set; } = "#";
    public string ZaloUrl { get; set; } = "https://oa.zalo.me/3191309080595223416";
    public string ReservationUrl { get; set; } = "/dat-ban";

    public string PhoneHref => $"tel:{Hotline.Replace(" ", string.Empty)}";
}

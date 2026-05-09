using KIGHolding.ViewModels.Shared;

namespace KIGHolding.ViewModels;

public class BranchIndexViewModel
{
    public IReadOnlyList<BranchCardViewModel> Branches { get; set; } = [];
    public IReadOnlyList<string> Cities { get; set; } = [];
    public string? SelectedCity { get; set; }
    public string? MainMapUrl { get; set; }
    public string SeoTitle { get; set; } = "Hệ thống chi nhánh";
    public string SeoDescription { get; set; } = "Tìm chi nhánh Truyền Thuyết Champong gần bạn nhất và đặt bàn trước cho bữa ăn Hàn Quốc cay nóng.";

    public bool ShouldShowCityFilter => Cities.Count > 1;
}

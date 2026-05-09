using KIGHolding.ViewModels.Shared;

namespace KIGHolding.ViewModels;

public class NewsIndexViewModel
{
    public IReadOnlyList<NewsCategoryViewModel> Categories { get; set; } = [];
    public string? SelectedCategory { get; set; }
    public PostCardViewModel? FeaturedPost { get; set; }
    public IReadOnlyList<PostCardViewModel> Posts { get; set; } = [];
    public string SeoTitle { get; set; } = "Tin tức & ưu đãi";
    public string SeoDescription { get; set; } = "Cập nhật món mới, khuyến mãi và câu chuyện ẩm thực Hàn Quốc tại Truyền Thuyết Champong.";
}

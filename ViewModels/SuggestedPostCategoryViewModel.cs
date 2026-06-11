namespace KIGHolding.ViewModels;

public class SuggestedPostCategoryViewModel
{
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int PostCount { get; set; }
    public DateTimeOffset LatestPostDate { get; set; }
    public string? LatestPostTitle { get; set; }
    public string? LatestPostSlug { get; set; }
    public int Order { get; set; }
}

public class SuggestedPostCategoriesViewModel
{
    public string Mode { get; set; } = "Home";
    public IReadOnlyList<SuggestedPostCategoryViewModel> Items { get; set; } = [];

    public bool IsNewsMode => string.Equals(Mode, "News", StringComparison.OrdinalIgnoreCase);
    public string Heading => IsNewsMode ? "Đang được quan tâm" : "Chủ đề đang nổi bật";
    public string Eyebrow => IsNewsMode ? "Gợi ý chuyên mục" : "Gợi ý đọc tiếp";
}

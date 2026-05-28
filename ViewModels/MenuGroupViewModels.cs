namespace KIGHolding.ViewModels;

public class MenuGroupCardViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public int PageImageCount { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class MenuGroupLandingViewModel
{
    public IReadOnlyList<MenuGroupCardViewModel> Groups { get; set; } = [];
}

public class MenuPageImageViewModel
{
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public int PageNumber { get; set; }
}

public class MenuGroupDetailViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? FirstImageUrl { get; set; }
    public string BackUrl { get; set; } = "/thuc-don";
    public string GroupUrl { get; set; } = string.Empty;
    public int TotalPages { get; set; }
    public bool HasImages { get; set; }
    public IReadOnlyList<MenuPageImageViewModel> Images { get; set; } = [];
}

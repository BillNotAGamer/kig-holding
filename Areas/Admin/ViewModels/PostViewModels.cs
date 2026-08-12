using System.ComponentModel.DataAnnotations;
using KIGHolding.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KIGHolding.Areas.Admin.ViewModels;

public static class PostEditorLimits
{
    public const int ContentMaxLength = 100_000;
    public const int FocusKeywordMaxLength = 120;
    public const int CanonicalUrlMaxLength = 500;
    public const int OgTitleMaxLength = 180;
    public const int OgDescriptionMaxLength = 320;
    public const int OgImageUrlMaxLength = 500;
}

public interface IPostContentEditorViewModel
{
    string Content { get; set; }
    PostContentMode ContentMode { get; set; }
}

public interface IPostSeoEditorViewModel
{
    string? SeoTitle { get; set; }
    string? SeoDescription { get; set; }
    string? FocusKeyword { get; set; }
    string? CanonicalUrl { get; set; }
    string? OgTitle { get; set; }
    string? OgDescription { get; set; }
    string? OgImageUrl { get; set; }
    bool RobotsIndex { get; set; }
    bool RobotsFollow { get; set; }
}

public class PostIndexViewModel
{
    public IReadOnlyList<PostListItemViewModel> Posts { get; set; } = [];
    public IReadOnlyList<PostFilterOptionViewModel> Categories { get; set; } = [];
    public IReadOnlyList<PostFilterOptionViewModel> StatusOptions { get; set; } = [];
    public string? SearchQuery { get; set; }
    public string? SelectedCategory { get; set; }
    public string? SelectedStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; } = 1;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public int FirstItemIndex => TotalItems == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastItemIndex => TotalItems == 0 ? 0 : Math.Min(Page * PageSize, TotalItems);
}

public class PostListItemViewModel
{
    public Guid Id { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CategoryDisplayName { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string Excerpt { get; set; } = string.Empty;
    public string PublicUrl => string.IsNullOrWhiteSpace(Slug) ? string.Empty : $"/tin-tuc/{Slug}";
}

public class PostFilterOptionViewModel
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class PostCreateViewModel : IPostContentEditorViewModel, IPostSeoEditorViewModel
{
    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = [];

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(220)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [StringLength(220)]
    [Display(Name = "Slug")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [StringLength(80)]
    [Display(Name = "Danh mục")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập trích dẫn.")]
    [StringLength(600)]
    [Display(Name = "Excerpt")]
    public string Excerpt { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nội dung.")]
    [Display(Name = "Nội dung")]
    [StringLength(PostEditorLimits.ContentMaxLength)]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Che do noi dung")]
    public PostContentMode ContentMode { get; set; } = PostContentMode.Visual;

    [Display(Name = "Đăng công khai")]
    public bool IsPublished { get; set; }

    [Display(Name = "Ngày đăng")]
    public DateTime? PublishedAt { get; set; }

    [StringLength(180)]
    [Display(Name = "SEO Title")]
    public string? SeoTitle { get; set; }

    [StringLength(320)]
    [Display(Name = "SEO Description")]
    public string? SeoDescription { get; set; }

    [StringLength(PostEditorLimits.FocusKeywordMaxLength)]
    [Display(Name = "Focus Keyword")]
    public string? FocusKeyword { get; set; }

    [StringLength(PostEditorLimits.CanonicalUrlMaxLength)]
    [Display(Name = "Canonical URL")]
    public string? CanonicalUrl { get; set; }

    [StringLength(PostEditorLimits.OgTitleMaxLength)]
    [Display(Name = "OG Title")]
    public string? OgTitle { get; set; }

    [StringLength(PostEditorLimits.OgDescriptionMaxLength)]
    [Display(Name = "OG Description")]
    public string? OgDescription { get; set; }

    [StringLength(PostEditorLimits.OgImageUrlMaxLength)]
    [Display(Name = "OG Image URL")]
    public string? OgImageUrl { get; set; }

    [Display(Name = "Robots Index")]
    public bool RobotsIndex { get; set; } = true;

    [Display(Name = "Robots Follow")]
    public bool RobotsFollow { get; set; } = true;

    [Display(Name = "Ảnh thumbnail")]
    public IFormFile? ThumbnailFile { get; set; }
}

public class PostEditViewModel : IPostContentEditorViewModel, IPostSeoEditorViewModel
{
    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = [];

    public Guid Id { get; set; }
    public string? ExistingThumbnailUrl { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(220)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [StringLength(220)]
    [Display(Name = "Slug")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [StringLength(80)]
    [Display(Name = "Danh mục")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập trích dẫn.")]
    [StringLength(600)]
    [Display(Name = "Excerpt")]
    public string Excerpt { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nội dung.")]
    [Display(Name = "Nội dung")]
    [StringLength(PostEditorLimits.ContentMaxLength)]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Che do noi dung")]
    public PostContentMode ContentMode { get; set; } = PostContentMode.Visual;

    [Display(Name = "Đăng công khai")]
    public bool IsPublished { get; set; }

    [Display(Name = "Ngày đăng")]
    public DateTime? PublishedAt { get; set; }

    [StringLength(180)]
    [Display(Name = "SEO Title")]
    public string? SeoTitle { get; set; }

    [StringLength(320)]
    [Display(Name = "SEO Description")]
    public string? SeoDescription { get; set; }

    [StringLength(PostEditorLimits.FocusKeywordMaxLength)]
    [Display(Name = "Focus Keyword")]
    public string? FocusKeyword { get; set; }

    [StringLength(PostEditorLimits.CanonicalUrlMaxLength)]
    [Display(Name = "Canonical URL")]
    public string? CanonicalUrl { get; set; }

    [StringLength(PostEditorLimits.OgTitleMaxLength)]
    [Display(Name = "OG Title")]
    public string? OgTitle { get; set; }

    [StringLength(PostEditorLimits.OgDescriptionMaxLength)]
    [Display(Name = "OG Description")]
    public string? OgDescription { get; set; }

    [StringLength(PostEditorLimits.OgImageUrlMaxLength)]
    [Display(Name = "OG Image URL")]
    public string? OgImageUrl { get; set; }

    [Display(Name = "Robots Index")]
    public bool RobotsIndex { get; set; } = true;

    [Display(Name = "Robots Follow")]
    public bool RobotsFollow { get; set; } = true;

    [Display(Name = "Ảnh thumbnail")]
    public IFormFile? ThumbnailFile { get; set; }
}

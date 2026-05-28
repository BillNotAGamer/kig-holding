using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KIGHolding.Areas.Admin.ViewModels;

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
    public int TotalPages { get; set; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => TotalPages > 0 && Page < TotalPages;
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

public class PostCreateViewModel
{
    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = [];

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(220)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập slug.")]
    [StringLength(220)]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

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
    public string Content { get; set; } = string.Empty;

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

    [Display(Name = "Ảnh thumbnail")]
    public IFormFile? ThumbnailFile { get; set; }
}

public class PostEditViewModel
{
    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = [];

    public Guid Id { get; set; }
    public string? ExistingThumbnailUrl { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(220)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập slug.")]
    [StringLength(220)]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

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
    public string Content { get; set; } = string.Empty;

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

    [Display(Name = "Ảnh thumbnail")]
    public IFormFile? ThumbnailFile { get; set; }
}

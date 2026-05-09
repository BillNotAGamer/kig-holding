using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KIGHolding.Areas.Admin.ViewModels;

public class PostIndexViewModel
{
    public IReadOnlyList<PostListItemViewModel> Posts { get; set; } = [];
}

public class PostListItemViewModel
{
    public Guid Id { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
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

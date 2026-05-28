using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KIGHolding.Areas.Admin.ViewModels;

public class MenuGroupListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }
    public int PageImageCount { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class MenuGroupIndexViewModel
{
    public IReadOnlyList<MenuGroupListItemViewModel> Groups { get; set; } = [];
    public string? SearchQuery { get; set; }
    public string? SelectedStatus { get; set; }
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
    public int TotalCount { get; set; }
}

public class MenuGroupFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên nhóm menu.")]
    [StringLength(160)]
    [Display(Name = "Tên nhóm menu")]
    public string Name { get; set; } = string.Empty;

    [StringLength(180)]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mô tả ngắn.")]
    [StringLength(300)]
    [Display(Name = "Mô tả ngắn")]
    public string ShortDescription { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Mô tả chi tiết")]
    public string? Description { get; set; }

    [StringLength(500)]
    [Display(Name = "URL ảnh bìa")]
    public string? CoverImageUrl { get; set; }

    [Display(Name = "Ảnh bìa")]
    public IFormFile? CoverImageFile { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị không hợp lệ.")]
    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hiển thị công khai")]
    public bool IsPublished { get; set; } = true;
}

public class MenuPageImageListItemViewModel
{
    public Guid Id { get; set; }
    public Guid MenuGroupId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class MenuGroupImagesViewModel
{
    public Guid MenuGroupId { get; set; }
    public string MenuGroupName { get; set; } = string.Empty;
    public string MenuGroupSlug { get; set; } = string.Empty;
    public IReadOnlyList<MenuPageImageListItemViewModel> Images { get; set; } = [];
    public MenuPageImageUploadViewModel Upload { get; set; } = new();
}

public class MenuPageImageUploadViewModel
{
    [Required]
    public Guid MenuGroupId { get; set; }

    [Display(Name = "Ảnh menu")]
    public List<IFormFile> ImageFiles { get; set; } = [];

    [StringLength(180)]
    [Display(Name = "Alt text")]
    public string? AltText { get; set; }

    [Display(Name = "Hiển thị công khai")]
    public bool IsPublished { get; set; } = true;
}

public class MenuPageImageUpdateViewModel
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid MenuGroupId { get; set; }

    [StringLength(180)]
    [Display(Name = "Alt text")]
    public string? AltText { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị không hợp lệ.")]
    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hiển thị công khai")]
    public bool IsPublished { get; set; } = true;
}

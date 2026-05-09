using System.ComponentModel.DataAnnotations;

namespace KIGHolding.Areas.Admin.ViewModels;

public class MenuCategoryIndexViewModel
{
    public IReadOnlyList<MenuCategoryListItemViewModel> Categories { get; set; } = [];
}

public class MenuCategoryListItemViewModel
{
    public Guid Id { get; set; }
    public int DisplayOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public bool IsActive { get; set; }
}

public class MenuCategoryCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [StringLength(160)]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = string.Empty;

    [StringLength(180)]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị không hợp lệ.")]
    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hiển thị công khai")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Ảnh danh mục")]
    public IFormFile? ThumbnailFile { get; set; }
}

public class MenuCategoryEditViewModel : MenuCategoryCreateViewModel
{
    public Guid Id { get; set; }
    public string? ExistingThumbnailUrl { get; set; }
}

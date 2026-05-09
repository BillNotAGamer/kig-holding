using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KIGHolding.Areas.Admin.ViewModels;

public class MenuItemIndexViewModel
{
    public Guid? CategoryFilter { get; set; }
    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = [];
    public IReadOnlyList<MenuItemListItemViewModel> Items { get; set; } = [];
}

public class MenuItemListItemViewModel
{
    public Guid Id { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}

public class MenuItemCreateViewModel
{
    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = [];

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Display(Name = "Danh mục")]
    public Guid? CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên món.")]
    [StringLength(180)]
    [Display(Name = "Tên món")]
    public string Name { get; set; } = string.Empty;

    [StringLength(180)]
    [Display(Name = "Tên tiếng Hàn")]
    public string? KoreanName { get; set; }

    [StringLength(180)]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập giá.")]
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Giá không hợp lệ.")]
    [Display(Name = "Giá")]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Giá gốc không hợp lệ.")]
    [Display(Name = "Giá gốc")]
    public decimal? OriginalPrice { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mô tả ngắn.")]
    [StringLength(500)]
    [Display(Name = "Mô tả ngắn")]
    public string ShortDescription { get; set; } = string.Empty;

    [StringLength(4000)]
    [Display(Name = "Mô tả chi tiết")]
    public string? Description { get; set; }

    [Range(0, 5, ErrorMessage = "Mức cay phải từ 0 đến 5.")]
    [Display(Name = "Mức cay")]
    public int SpicyLevel { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Calories không hợp lệ.")]
    [Display(Name = "Calories")]
    public int? Calories { get; set; }

    [Display(Name = "Món signature")]
    public bool IsSignature { get; set; }

    [Display(Name = "Best seller")]
    public bool IsBestSeller { get; set; }

    [Display(Name = "Món mới")]
    public bool IsNew { get; set; }

    [Display(Name = "Hiển thị công khai")]
    public bool IsAvailable { get; set; } = true;

    [Display(Name = "Món combo")]
    public bool IsCombo { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị không hợp lệ.")]
    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Thumbnail")]
    [Required(ErrorMessage = "Vui lòng tải lên ảnh thumbnail.")]
    public IFormFile? ThumbnailFile { get; set; }
}

public class MenuItemEditViewModel
{
    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = [];

    public Guid Id { get; set; }
    public string? ExistingThumbnailUrl { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Display(Name = "Danh mục")]
    public Guid? CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên món.")]
    [StringLength(180)]
    [Display(Name = "Tên món")]
    public string Name { get; set; } = string.Empty;

    [StringLength(180)]
    [Display(Name = "Tên tiếng Hàn")]
    public string? KoreanName { get; set; }

    [StringLength(180)]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập giá.")]
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Giá không hợp lệ.")]
    [Display(Name = "Giá")]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Giá gốc không hợp lệ.")]
    [Display(Name = "Giá gốc")]
    public decimal? OriginalPrice { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mô tả ngắn.")]
    [StringLength(500)]
    [Display(Name = "Mô tả ngắn")]
    public string ShortDescription { get; set; } = string.Empty;

    [StringLength(4000)]
    [Display(Name = "Mô tả chi tiết")]
    public string? Description { get; set; }

    [Range(0, 5, ErrorMessage = "Mức cay phải từ 0 đến 5.")]
    [Display(Name = "Mức cay")]
    public int SpicyLevel { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Calories không hợp lệ.")]
    [Display(Name = "Calories")]
    public int? Calories { get; set; }

    [Display(Name = "Món signature")]
    public bool IsSignature { get; set; }

    [Display(Name = "Best seller")]
    public bool IsBestSeller { get; set; }

    [Display(Name = "Món mới")]
    public bool IsNew { get; set; }

    [Display(Name = "Hiển thị công khai")]
    public bool IsAvailable { get; set; } = true;

    [Display(Name = "Món combo")]
    public bool IsCombo { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị không hợp lệ.")]
    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Thumbnail")]
    public IFormFile? ThumbnailFile { get; set; }
}

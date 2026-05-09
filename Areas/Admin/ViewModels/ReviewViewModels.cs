using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KIGHolding.Areas.Admin.ViewModels;

public class ReviewIndexViewModel
{
    public IReadOnlyList<ReviewListItemViewModel> Reviews { get; set; } = [];
}

public class ReviewListItemViewModel
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public bool IsVisible { get; set; }
    public int DisplayOrder { get; set; }
}

public class ReviewCreateViewModel
{
    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = [];

    [Required(ErrorMessage = "Vui lòng nhập tên khách hàng.")]
    [StringLength(160)]
    [Display(Name = "Tên khách hàng")]
    public string CustomerName { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5.")]
    [Display(Name = "Rating")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
    [StringLength(1000)]
    [Display(Name = "Nội dung")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Chi nhánh")]
    public Guid? BranchId { get; set; }

    [Display(Name = "Hiển thị công khai")]
    public bool IsVisible { get; set; } = true;

    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị không hợp lệ.")]
    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }
}

public class ReviewEditViewModel : ReviewCreateViewModel
{
    public Guid Id { get; set; }
}


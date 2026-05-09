using System.ComponentModel.DataAnnotations;

namespace KIGHolding.Areas.Admin.ViewModels;

public class BranchCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên chi nhánh.")]
    [StringLength(180)]
    [Display(Name = "Tên chi nhánh")]
    public string Name { get; set; } = string.Empty;

    [StringLength(180)]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ chi tiết.")]
    [StringLength(300)]
    [Display(Name = "Địa chỉ chi tiết")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập quận/huyện.")]
    [StringLength(120)]
    [Display(Name = "Quận/Huyện")]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tỉnh/thành phố.")]
    [StringLength(120)]
    [Display(Name = "Tỉnh/Thành phố")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập hotline.")]
    [StringLength(32)]
    [Display(Name = "Hotline")]
    public string Hotline { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(160)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Giờ mở cửa")]
    public TimeOnly OpeningTime { get; set; } = new TimeOnly(10, 0);

    [Display(Name = "Giờ đóng cửa")]
    public TimeOnly ClosingTime { get; set; } = new TimeOnly(22, 0);

    [Range(0, int.MaxValue, ErrorMessage = "Sức chứa không hợp lệ.")]
    [Display(Name = "Sức chứa")]
    public int Capacity { get; set; }

    [Range(typeof(decimal), "0", "999999", ErrorMessage = "Diện tích không hợp lệ.")]
    [Display(Name = "Diện tích (m²)")]
    public decimal? AreaSquareMeters { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số tầng phải lớn hơn 0.")]
    [Display(Name = "Số tầng")]
    public int? NumberOfFloors { get; set; }

    [Display(Name = "Mô tả")]
    public string Description { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Google Map URL / Embed Code")]
    public string GoogleMapUrl { get; set; } = string.Empty;

    [Display(Name = "Hiển thị công khai")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Ảnh đại diện")]
    public IFormFile? ThumbnailFile { get; set; }
}

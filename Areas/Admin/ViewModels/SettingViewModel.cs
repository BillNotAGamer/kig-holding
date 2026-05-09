using System.ComponentModel.DataAnnotations;

namespace KIGHolding.Areas.Admin.ViewModels;

public class SettingViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thương hiệu.")]
    [StringLength(160)]
    [Display(Name = "Brand Name")]
    public string BrandName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập slogan.")]
    [StringLength(300)]
    [Display(Name = "Slogan")]
    public string Slogan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập hotline.")]
    [StringLength(32)]
    [Display(Name = "Hotline")]
    public string Hotline { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(160)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [StringLength(300)]
    [Display(Name = "Địa chỉ")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Google Map URL.")]
    [StringLength(1000)]
    [Display(Name = "Google Map URL")]
    public string GoogleMapUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Facebook URL.")]
    [StringLength(500)]
    [Display(Name = "Facebook URL")]
    public string FacebookUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Zalo URL.")]
    [StringLength(500)]
    [Display(Name = "Zalo URL")]
    public string ZaloUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Tiktok URL.")]
    [StringLength(500)]
    [Display(Name = "Tiktok URL")]
    public string TiktokUrl { get; set; } = string.Empty;
}

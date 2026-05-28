using System.ComponentModel.DataAnnotations;
using KIGHolding.Models.Entities;

namespace KIGHolding.ViewModels;

public class ContactCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(160, ErrorMessage = "Họ tên không được vượt quá 160 ký tự.")]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(32, ErrorMessage = "Số điện thoại không được vượt quá 32 ký tự.")]
    [Phone(ErrorMessage = "Số điện thoại chưa đúng định dạng.")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
    [StringLength(160, ErrorMessage = "Email không được vượt quá 160 ký tự.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(220, ErrorMessage = "Chủ đề không được vượt quá 220 ký tự.")]
    [Display(Name = "Chủ đề")]
    public string? Subject { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung liên hệ.")]
    [StringLength(2000, ErrorMessage = "Nội dung không được vượt quá 2000 ký tự.")]
    [Display(Name = "Nội dung")]
    public string Message { get; set; } = string.Empty;

    public SiteSetting? SiteSetting { get; set; }
    public IReadOnlyList<ContactBranchDisplayViewModel> Branches { get; set; } = [];
    public bool IsSuccess { get; set; }

    public string BrandName => string.IsNullOrWhiteSpace(SiteSetting?.BrandName) ? "Truyền Thuyết Champong" : SiteSetting!.BrandName;
    public string Hotline => string.IsNullOrWhiteSpace(SiteSetting?.Hotline) ? "0909 888 777" : SiteSetting!.Hotline;
    public string EmailDisplay => string.IsNullOrWhiteSpace(SiteSetting?.Email) ? "truyenthuyetchamponghcm@gmail.com" : SiteSetting!.Email;
    public string Address => string.IsNullOrWhiteSpace(SiteSetting?.Address) ? "Hệ thống chi nhánh Truyền Thuyết Champong" : SiteSetting!.Address;
    public string GoogleMapUrl => string.IsNullOrWhiteSpace(SiteSetting?.GoogleMapUrl) ? string.Empty : SiteSetting!.GoogleMapUrl;
    public string FacebookUrl => string.IsNullOrWhiteSpace(SiteSetting?.FacebookUrl) ? "#" : SiteSetting!.FacebookUrl;
    public string ZaloUrl => string.IsNullOrWhiteSpace(SiteSetting?.ZaloUrl) ? "#" : SiteSetting!.ZaloUrl;
    public string TiktokUrl => string.IsNullOrWhiteSpace(SiteSetting?.TiktokUrl) ? "#" : SiteSetting!.TiktokUrl;
    public string OpeningHours => Branches.FirstOrDefault()?.OpeningHours ?? "10:00 - 22:30";
}

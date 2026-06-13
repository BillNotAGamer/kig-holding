using System.ComponentModel.DataAnnotations;
using KIGHolding.Services;

namespace KIGHolding.Areas.Admin.ViewModels.Account;

public class ChangePasswordViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(12, ErrorMessage = "Mật khẩu mới phải có ít nhất 12 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            yield break;
        }

        foreach (var error in AdminPasswordPolicy.Validate(NewPassword))
        {
            yield return new ValidationResult(error, [nameof(NewPassword)]);
        }
    }
}

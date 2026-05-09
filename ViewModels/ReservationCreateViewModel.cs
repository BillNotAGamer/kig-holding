using System.ComponentModel.DataAnnotations;

namespace KIGHolding.ViewModels;

public class ReservationCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(160, ErrorMessage = "Họ tên không được vượt quá 160 ký tự.")]
    [Display(Name = "Họ tên")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(32, ErrorMessage = "Số điện thoại không được vượt quá 32 ký tự.")]
    [Phone(ErrorMessage = "Số điện thoại chưa đúng định dạng.")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
    [StringLength(160, ErrorMessage = "Email không được vượt quá 160 ký tự.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn chi nhánh.")]
    [Display(Name = "Chi nhánh")]
    public Guid? BranchId { get; set; }

    [Range(1, 100, ErrorMessage = "Số khách phải từ 1 đến 100.")]
    [Display(Name = "Số lượng khách")]
    public int GuestCount { get; set; } = 2;

    [Required(ErrorMessage = "Vui lòng chọn ngày đến.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày đến")]
    public DateOnly? ReservationDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giờ đến.")]
    [DataType(DataType.Time)]
    [Display(Name = "Giờ đến")]
    public TimeOnly? ReservationTime { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public string? SelectedBranchSlug { get; set; }
    public string Hotline { get; set; } = "0909 888 777";
    public IReadOnlyList<ReservationBranchOptionViewModel> Branches { get; set; } = [];

    public bool ShouldShowLargeGroupNotice => GuestCount > 12;
}

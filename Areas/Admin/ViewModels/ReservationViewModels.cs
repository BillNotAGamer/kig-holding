using System.ComponentModel.DataAnnotations;
using KIGHolding.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KIGHolding.Areas.Admin.ViewModels;

public class ReservationIndexViewModel
{
    [Display(Name = "Trạng thái")]
    public ReservationStatus? StatusFilter { get; set; }

    [Display(Name = "Chi nhánh")]
    public Guid? BranchFilter { get; set; }

    [Display(Name = "Tìm kiếm")]
    public string? SearchQuery { get; set; }

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = [];
    public IReadOnlyList<ReservationListItemViewModel> Reservations { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; } = 1;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public int FirstItemIndex => TotalItems == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastItemIndex => TotalItems == 0 ? 0 : Math.Min(Page * PageSize, TotalItems);
}

public class ReservationListItemViewModel
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public DateOnly ReservationDate { get; set; }
    public TimeOnly ReservationTime { get; set; }
    public int GuestCount { get; set; }
    public string? DiningOccasionCodes { get; set; }
    public string DiningOccasionDisplay { get; set; } = string.Empty;
    public ReservationStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
}

public class ReservationDetailViewModel
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string BranchAddress { get; set; } = string.Empty;
    public DateOnly ReservationDate { get; set; }
    public TimeOnly ReservationTime { get; set; }
    public int GuestCount { get; set; }
    public string? DiningOccasionCodes { get; set; }
    public string? DiningOccasionOtherNote { get; set; }
    public string DiningOccasionDisplay { get; set; } = string.Empty;
    public string? Note { get; set; }
    public ReservationStatus CurrentStatus { get; set; }
    public string StatusLabel { get; set; } = string.Empty;

    [Display(Name = "Tình trạng")]
    public ReservationStatus? Status { get; set; }

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class ReservationPolicyViewModel
{
    public DateOnly VietnamToday { get; set; }
    public int CalendarYear { get; set; }
    public int CalendarMonth { get; set; }
    public IReadOnlyList<DateOnly> BlockedDates { get; set; } = [];
    public string? BlockedDatesInput { get; set; }
    public bool PolicyPayloadPresent { get; set; }
    public bool ConfirmClearAllBlockedDates { get; set; }
}

public sealed class BranchReservationPolicyViewModel
{
    public DateOnly VietnamToday { get; set; }
    public int CalendarYear { get; set; }
    public int CalendarMonth { get; set; }
    public Guid? SelectedBranchId { get; set; }
    public string? SelectedBranchName { get; set; }
    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = [];
    public IReadOnlyList<BranchReservationPolicyCalendarItemViewModel> Policies { get; set; } = [];
    public IReadOnlySet<DateOnly> GlobalBlockedDates { get; set; } = new HashSet<DateOnly>();
    public BranchReservationPolicyEditViewModel Editor { get; set; } = new();
    public bool RequiresImpactConfirmation { get; set; }
    public IReadOnlyList<ReservationStatusCountViewModel> ExistingReservationCounts { get; set; } = [];
    public int ExistingReservationTotal => ExistingReservationCounts.Sum(item => item.Count);
    public bool HasConcurrencyConflict { get; set; }
    public string? PageMessage { get; set; }
}

public sealed class BranchReservationPolicyCalendarItemViewModel
{
    public DateOnly Date { get; set; }
    public BranchReservationRuleType RuleType { get; set; }
    public int? MinGuests { get; set; }
    public Guid Version { get; set; }
}

public sealed class BranchReservationPolicyEditViewModel
{
    public Guid BranchId { get; set; }
    public DateOnly Date { get; set; }
    public BranchPolicyAdminRuleType RuleType { get; set; }
    public int? MinGuests { get; set; }
    public Guid? ExpectedVersion { get; set; }
    public int CalendarYear { get; set; }
    public int CalendarMonth { get; set; }
    public bool ConfirmExistingReservations { get; set; }

    [StringLength(4096)]
    public string? ImpactConfirmationToken { get; set; }
}

public enum BranchPolicyAdminRuleType
{
    Allowed,
    Closed,
    MinimumGuests
}

public sealed class ReservationStatusCountViewModel
{
    public ReservationStatus Status { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

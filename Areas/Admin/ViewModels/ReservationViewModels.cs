using KIGHolding.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KIGHolding.Areas.Admin.ViewModels;

public class ReservationIndexViewModel
{
    public ReservationStatus? StatusFilter { get; set; }
    public Guid? BranchFilter { get; set; }
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = [];
    public IReadOnlyList<ReservationListItemViewModel> Reservations { get; set; } = [];
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
    public ReservationStatus Status { get; set; }
}

public class ReservationDetailViewModel
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchAddress { get; set; } = string.Empty;
    public DateOnly ReservationDate { get; set; }
    public TimeOnly ReservationTime { get; set; }
    public int GuestCount { get; set; }
    public string? Note { get; set; }
    public ReservationStatus Status { get; set; }
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

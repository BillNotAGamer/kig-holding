using KIGHolding.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KIGHolding.Areas.Admin.ViewModels;

public class ContactIndexViewModel
{
    public ContactMessageStatus? StatusFilter { get; set; }
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
    public IReadOnlyList<ContactListItemViewModel> Messages { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; } = 1;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public int FirstItemIndex => TotalItems == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastItemIndex => TotalItems == 0 ? 0 : Math.Min(Page * PageSize, TotalItems);
}

public class ContactListItemViewModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public ContactMessageStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ContactDetailViewModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
    public ContactMessageStatus Status { get; set; }
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

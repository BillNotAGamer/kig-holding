using KIGHolding.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KIGHolding.Areas.Admin.ViewModels;

public class ContactIndexViewModel
{
    public ContactMessageStatus? StatusFilter { get; set; }
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
    public IReadOnlyList<ContactListItemViewModel> Messages { get; set; } = [];
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

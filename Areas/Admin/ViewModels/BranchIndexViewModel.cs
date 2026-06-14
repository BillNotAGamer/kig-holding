using KIGHolding.Models.Entities;

namespace KIGHolding.Areas.Admin.ViewModels;

public sealed class BranchIndexViewModel
{
    public IReadOnlyList<Branch> Branches { get; init; } = [];

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; } = 1;

    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public int FirstItemIndex =>
        TotalItems == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    public int LastItemIndex =>
        TotalItems == 0 ? 0 : Math.Min(Page * PageSize, TotalItems);
}

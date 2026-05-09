namespace KIGHolding.ViewModels.Shared;

public class BookingMiniFormViewModel
{
    public string ActionUrl { get; set; } = "/dat-ban";
    public IReadOnlyList<BookingBranchOptionViewModel> Branches { get; set; } = [];
}

namespace KIGHolding.ViewModels.Shared;

public class NavItemViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IReadOnlyList<NavItemViewModel> Children { get; set; } = [];

    public bool HasChildren => Children.Count > 0;
}

namespace KIGHolding.ViewModels;

public class NewsCategoryViewModel
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? Count { get; set; }
}

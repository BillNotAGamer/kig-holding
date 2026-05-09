using System.ComponentModel.DataAnnotations;

namespace KIGHolding.Areas.Admin.ViewModels;

public class BranchEditViewModel : BranchCreateViewModel
{
    public Guid Id { get; set; }
    public string? ExistingThumbnailUrl { get; set; }
}

using KIGHolding.Models.Entities;
using KIGHolding.ViewModels.Shared;

namespace KIGHolding.ViewModels;

public class NewsDetailViewModel
{
    public Post Post { get; set; } = null!;
    public string CategorySlug { get; set; } = string.Empty;
    public string CategoryDisplayName { get; set; } = string.Empty;
    public bool IsPromotion { get; set; }
    public IReadOnlyList<PostCardViewModel> RelatedPosts { get; set; } = [];
    public string SeoTitle { get; set; } = string.Empty;
    public string SeoDescription { get; set; } = string.Empty;
}

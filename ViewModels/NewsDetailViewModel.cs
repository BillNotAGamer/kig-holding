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
    public string OgTitle { get; set; } = string.Empty;
    public string OgDescription { get; set; } = string.Empty;
    public string OgImageUrl { get; set; } = string.Empty;
    public string CanonicalUrl { get; set; } = string.Empty;
    public string RobotsContent { get; set; } = "index,follow";
    public string? ArticlePublishedTime { get; set; }
    public string ArticleModifiedTime { get; set; } = string.Empty;
    public string ArticleJsonLd { get; set; } = string.Empty;
}

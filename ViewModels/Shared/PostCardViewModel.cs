using KIGHolding.Models.Entities;

namespace KIGHolding.ViewModels.Shared;

public class PostCardViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Url { get; set; } = "#";
    public string ImageUrl { get; set; } = "/images/placeholders/post-card.webp";
    public string Excerpt { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; set; }

    public static PostCardViewModel FromPost(Post post)
    {
        return new PostCardViewModel
        {
            Title = post.Title,
            Slug = post.Slug,
            Url = $"/tin-tuc/{post.Slug}",
            ImageUrl = string.IsNullOrWhiteSpace(post.ThumbnailUrl) ? "/images/placeholders/post-card.webp" : post.ThumbnailUrl,
            Excerpt = post.Excerpt,
            Category = post.Category,
            PublishedAt = post.PublishedAt
        };
    }
}

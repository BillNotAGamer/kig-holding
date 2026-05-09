using KIGHolding.Models.Entities;

namespace KIGHolding.Services;

public interface INewsService
{
    Task<IReadOnlyList<Post>> GetPublishedPostsAsync(int? take = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetPublishedPostsByCategoryAsync(string? category, int? take = null, CancellationToken cancellationToken = default);
    Task<Post?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetRelatedPostsAsync(string category, Guid excludePostId, int take = 3, CancellationToken cancellationToken = default);
}

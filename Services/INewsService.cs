using KIGHolding.Models;
using KIGHolding.Models.Entities;
using KIGHolding.ViewModels;

namespace KIGHolding.Services;

public interface INewsService
{
    Task<IReadOnlyList<Post>> GetPublishedPostsAsync(int? take = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetPublishedPostsByCategoryAsync(string? category, int? take = null, CancellationToken cancellationToken = default);
    Task<PagedResult<Post>> GetPublishedPostsPageAsync(string? category, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Post?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetRelatedPostsAsync(string category, Guid excludePostId, int take = 3, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SuggestedPostCategoryViewModel>> GetSuggestedCategoriesAsync(
        int limit = 4,
        int poolSize = 8,
        bool rotateWithinRecentPool = false,
        CancellationToken cancellationToken = default);
}

using KIGHolding.Models;
using KIGHolding.Models.Entities;
using KIGHolding.ViewModels;
using KIGHolding.ViewModels.Shared;

namespace KIGHolding.Services;

public interface INewsService
{
    Task<IReadOnlyList<PostCardViewModel>> GetPublishedPostCardsAsync(int? take = null, CancellationToken cancellationToken = default);
    Task<PagedResult<PostCardViewModel>> GetPublishedPostCardsPageAsync(string? category, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Post?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostCardViewModel>> GetRelatedPostCardsAsync(string category, Guid excludePostId, int take = 3, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SuggestedPostCategoryViewModel>> GetSuggestedCategoriesAsync(
        int limit = 4,
        int poolSize = 8,
        bool rotateWithinRecentPool = false,
        CancellationToken cancellationToken = default);
    void InvalidateNewsCache();
}

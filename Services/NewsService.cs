using KIGHolding.Data;
using KIGHolding.Models;
using KIGHolding.Models.Content;
using KIGHolding.Models.Entities;
using KIGHolding.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KIGHolding.Services;

public class NewsService : INewsService
{
    private const int SuggestedCategoriesCacheMinutes = 15;

    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public NewsService(AppDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<IReadOnlyList<Post>> GetPublishedPostsAsync(int? take = null, CancellationToken cancellationToken = default)
    {
        var query = CreatePublishedPostsQuery();

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetPublishedPostsByCategoryAsync(string? category, int? take = null, CancellationToken cancellationToken = default)
    {
        var query = ApplyCategoryFilter(CreatePublishedPostsQuery(), category);

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Post>> GetPublishedPostsPageAsync(string? category, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var resolvedPage = Math.Max(1, page);
        var resolvedPageSize = pageSize > 0 ? pageSize : 9;
        var query = ApplyCategoryFilter(CreatePublishedPostsQuery(), category);

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)resolvedPageSize);

        if (totalPages > 0 && resolvedPage > totalPages)
        {
            resolvedPage = totalPages;
        }

        var items = totalItems == 0
            ? []
            : await query
                .Skip((resolvedPage - 1) * resolvedPageSize)
                .Take(resolvedPageSize)
                .ToListAsync(cancellationToken);

        return new PagedResult<Post>
        {
            Items = items,
            Page = resolvedPage,
            PageSize = resolvedPageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public Task<Post?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _dbContext.Posts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished, cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetRelatedPostsAsync(string category, Guid excludePostId, int take = 3, CancellationToken cancellationToken = default)
    {
        return await ApplyCategoryFilter(CreatePublishedPostsQuery(), category)
            .Where(x => x.Id != excludePostId)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SuggestedPostCategoryViewModel>> GetSuggestedCategoriesAsync(
        int limit = 4,
        int poolSize = 8,
        bool rotateWithinRecentPool = false,
        CancellationToken cancellationToken = default)
    {
        var resolvedLimit = Math.Clamp(limit, 1, 12);
        var resolvedPoolSize = Math.Clamp(poolSize, resolvedLimit, 24);
        var dailyRotationKey = rotateWithinRecentPool
            ? DateOnly.FromDateTime(DateTime.UtcNow).DayNumber
            : 0;
        var cacheKey = $"news:suggested-categories:{resolvedLimit}:{resolvedPoolSize}:{rotateWithinRecentPool}:{dailyRotationKey}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(SuggestedCategoriesCacheMinutes);
            entry.Size = 1;

            return await BuildSuggestedCategoriesAsync(
                resolvedLimit,
                resolvedPoolSize,
                rotateWithinRecentPool,
                dailyRotationKey,
                cancellationToken);
        }) ?? [];
    }

    private async Task<IReadOnlyList<SuggestedPostCategoryViewModel>> BuildSuggestedCategoriesAsync(
        int limit,
        int poolSize,
        bool rotateWithinRecentPool,
        int dailyRotationKey,
        CancellationToken cancellationToken)
    {
        var posts = await _dbContext.Posts.AsNoTracking()
            .Where(x => x.IsPublished)
            .Select(x => new
            {
                x.Category,
                x.Title,
                x.Slug,
                x.PublishedAt,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var categories = posts
            .Select(post =>
            {
                var categorySlug = NewsCategories.NormalizeCategory(post.Category);
                if (string.IsNullOrWhiteSpace(categorySlug) ||
                    !NewsCategories.TryGetBySlug(categorySlug, out var definition) ||
                    definition is null)
                {
                    return null;
                }

                return new SuggestedPostCategoryPost(
                    categorySlug,
                    definition.DisplayName,
                    definition.Order,
                    post.Title,
                    post.Slug,
                    post.PublishedAt ?? post.CreatedAt);
            })
            .Where(post => post is not null)
            .Cast<SuggestedPostCategoryPost>()
            .GroupBy(post => post.Slug, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var latestPost = group
                    .OrderByDescending(post => post.PublicDate)
                    .ThenBy(post => post.Title, StringComparer.OrdinalIgnoreCase)
                    .First();

                return new SuggestedPostCategoryViewModel
                {
                    Slug = group.Key,
                    DisplayName = latestPost.DisplayName,
                    Order = latestPost.Order,
                    PostCount = group.Count(),
                    LatestPostDate = latestPost.PublicDate,
                    LatestPostTitle = latestPost.Title,
                    LatestPostSlug = latestPost.PostSlug
                };
            })
            .OrderByDescending(category => category.LatestPostDate)
            .ThenBy(category => category.Order)
            .ThenBy(category => category.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(poolSize)
            .ToList();

        if (rotateWithinRecentPool && categories.Count > 1)
        {
            var offset = dailyRotationKey % categories.Count;
            categories = categories
                .Skip(offset)
                .Concat(categories.Take(offset))
                .ToList();
        }

        return categories.Take(limit).ToList();
    }

    private IQueryable<Post> CreatePublishedPostsQuery()
    {
        return _dbContext.Posts
            .AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.CreatedAt);
    }

    private static IQueryable<Post> ApplyCategoryFilter(IQueryable<Post> query, string? category)
    {
        var categoryAliases = NewsCategories.GetStorageAliases(category);
        if (categoryAliases.Count == 0)
        {
            return query;
        }

        return query.Where(x => categoryAliases.Contains(x.Category));
    }

    private sealed record SuggestedPostCategoryPost(
        string Slug,
        string DisplayName,
        int Order,
        string Title,
        string PostSlug,
        DateTimeOffset PublicDate);
}

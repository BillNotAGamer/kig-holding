using System.Threading;
using KIGHolding.Data;
using KIGHolding.Models;
using KIGHolding.Models.Content;
using KIGHolding.Models.Entities;
using KIGHolding.ViewModels;
using KIGHolding.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KIGHolding.Services;

public class NewsService : INewsService
{
    private const int SuggestedCategoriesCacheMinutes = 15;
    private static readonly TimeSpan PublicNewsAbsoluteExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PublicNewsSlidingExpiration = TimeSpan.FromMinutes(2);
    private static long _cacheVersion = 1;

    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public NewsService(AppDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<IReadOnlyList<PostCardViewModel>> GetPublishedPostCardsAsync(int? take = null, CancellationToken cancellationToken = default)
    {
        int? resolvedTake = take.HasValue && take.Value > 0 ? take.Value : null;
        var cacheKey = BuildCacheKey("list", $"take:{resolvedTake?.ToString() ?? "all"}");

        var items = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            ApplyPublicNewsCacheOptions(entry);

            var query = CreatePublishedPostCardProjection(CreatePublishedPostsQuery());
            if (resolvedTake.HasValue)
            {
                query = query.Take(resolvedTake.Value);
            }

            var posts = await query.ToListAsync(cancellationToken);
            return posts.Select(MapToPostCard).ToList();
        });

        return items ?? [];
    }

    public async Task<PagedResult<PostCardViewModel>> GetPublishedPostCardsPageAsync(string? category, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalizedCategory = NewsCategories.NormalizeCategory(category);
        var resolvedPage = Math.Max(1, page);
        var resolvedPageSize = pageSize > 0 ? pageSize : 9;
        var cacheKey = BuildCacheKey(
            "page",
            $"category:{BuildCategoryCacheToken(normalizedCategory)}",
            $"page:{resolvedPage}",
            $"page-size:{resolvedPageSize}");

        var pagedResult = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            ApplyPublicNewsCacheOptions(entry);

            var query = ApplyCategoryFilter(CreatePublishedPostsQuery(), normalizedCategory);
            var totalItems = await query.CountAsync(cancellationToken);
            var totalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling(totalItems / (double)resolvedPageSize);

            var currentPage = resolvedPage;
            if (totalPages > 0 && currentPage > totalPages)
            {
                currentPage = totalPages;
            }

            var items = totalItems == 0
                ? []
                : await CreatePublishedPostCardProjection(query)
                    .Skip((currentPage - 1) * resolvedPageSize)
                    .Take(resolvedPageSize)
                    .ToListAsync(cancellationToken);

            return new PagedResult<PostCardViewModel>
            {
                Items = items.Select(MapToPostCard).ToList(),
                Page = currentPage,
                PageSize = resolvedPageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        });

        return pagedResult ?? new PagedResult<PostCardViewModel>
        {
            Page = resolvedPage,
            PageSize = resolvedPageSize
        };
    }

    public async Task<Post?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return null;
        }

        var cacheKey = BuildCacheKey("detail", $"slug:{normalizedSlug}");
        if (_cache.TryGetValue(cacheKey, out Post? cachedPost))
        {
            return cachedPost;
        }

        var post = await _dbContext.Posts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == normalizedSlug && x.IsPublished, cancellationToken);

        if (post is not null)
        {
            _cache.Set(cacheKey, post, CreatePublicNewsCacheOptions());
        }

        return post;
    }

    public async Task<IReadOnlyList<PostCardViewModel>> GetRelatedPostCardsAsync(string category, Guid excludePostId, int take = 3, CancellationToken cancellationToken = default)
    {
        var resolvedTake = take > 0 ? take : 3;
        var normalizedCategory = NewsCategories.NormalizeCategory(category);
        var cacheKey = BuildCacheKey(
            "related",
            $"category:{BuildCategoryCacheToken(normalizedCategory)}",
            $"exclude:{excludePostId:N}",
            $"take:{resolvedTake}");

        var items = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            ApplyPublicNewsCacheOptions(entry);

            var posts = await CreatePublishedPostCardProjection(
                    ApplyCategoryFilter(CreatePublishedPostsQuery(), normalizedCategory)
                        .Where(x => x.Id != excludePostId))
                .Take(resolvedTake)
                .ToListAsync(cancellationToken);

            return posts.Select(MapToPostCard).ToList();
        });

        return items ?? [];
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
        var cacheKey = BuildCacheKey(
            "suggested-categories",
            $"limit:{resolvedLimit}",
            $"pool:{resolvedPoolSize}",
            $"rotate:{rotateWithinRecentPool}",
            $"day:{dailyRotationKey}");

        var categories = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(SuggestedCategoriesCacheMinutes);
            entry.SetSize(1);

            return await BuildSuggestedCategoriesAsync(
                resolvedLimit,
                resolvedPoolSize,
                rotateWithinRecentPool,
                dailyRotationKey,
                cancellationToken);
        });

        return categories ?? [];
    }

    public void InvalidateNewsCache()
    {
        Interlocked.Increment(ref _cacheVersion);
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

    private static IQueryable<PublicPostListItem> CreatePublishedPostCardProjection(IQueryable<Post> query)
    {
        return query.Select(x => new PublicPostListItem
        {
            Id = x.Id,
            Title = x.Title,
            Slug = x.Slug,
            ThumbnailUrl = x.ThumbnailUrl,
            Excerpt = x.Excerpt,
            Category = x.Category,
            PublishedAt = x.PublishedAt,
            UpdatedAt = x.UpdatedAt,
            RobotsIndex = x.RobotsIndex
        });
    }

    private static PostCardViewModel MapToPostCard(PublicPostListItem post)
    {
        return new PostCardViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Url = $"/tin-tuc/{post.Slug}",
            ImageUrl = string.IsNullOrWhiteSpace(post.ThumbnailUrl) ? "/images/placeholders/post-card.webp" : post.ThumbnailUrl,
            Excerpt = post.Excerpt,
            Category = NewsCategories.GetDisplayName(post.Category),
            PublishedAt = post.PublishedAt,
            UpdatedAt = post.UpdatedAt,
            RobotsIndex = post.RobotsIndex
        };
    }

    private static MemoryCacheEntryOptions CreatePublicNewsCacheOptions()
    {
        return new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(PublicNewsAbsoluteExpiration)
            .SetSlidingExpiration(PublicNewsSlidingExpiration)
            .SetSize(1);
    }

    private static void ApplyPublicNewsCacheOptions(ICacheEntry entry)
    {
        entry.AbsoluteExpirationRelativeToNow = PublicNewsAbsoluteExpiration;
        entry.SlidingExpiration = PublicNewsSlidingExpiration;
        entry.SetSize(1);
    }

    private static string BuildCacheKey(string scope, params string[] segments)
    {
        var version = Volatile.Read(ref _cacheVersion);
        return string.Join(":", new[] { "news", scope, $"v{version}" }.Concat(segments));
    }

    private static string BuildCategoryCacheToken(string? category)
    {
        return string.IsNullOrWhiteSpace(category) ? "all" : category.Trim().ToLowerInvariant();
    }

    private static string NormalizeSlug(string slug)
    {
        return string.IsNullOrWhiteSpace(slug)
            ? string.Empty
            : slug.Trim().ToLowerInvariant();
    }

    private sealed class PublicPostListItem
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string? ThumbnailUrl { get; init; }
        public string Excerpt { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public DateTimeOffset? PublishedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public bool RobotsIndex { get; init; } = true;
    }

    private sealed record SuggestedPostCategoryPost(
        string Slug,
        string DisplayName,
        int Order,
        string Title,
        string PostSlug,
        DateTimeOffset PublicDate);
}

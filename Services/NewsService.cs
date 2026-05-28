using KIGHolding.Data;
using KIGHolding.Models;
using KIGHolding.Models.Content;
using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Services;

public class NewsService : INewsService
{
    private readonly AppDbContext _dbContext;

    public NewsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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
}

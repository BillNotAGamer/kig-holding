using KIGHolding.Data;
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
        var query = CreatePublishedPostsQuery();

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryAliases = GetCategoryAliases(category);
            query = query.Where(x => categoryAliases.Contains(x.Category));
        }

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<Post?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _dbContext.Posts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished, cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetRelatedPostsAsync(string category, Guid excludePostId, int take = 3, CancellationToken cancellationToken = default)
    {
        var categoryAliases = GetCategoryAliases(category);

        return await CreatePublishedPostsQuery()
            .Where(x => x.Id != excludePostId && categoryAliases.Contains(x.Category))
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

    private static string[] GetCategoryAliases(string category)
    {
        var normalizedCategory = category.Trim();

        return normalizedCategory switch
        {
            "Câu chuyện ẩm thực" => ["Câu chuyện ẩm thực", "Ẩm thực"],
            "Ẩm thực" => ["Câu chuyện ẩm thực", "Ẩm thực"],
            "Sự kiện" => ["Sự kiện", "Tin tức"],
            "Tin tức" => ["Sự kiện", "Tin tức"],
            _ => [normalizedCategory]
        };
    }
}

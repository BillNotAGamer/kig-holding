using KIGHolding.Data;
using KIGHolding.Models.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Services;

public class BranchService : IBranchService
{
    private const string ActiveBranchesCacheKey = "branches:active";
    private static readonly TimeSpan ActiveBranchesCacheDuration = TimeSpan.FromMinutes(10);

    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public BranchService(AppDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<IReadOnlyList<Branch>> GetActiveBranchesAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(ActiveBranchesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ActiveBranchesCacheDuration;

            return await _dbContext.Branches
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }) ?? [];
    }

    public Task<Branch?> GetBranchBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _dbContext.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetVisibleReviewsAsync(int take = 6, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Reviews
            .AsNoTracking()
            .Include(x => x.Branch)
            .Where(x => x.IsVisible)
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public void InvalidateActiveBranchesCache()
    {
        _cache.Remove(ActiveBranchesCacheKey);
    }
}

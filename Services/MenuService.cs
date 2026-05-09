using KIGHolding.Data;
using KIGHolding.Models.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Services;

public class MenuService : IMenuService
{
    private const string ActiveCategoriesCacheKey = "menu-categories:active";
    private static readonly TimeSpan ActiveCategoriesCacheDuration = TimeSpan.FromMinutes(10);

    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public MenuService(AppDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<IReadOnlyList<MenuCategory>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(ActiveCategoriesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ActiveCategoriesCacheDuration;

            return await _dbContext.MenuCategories
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }) ?? [];
    }

    public void InvalidateActiveCategoriesCache()
    {
        _cache.Remove(ActiveCategoriesCacheKey);
    }

    public async Task<IReadOnlyList<MenuItem>> GetActiveMenuItemsAsync(string? categorySlug = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.IsAvailable && x.Category.IsActive);

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            query = query.Where(x => x.Category.Slug == categorySlug);
        }

        return await query
            .OrderBy(x => x.Category.DisplayOrder)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuItem>> GetSignatureMenuItemsAsync(int take = 8, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.IsAvailable && x.IsSignature && x.Category.IsActive)
            .OrderBy(x => x.Category.DisplayOrder)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuItem>> GetFeaturedMenuItemsAsync(int take = 8, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.IsAvailable && x.Category.IsActive && (x.IsBestSeller || x.IsSignature || x.IsNew))
            .OrderByDescending(x => x.IsBestSeller)
            .ThenByDescending(x => x.IsSignature)
            .ThenByDescending(x => x.IsNew)
            .ThenBy(x => x.Category.DisplayOrder)
            .ThenBy(x => x.DisplayOrder)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<MenuItem?> GetMenuItemBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _dbContext.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images.OrderBy(image => image.DisplayOrder))
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsAvailable && x.Category.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<MenuItem>> GetRelatedMenuItemsAsync(Guid categoryId, Guid excludeMenuItemId, int take = 4, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.CategoryId == categoryId
                && x.Id != excludeMenuItemId
                && x.IsAvailable
                && x.Category.IsActive)
            .OrderByDescending(x => x.IsBestSeller)
            .ThenByDescending(x => x.IsSignature)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}

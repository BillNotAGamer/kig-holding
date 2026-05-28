using KIGHolding.Data;
using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Services;

public class MenuGroupService : IMenuGroupService
{
    private readonly AppDbContext _dbContext;

    public MenuGroupService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MenuGroup>> GetPublishedGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuGroups
            .AsNoTracking()
            .Include(x => x.PageImages.Where(image => image.IsPublished))
            .Where(x => x.IsPublished)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<MenuGroup?> GetPublishedGroupBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return Task.FromResult<MenuGroup?>(null);
        }

        return _dbContext.MenuGroups
            .AsNoTracking()
            .Include(x => x.PageImages.Where(image => image.IsPublished))
            .FirstOrDefaultAsync(x => x.IsPublished && x.Slug == normalizedSlug, cancellationToken);
    }

    public async Task<IReadOnlyList<MenuPageImage>> GetPublishedImagesByGroupSlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return [];
        }

        return await _dbContext.MenuPageImages
            .AsNoTracking()
            .Where(x => x.IsPublished && x.MenuGroup.IsPublished && x.MenuGroup.Slug == normalizedSlug)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuGroup>> GetAllGroupsForAdminAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuGroups
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<MenuGroup?> GetGroupForAdminAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.MenuGroups
            .AsNoTracking()
            .Include(x => x.PageImages)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<MenuGroup?> GetGroupForAdminBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return Task.FromResult<MenuGroup?>(null);
        }

        return _dbContext.MenuGroups
            .AsNoTracking()
            .Include(x => x.PageImages)
            .FirstOrDefaultAsync(x => x.Slug == normalizedSlug, cancellationToken);
    }

    private static string? NormalizeSlug(string? slug)
    {
        return string.IsNullOrWhiteSpace(slug) ? null : slug.Trim().ToLowerInvariant();
    }
}

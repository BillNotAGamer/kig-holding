using KIGHolding.Data;
using KIGHolding.Models.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Services;

public class SiteSettingService : ISiteSettingService
{
    private const string CacheKey = "site-setting:active";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public SiteSettingService(AppDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<SiteSetting?> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            return await _dbContext.SiteSettings
                .AsNoTracking()
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        });
    }

    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
    }
}

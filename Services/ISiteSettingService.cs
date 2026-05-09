using KIGHolding.Models.Entities;

namespace KIGHolding.Services;

public interface ISiteSettingService
{
    Task<SiteSetting?> GetSettingsAsync(CancellationToken cancellationToken = default);
    void InvalidateCache();
}

using KIGHolding.Models.Entities;

namespace KIGHolding.Services;

public interface IMenuService
{
    Task<IReadOnlyList<MenuCategory>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);
    void InvalidateActiveCategoriesCache();
    Task<IReadOnlyList<MenuItem>> GetActiveMenuItemsAsync(string? categorySlug = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuItem>> GetSignatureMenuItemsAsync(int take = 8, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuItem>> GetFeaturedMenuItemsAsync(int take = 8, CancellationToken cancellationToken = default);
    Task<MenuItem?> GetMenuItemBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuItem>> GetRelatedMenuItemsAsync(Guid categoryId, Guid excludeMenuItemId, int take = 4, CancellationToken cancellationToken = default);
}

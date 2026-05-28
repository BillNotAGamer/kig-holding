using KIGHolding.Models.Entities;

namespace KIGHolding.Services;

public interface IMenuGroupService
{
    Task<IReadOnlyList<MenuGroup>> GetPublishedGroupsAsync(CancellationToken cancellationToken = default);
    Task<MenuGroup?> GetPublishedGroupBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuPageImage>> GetPublishedImagesByGroupSlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuGroup>> GetAllGroupsForAdminAsync(CancellationToken cancellationToken = default);
    Task<MenuGroup?> GetGroupForAdminAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MenuGroup?> GetGroupForAdminBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

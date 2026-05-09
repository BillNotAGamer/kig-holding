using KIGHolding.Models.Entities;

namespace KIGHolding.Services;

public interface IBranchService
{
    Task<IReadOnlyList<Branch>> GetActiveBranchesAsync(CancellationToken cancellationToken = default);
    Task<Branch?> GetBranchBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetVisibleReviewsAsync(int take = 6, CancellationToken cancellationToken = default);
    void InvalidateActiveBranchesCache();
}

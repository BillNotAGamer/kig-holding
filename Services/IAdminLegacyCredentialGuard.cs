namespace KIGHolding.Services;

public interface IAdminLegacyCredentialGuard
{
    Task EnsureSecureAsync(CancellationToken cancellationToken = default);
}

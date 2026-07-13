using KIGHolding.Options;
using Microsoft.Extensions.Options;

namespace KIGHolding.Services;

public sealed class ImageStorageService : IImageStorageService
{
    private readonly IReadOnlyDictionary<ImageStorageProviderKind, IImageStorageProvider> _providers;
    private readonly ImageStorageSettings _settings;

    public ImageStorageService(
        IEnumerable<IImageStorageProvider> providers,
        IOptions<ImageStorageSettings> settings)
    {
        _providers = providers.ToDictionary(x => x.Provider);
        _settings = settings.Value;
    }

    public Task<string> UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default)
    {
        return UploadCoreAsync(file, category, storageScope: null, cancellationToken);
    }

    public Task<string> UploadAsync(IFormFile file, ImageCategory category, string storageScope, CancellationToken cancellationToken = default)
    {
        return UploadCoreAsync(file, category, storageScope, cancellationToken);
    }

    private Task<string> UploadCoreAsync(IFormFile file, ImageCategory category, string? storageScope, CancellationToken cancellationToken)
    {
        var providerKind = ImageStorageProviderKindParser.ParseOrThrow(_settings.Provider);
        if (!_providers.TryGetValue(providerKind, out var provider))
        {
            throw new InvalidOperationException($"Image storage provider '{providerKind}' is not registered.");
        }

        var normalizedScope = ImageStoragePathUtilities.NormalizeOptionalStorageScope(category, storageScope);
        return provider.UploadAsync(file, category, normalizedScope, cancellationToken);
    }

    public async Task DeleteAsync(string? imageUrlOrPath, ImageCategory category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrlOrPath))
        {
            return;
        }

        foreach (var provider in _providers.Values)
        {
            if (!provider.CanDelete(imageUrlOrPath, category))
            {
                continue;
            }

            await provider.DeleteAsync(imageUrlOrPath, category, cancellationToken);
            return;
        }
    }
}

namespace KIGHolding.Services;

public interface IImageStorageProvider
{
    ImageStorageProviderKind Provider { get; }

    Task<string> UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default);

    Task<string> UploadAsync(IFormFile file, ImageCategory category, string? storageScope, CancellationToken cancellationToken = default);

    bool CanDelete(string imageUrlOrPath, ImageCategory category);

    Task DeleteAsync(string imageUrlOrPath, ImageCategory category, CancellationToken cancellationToken = default);
}

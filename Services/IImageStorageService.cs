namespace KIGHolding.Services;

public interface IImageStorageService
{
    Task<string> UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? imageUrlOrPath, ImageCategory category, CancellationToken cancellationToken = default);
}

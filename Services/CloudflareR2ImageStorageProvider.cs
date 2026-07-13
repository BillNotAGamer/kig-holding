using KIGHolding.Options;
using Microsoft.Extensions.Options;

namespace KIGHolding.Services;

public sealed class CloudflareR2ImageStorageProvider : IImageStorageProvider
{
    private readonly ImageStorageSettings _settings;
    private readonly ICloudflareR2Client _client;
    private readonly ILogger<CloudflareR2ImageStorageProvider> _logger;
    private readonly ImageUploadValidator _uploadValidator;

    public CloudflareR2ImageStorageProvider(
        IOptions<ImageStorageSettings> settings,
        ICloudflareR2Client client,
        ILogger<CloudflareR2ImageStorageProvider> logger)
    {
        _settings = settings.Value;
        _client = client;
        _logger = logger;
        _uploadValidator = new ImageUploadValidator(_settings);
    }

    public ImageStorageProviderKind Provider => ImageStorageProviderKind.CloudflareR2;

    public async Task<string> UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default)
    {
        return await UploadAsync(file, category, storageScope: null, cancellationToken);
    }

    public async Task<string> UploadAsync(IFormFile file, ImageCategory category, string? storageScope, CancellationToken cancellationToken = default)
    {
        var validated = _uploadValidator.Validate(file);
        var objectKey = CloudflareR2ObjectKeyBuilder.BuildObjectKey(
            _settings,
            category,
            file.FileName,
            validated.Extension,
            storageScope);

        try
        {
            await using var stream = file.OpenReadStream();
            await _client.PutObjectAsync(
                _settings.BucketName.Trim(),
                objectKey,
                validated.ContentType,
                stream,
                cancellationToken);

            return BuildPublicUrl(objectKey);
        }
        catch (Exception exception) when (exception is not InvalidOperationException and not OperationCanceledException)
        {
            _logger.LogWarning(
                "Cloudflare R2 upload failed for category {Category}, object key {ObjectKey}. ExceptionType={ExceptionType}",
                category,
                objectKey,
                exception.GetType().Name);
            throw new InvalidOperationException("Khong the tai anh len kho luu tru R2. Vui long thu lai.");
        }
    }

    public bool CanDelete(string imageUrlOrPath, ImageCategory category)
    {
        return TryExtractManagedObjectKey(imageUrlOrPath, category, out _);
    }

    public async Task DeleteAsync(string imageUrlOrPath, ImageCategory category, CancellationToken cancellationToken = default)
    {
        if (!TryExtractManagedObjectKey(imageUrlOrPath, category, out var objectKey))
        {
            return;
        }

        try
        {
            await _client.DeleteObjectAsync(
                _settings.BucketName.Trim(),
                objectKey,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                "Cloudflare R2 delete failed for category {Category}, object key {ObjectKey}. ExceptionType={ExceptionType}",
                category,
                objectKey,
                exception.GetType().Name);
        }
    }

    public bool TryExtractManagedObjectKey(
        string? imageUrlOrPath,
        ImageCategory category,
        out string objectKey)
    {
        objectKey = string.Empty;

        if (string.IsNullOrWhiteSpace(imageUrlOrPath))
        {
            return false;
        }

        var candidate = imageUrlOrPath.Trim();
        if (Uri.TryCreate(candidate, UriKind.Absolute, out var candidateUri))
        {
            if (!TryExtractKeyFromPublicUrl(candidateUri, out candidate))
            {
                return false;
            }
        }
        else if (candidate.StartsWith('/'))
        {
            return false;
        }

        candidate = Uri.UnescapeDataString(candidate)
            .Replace('\\', '/')
            .TrimStart('/');

        if (!ImageStoragePathUtilities.IsSafeObjectKey(candidate))
        {
            return false;
        }

        var expectedPrefix = ImageStoragePathUtilities.GetR2Prefix(_settings, category);
        if (!candidate.StartsWith($"{expectedPrefix}/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        objectKey = candidate;
        return true;
    }

    private string BuildPublicUrl(string objectKey)
    {
        return $"{ImageStoragePathUtilities.NormalizePublicBaseUrl(_settings.PublicBaseUrl)}/{objectKey}";
    }

    private bool TryExtractKeyFromPublicUrl(Uri candidateUri, out string objectKey)
    {
        objectKey = string.Empty;

        if (!Uri.TryCreate(ImageStoragePathUtilities.NormalizePublicBaseUrl(_settings.PublicBaseUrl), UriKind.Absolute, out var publicBaseUri))
        {
            return false;
        }

        if (!string.Equals(candidateUri.Scheme, publicBaseUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(candidateUri.Host, publicBaseUri.Host, StringComparison.OrdinalIgnoreCase) ||
            candidateUri.Port != publicBaseUri.Port)
        {
            return false;
        }

        var basePath = publicBaseUri.AbsolutePath.TrimEnd('/');
        var candidatePath = candidateUri.AbsolutePath;

        if (string.Equals(basePath, "/", StringComparison.Ordinal))
        {
            objectKey = candidatePath.TrimStart('/');
            return true;
        }

        if (!candidatePath.StartsWith(basePath + "/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        objectKey = candidatePath[(basePath.Length + 1)..];
        return true;
    }
}

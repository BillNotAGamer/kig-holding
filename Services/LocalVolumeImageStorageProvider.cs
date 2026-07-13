using KIGHolding.Options;
using Microsoft.Extensions.Options;

namespace KIGHolding.Services;

public sealed class LocalVolumeImageStorageProvider : IImageStorageProvider
{
    private readonly IWebHostEnvironment _environment;
    private readonly ImageStorageSettings _settings;
    private readonly ILogger<LocalVolumeImageStorageProvider> _logger;
    private readonly ImageUploadValidator _uploadValidator;

    public LocalVolumeImageStorageProvider(
        IWebHostEnvironment environment,
        IOptions<ImageStorageSettings> settings,
        ILogger<LocalVolumeImageStorageProvider> logger)
    {
        _environment = environment;
        _settings = settings.Value;
        _logger = logger;
        _uploadValidator = new ImageUploadValidator(_settings);
    }

    public ImageStorageProviderKind Provider => ImageStorageProviderKind.LocalVolume;

    public async Task<string> UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default)
    {
        return await UploadAsync(file, category, storageScope: null, cancellationToken);
    }

    public async Task<string> UploadAsync(IFormFile file, ImageCategory category, string? storageScope, CancellationToken cancellationToken = default)
    {
        var validated = _uploadValidator.Validate(file);
        var categorySubPath = ImageStoragePathUtilities.GetLocalCategorySubPath(category, storageScope);
        var uploadsFolder = GetPhysicalFolder(categorySubPath);
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = ImageStoragePathUtilities.CreateSafeFileName(file.FileName, category, validated.Extension);
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        try
        {
            await using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(fileStream, cancellationToken);

            var publicPath = ImageStoragePathUtilities.NormalizePublicPath(_settings.PublicBasePath);
            return $"{publicPath}/{categorySubPath}/{uniqueFileName}";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Local image upload failed for category {Category}.", category);
            throw new InvalidOperationException("Khong the luu anh tren may chu. Vui long thu lai.", exception);
        }
    }

    public bool CanDelete(string imageUrlOrPath, ImageCategory category)
    {
        if (string.IsNullOrWhiteSpace(imageUrlOrPath))
        {
            return false;
        }

        var publicPath = ImageStoragePathUtilities.NormalizePublicPath(_settings.PublicBasePath);
        var expectedPrefix = $"{publicPath}/{ImageStoragePathUtilities.GetLocalCategorySubPath(category)}";

        return imageUrlOrPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) ||
               imageUrlOrPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase);
    }

    public Task DeleteAsync(string imageUrlOrPath, ImageCategory category, CancellationToken cancellationToken = default)
    {
        if (!CanDelete(imageUrlOrPath, category))
        {
            return Task.CompletedTask;
        }

        try
        {
            var publicPath = ImageStoragePathUtilities.NormalizePublicPath(_settings.PublicBasePath);
            var urlBase = imageUrlOrPath.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase)
                ? publicPath
                : "/uploads";

            var relativePath = imageUrlOrPath[urlBase.Length..]
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var externalUploadRoot = GetUploadRoot();
            var fullPath = Path.GetFullPath(Path.Combine(externalUploadRoot, relativePath));
            var uploadsRoot = Path.GetFullPath(externalUploadRoot);

            if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Best-effort local delete failed for category {Category}.", category);
        }

        return Task.CompletedTask;
    }

    private string GetPhysicalFolder(string categorySubPath)
    {
        var categoryPath = categorySubPath
            .Replace('/', Path.DirectorySeparatorChar);

        var uploadRoot = Path.GetFullPath(GetUploadRoot());
        var folder = Path.GetFullPath(Path.Combine(uploadRoot, categoryPath));
        if (!folder.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved local image storage path is outside the upload root.");
        }

        return folder;
    }

    private string GetUploadRoot()
    {
        return Path.IsPathRooted(_settings.RootPath)
            ? _settings.RootPath
            : Path.Combine(_environment.ContentRootPath, _settings.RootPath);
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using KIGHolding.Options;
using Microsoft.Extensions.Options;

namespace KIGHolding.Services;

public sealed class ImageStorageService : IImageStorageService
{
    private const string DefaultFolderPrefix = "kig-holding";
    private const string CloudinaryUploadPathMarker = "/image/upload/";

    private readonly IWebHostEnvironment _environment;
    private readonly CloudinarySettings _settings;
    private readonly ILogger<ImageStorageService> _logger;

    public ImageStorageService(
        IWebHostEnvironment environment,
        IOptions<CloudinarySettings> settings,
        ILogger<ImageStorageService> logger)
    {
        _environment = environment;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Tệp ảnh tải lên không hợp lệ.");
        }

        if (category == ImageCategory.MenuGroupCovers)
        {
            return await UploadLocallyAsync(file, category, cancellationToken);
        }

        var folder = GetCloudinaryFolder(category);
        var cloudinary = CreateCloudinaryClientOrThrow(category, folder);

        try
        {
            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await cloudinary.UploadAsync(uploadParams);
            if (result.Error is not null)
            {
                _logger.LogWarning(
                    "Cloudinary upload failed for category {Category}, folder {Folder}. Message={Message}",
                    category,
                    folder,
                    result.Error.Message);
                throw new InvalidOperationException("Không thể tải ảnh lên Cloudinary. Vui lòng thử lại.");
            }

            var secureUrl = result.SecureUrl?.AbsoluteUri;
            if (string.IsNullOrWhiteSpace(secureUrl))
            {
                _logger.LogWarning(
                    "Cloudinary upload for category {Category}, folder {Folder} completed without a secure URL. PublicId={PublicId}",
                    category,
                    folder,
                    result.PublicId);
                throw new InvalidOperationException("Cloudinary không trả về URL ảnh hợp lệ.");
            }

            return secureUrl;
        }
        catch (Exception exception) when (exception is not InvalidOperationException)
        {
            _logger.LogWarning(
                exception,
                "Cloudinary upload threw for category {Category}, folder {Folder}.",
                category,
                folder);
            throw new InvalidOperationException("Không thể tải ảnh lên Cloudinary. Vui lòng thử lại.", exception);
        }
    }

    public async Task DeleteAsync(string? imageUrlOrPath, ImageCategory category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrlOrPath))
        {
            return;
        }

        if (imageUrlOrPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            TryDeleteLocalFile(imageUrlOrPath, category);
            return;
        }

        if (category == ImageCategory.MenuGroupCovers)
        {
            return;
        }

        if (!Uri.TryCreate(imageUrlOrPath, UriKind.Absolute, out var imageUri) ||
            !imageUri.Host.Contains("cloudinary.com", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var publicId = TryDeriveCloudinaryPublicId(imageUri);
        if (string.IsNullOrWhiteSpace(publicId))
        {
            _logger.LogWarning(
                "Skipping Cloudinary delete for category {Category}. Could not safely derive public_id from URL '{ImageUrl}'.",
                category,
                imageUrlOrPath);
            return;
        }

        if (!TryCreateCloudinaryClient(out var cloudinary))
        {
            _logger.LogWarning(
                "Skipping Cloudinary delete for category {Category} because CloudinarySettings are incomplete. HasCloudName={HasCloudName}, HasApiKey={HasApiKey}, HasApiSecret={HasApiSecret}",
                category,
                HasConfiguredValue(_settings.CloudName),
                HasConfiguredValue(_settings.ApiKey),
                HasConfiguredValue(_settings.ApiSecret));
            return;
        }

        try
        {
            var deletionResult = await cloudinary.DestroyAsync(new DeletionParams(publicId)
            {
                Invalidate = true
            });

            if (deletionResult.Error is not null)
            {
                _logger.LogWarning(
                    "Cloudinary delete failed for category {Category}, public_id '{PublicId}'. Message={Message}",
                    category,
                    publicId,
                    deletionResult.Error.Message);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Cloudinary delete threw for category {Category}, public_id '{PublicId}'.",
                category,
                publicId);
        }
    }

    private async Task<string> UploadLocallyAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uploadsFolder = GetLocalPhysicalFolder(category);
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = CreateSafeFileName(file.FileName, GetFallbackBaseName(category), extension);
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        try
        {
            await using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(fileStream, cancellationToken);
            return $"{GetLocalUrlPrefix(category)}/{uniqueFileName}";
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Local image upload failed for category {Category}.", category);
            throw new InvalidOperationException("Không thể lưu ảnh trên máy chủ. Vui lòng thử lại.", exception);
        }
    }

    private void TryDeleteLocalFile(string imageUrlOrPath, ImageCategory category)
    {
        var expectedPrefix = GetLocalUrlPrefix(category);
        if (!imageUrlOrPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            var trimmedPath = imageUrlOrPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, trimmedPath));
            var uploadsRoot = Path.GetFullPath(GetLocalPhysicalFolder(category));

            if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            {
                return;
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
    }

    private Cloudinary CreateCloudinaryClientOrThrow(ImageCategory category, string folder)
    {
        if (!TryCreateCloudinaryClient(out var cloudinary))
        {
            _logger.LogWarning(
                "Cloudinary configuration is incomplete for category {Category}, folder {Folder}. HasCloudName={HasCloudName}, HasApiKey={HasApiKey}, HasApiSecret={HasApiSecret}",
                category,
                folder,
                HasConfiguredValue(_settings.CloudName),
                HasConfiguredValue(_settings.ApiKey),
                HasConfiguredValue(_settings.ApiSecret));
            throw new InvalidOperationException("Cloudinary chưa được cấu hình đầy đủ. Vui lòng kiểm tra CloudName, ApiKey và ApiSecret.");
        }

        try
        {
            cloudinary.Api.Secure = true;
            return cloudinary;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Cloudinary client initialization failed for category {Category}, folder {Folder}.",
                category,
                folder);
            throw new InvalidOperationException("Không thể khởi tạo kết nối Cloudinary.", exception);
        }
    }

    private bool TryCreateCloudinaryClient([NotNullWhen(true)] out Cloudinary? cloudinary)
    {
        var cloudName = _settings.CloudName?.Trim();
        var apiKey = _settings.ApiKey?.Trim();
        var apiSecret = _settings.ApiSecret?.Trim();

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            cloudinary = null;
            return false;
        }

        cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        return true;
    }

    private string? TryDeriveCloudinaryPublicId(Uri imageUri)
    {
        var uploadMarkerIndex = imageUri.AbsolutePath.IndexOf(CloudinaryUploadPathMarker, StringComparison.OrdinalIgnoreCase);
        if (uploadMarkerIndex < 0)
        {
            return null;
        }

        var uploadPath = imageUri.AbsolutePath[(uploadMarkerIndex + CloudinaryUploadPathMarker.Length)..];
        var rawSegments = uploadPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        if (rawSegments.Length == 0)
        {
            return null;
        }

        var prefixSegments = GetResolvedFolderPrefix()
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var prefixIndex = FindSegmentSequence(rawSegments, prefixSegments);
        if (prefixIndex < 0)
        {
            return null;
        }

        var publicIdSegments = rawSegments[prefixIndex..].ToArray();
        if (publicIdSegments.Length == 0)
        {
            return null;
        }

        // Phase 1 compatibility: the database stores only the delivery URL today.
        // Deriving public_id from the URL is conservative and limited to our configured folder prefix.
        publicIdSegments[^1] = Path.GetFileNameWithoutExtension(publicIdSegments[^1]);
        if (string.IsNullOrWhiteSpace(publicIdSegments[^1]))
        {
            return null;
        }

        var publicId = string.Join('/', publicIdSegments).Trim('/');
        var folderPrefix = GetResolvedFolderPrefix();
        return publicId.StartsWith($"{folderPrefix}/", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(publicId, folderPrefix, StringComparison.OrdinalIgnoreCase)
            ? publicId
            : null;
    }

    private string GetCloudinaryFolder(ImageCategory category)
    {
        var folderPrefix = GetResolvedFolderPrefix();

        return category switch
        {
            ImageCategory.Branches => $"{folderPrefix}/branches",
            ImageCategory.Posts => $"{folderPrefix}/posts",
            ImageCategory.MenuPages => $"{folderPrefix}/menu-pages",
            ImageCategory.MenuGroupCovers => throw new InvalidOperationException("Menu group covers stay on local storage."),
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unsupported image category.")
        };
    }

    private string GetLocalPhysicalFolder(ImageCategory category)
    {
        var relativeFolder = GetLocalRelativeFolder(category).Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_environment.WebRootPath, relativeFolder);
    }

    private static string GetLocalUrlPrefix(ImageCategory category)
    {
        return category switch
        {
            ImageCategory.Branches => "/uploads/branches",
            ImageCategory.Posts => "/uploads/posts",
            ImageCategory.MenuPages => "/uploads/menu-pages",
            ImageCategory.MenuGroupCovers => "/uploads/menu-groups/covers",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unsupported image category.")
        };
    }

    private static string GetLocalRelativeFolder(ImageCategory category)
    {
        return category switch
        {
            ImageCategory.Branches => "uploads/branches",
            ImageCategory.Posts => "uploads/posts",
            ImageCategory.MenuPages => "uploads/menu-pages",
            ImageCategory.MenuGroupCovers => "uploads/menu-groups/covers",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unsupported image category.")
        };
    }

    private static string GetFallbackBaseName(ImageCategory category)
    {
        return category switch
        {
            ImageCategory.Branches => "branch",
            ImageCategory.Posts => "post",
            ImageCategory.MenuPages => "menu-page",
            ImageCategory.MenuGroupCovers => "menu-group-cover",
            _ => "image"
        };
    }

    private string GetResolvedFolderPrefix()
    {
        return string.IsNullOrWhiteSpace(_settings.FolderPrefix)
            ? DefaultFolderPrefix
            : _settings.FolderPrefix.Trim().Trim('/');
    }

    private static int FindSegmentSequence(IReadOnlyList<string> segments, IReadOnlyList<string> sequence)
    {
        if (segments.Count == 0 || sequence.Count == 0 || sequence.Count > segments.Count)
        {
            return -1;
        }

        for (var index = 0; index <= segments.Count - sequence.Count; index++)
        {
            var matches = true;
            for (var offset = 0; offset < sequence.Count; offset++)
            {
                if (!string.Equals(segments[index + offset], sequence[offset], StringComparison.OrdinalIgnoreCase))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool HasConfiguredValue(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string CreateSafeFileName(string originalFileName, string fallbackBaseName, string extension)
    {
        var safeBaseName = NormalizeSlugInput(Path.GetFileNameWithoutExtension(originalFileName));
        if (string.IsNullOrWhiteSpace(safeBaseName))
        {
            safeBaseName = fallbackBaseName;
        }

        return $"{safeBaseName}-{Guid.NewGuid():N}{extension}";
    }

    private static string NormalizeSlugInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        normalized = builder.ToString().Normalize(NormalizationForm.FormC);
        normalized = normalized.Replace('đ', 'd');
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", string.Empty);
        normalized = Regex.Replace(normalized, @"[\s-]+", "-").Trim('-');

        return normalized;
    }
}

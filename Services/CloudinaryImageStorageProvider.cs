using System.Diagnostics.CodeAnalysis;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using KIGHolding.Options;
using Microsoft.Extensions.Options;

namespace KIGHolding.Services;

public sealed class CloudinaryImageStorageProvider : IImageStorageProvider
{
    private const string DefaultFolderPrefix = "kig-holding";
    private const string CloudinaryUploadPathMarker = "/image/upload/";

    private readonly CloudinarySettings _cloudinarySettings;
    private readonly ImageStorageSettings _imageStorageSettings;
    private readonly ILogger<CloudinaryImageStorageProvider> _logger;
    private readonly ImageUploadValidator _uploadValidator;

    public CloudinaryImageStorageProvider(
        IOptions<CloudinarySettings> cloudinarySettings,
        IOptions<ImageStorageSettings> imageStorageSettings,
        ILogger<CloudinaryImageStorageProvider> logger)
    {
        _cloudinarySettings = cloudinarySettings.Value;
        _imageStorageSettings = imageStorageSettings.Value;
        _logger = logger;
        _uploadValidator = new ImageUploadValidator(_imageStorageSettings);
    }

    public ImageStorageProviderKind Provider => ImageStorageProviderKind.Cloudinary;

    public async Task<string> UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default)
    {
        return await UploadAsync(file, category, storageScope: null, cancellationToken);
    }

    public async Task<string> UploadAsync(IFormFile file, ImageCategory category, string? storageScope, CancellationToken cancellationToken = default)
    {
        ImageStoragePathUtilities.NormalizeOptionalStorageScope(category, storageScope);
        _uploadValidator.Validate(file);

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
                throw new InvalidOperationException("Khong the tai anh len Cloudinary. Vui long thu lai.");
            }

            var secureUrl = result.SecureUrl?.AbsoluteUri;
            if (string.IsNullOrWhiteSpace(secureUrl))
            {
                _logger.LogWarning(
                    "Cloudinary upload for category {Category}, folder {Folder} completed without a secure URL. PublicId={PublicId}",
                    category,
                    folder,
                    result.PublicId);
                throw new InvalidOperationException("Cloudinary khong tra ve URL anh hop le.");
            }

            return secureUrl;
        }
        catch (Exception exception) when (exception is not InvalidOperationException and not OperationCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Cloudinary upload threw for category {Category}, folder {Folder}.",
                category,
                folder);
            throw new InvalidOperationException("Khong the tai anh len Cloudinary. Vui long thu lai.", exception);
        }
    }

    public bool CanDelete(string imageUrlOrPath, ImageCategory category)
    {
        return Uri.TryCreate(imageUrlOrPath, UriKind.Absolute, out var imageUri) &&
               imageUri.Host.Contains("cloudinary.com", StringComparison.OrdinalIgnoreCase) &&
               !string.IsNullOrWhiteSpace(TryDeriveCloudinaryPublicId(imageUri));
    }

    public async Task DeleteAsync(string imageUrlOrPath, ImageCategory category, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(imageUrlOrPath, UriKind.Absolute, out var imageUri) ||
            !imageUri.Host.Contains("cloudinary.com", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var publicId = TryDeriveCloudinaryPublicId(imageUri);
        if (string.IsNullOrWhiteSpace(publicId))
        {
            _logger.LogWarning(
                "Skipping Cloudinary delete for category {Category}. Could not safely derive public_id.",
                category);
            return;
        }

        if (!TryCreateCloudinaryClient(out var cloudinary))
        {
            _logger.LogWarning(
                "Skipping Cloudinary delete for category {Category} because CloudinarySettings are incomplete. HasCloudName={HasCloudName}, HasApiKey={HasApiKey}, HasApiSecret={HasApiSecret}",
                category,
                HasConfiguredValue(_cloudinarySettings.CloudName),
                HasConfiguredValue(_cloudinarySettings.ApiKey),
                HasConfiguredValue(_cloudinarySettings.ApiSecret));
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
                    "Cloudinary delete failed for category {Category}, public_id {PublicId}. Message={Message}",
                    category,
                    publicId,
                    deletionResult.Error.Message);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Cloudinary delete threw for category {Category}, public_id {PublicId}.",
                category,
                publicId);
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
                HasConfiguredValue(_cloudinarySettings.CloudName),
                HasConfiguredValue(_cloudinarySettings.ApiKey),
                HasConfiguredValue(_cloudinarySettings.ApiSecret));
            throw new InvalidOperationException("Cloudinary chua duoc cau hinh day du. Vui long kiem tra CloudName, ApiKey va ApiSecret.");
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
            throw new InvalidOperationException("Khong the khoi tao ket noi Cloudinary.", exception);
        }
    }

    private bool TryCreateCloudinaryClient([NotNullWhen(true)] out Cloudinary? cloudinary)
    {
        var cloudName = _cloudinarySettings.CloudName?.Trim();
        var apiKey = _cloudinarySettings.ApiKey?.Trim();
        var apiSecret = _cloudinarySettings.ApiSecret?.Trim();

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
            ImageCategory.MenuGroupCovers => $"{folderPrefix}/menu-group-covers",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unsupported image category.")
        };
    }

    private string GetResolvedFolderPrefix()
    {
        return string.IsNullOrWhiteSpace(_cloudinarySettings.FolderPrefix)
            ? DefaultFolderPrefix
            : _cloudinarySettings.FolderPrefix.Trim().Trim('/');
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
}

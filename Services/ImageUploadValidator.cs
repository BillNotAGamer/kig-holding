using KIGHolding.Options;

namespace KIGHolding.Services;

public sealed class ImageUploadValidator
{
    private readonly ImageStorageSettings _settings;

    public ImageUploadValidator(ImageStorageSettings settings)
    {
        _settings = settings;
    }

    public ValidatedImageUpload Validate(IFormFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length <= 0 || file.Length > _settings.MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"Tep anh khong hop le hoac vuot qua dung luong toi da ({_settings.MaxFileSizeBytes / 1024 / 1024}MB).");
        }

        var extension = CloudflareR2ObjectKeyBuilder.NormalizeExtension(Path.GetExtension(file.FileName));
        if (_settings.AllowedExtensions is null ||
            !_settings.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Dinh dang tep khong duoc ho tro.");
        }

        if (_settings.AllowedContentTypes is null ||
            !_settings.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Dinh dang noi dung tep khong duoc ho tro.");
        }

        return new ValidatedImageUpload(extension, file.ContentType);
    }
}

public readonly record struct ValidatedImageUpload(string Extension, string ContentType);

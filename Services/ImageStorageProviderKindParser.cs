namespace KIGHolding.Services;

public static class ImageStorageProviderKindParser
{
    public static bool TryParse(string? value, out ImageStorageProviderKind provider)
    {
        var normalized = value?.Trim();

        if (string.Equals(normalized, "LocalVolume", StringComparison.OrdinalIgnoreCase))
        {
            provider = ImageStorageProviderKind.LocalVolume;
            return true;
        }

        if (string.Equals(normalized, "Cloudinary", StringComparison.OrdinalIgnoreCase))
        {
            provider = ImageStorageProviderKind.Cloudinary;
            return true;
        }

        if (string.Equals(normalized, "CloudflareR2", StringComparison.OrdinalIgnoreCase))
        {
            provider = ImageStorageProviderKind.CloudflareR2;
            return true;
        }

        provider = default;
        return false;
    }

    public static ImageStorageProviderKind ParseOrThrow(string? value)
    {
        if (TryParse(value, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException("ImageStorage:Provider must be one of LocalVolume, Cloudinary, or CloudflareR2.");
    }
}

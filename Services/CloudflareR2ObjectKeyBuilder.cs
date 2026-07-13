using KIGHolding.Options;

namespace KIGHolding.Services;

public static class CloudflareR2ObjectKeyBuilder
{
    public static string BuildObjectKey(
        ImageStorageSettings settings,
        ImageCategory category,
        string originalFileName,
        string extension)
    {
        return BuildObjectKey(settings, category, originalFileName, extension, storageScope: null);
    }

    public static string BuildObjectKey(
        ImageStorageSettings settings,
        ImageCategory category,
        string originalFileName,
        string extension,
        string? storageScope)
    {
        var prefix = ImageStoragePathUtilities.BuildR2ObjectPrefix(settings, category, storageScope);

        var normalizedExtension = NormalizeExtension(extension);
        var safeBaseName = ImageStoragePathUtilities.NormalizeSlugInput(Path.GetFileNameWithoutExtension(originalFileName));
        if (string.IsNullOrWhiteSpace(safeBaseName))
        {
            safeBaseName = ImageStoragePathUtilities.GetFallbackBaseName(category);
        }

        var key = $"{prefix}/{safeBaseName}-{Guid.NewGuid():N}{normalizedExtension}";
        if (!ImageStoragePathUtilities.IsSafeObjectKey(key))
        {
            throw new InvalidOperationException("Generated R2 object key is invalid.");
        }

        return key;
    }

    public static string NormalizeExtension(string extension)
    {
        var normalized = (extension ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        return normalized.StartsWith('.') ? normalized : "." + normalized;
    }
}

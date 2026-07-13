using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KIGHolding.Options;

namespace KIGHolding.Services;

public static class ImageStoragePathUtilities
{
    private static readonly Regex StorageScopeSegmentPattern = new(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string NormalizePublicPath(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "/uploads"
            : value.Trim();

        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        return normalized.TrimEnd('/');
    }

    public static string NormalizePublicBaseUrl(string? value)
    {
        return (value ?? string.Empty).Trim().TrimEnd('/');
    }

    public static string NormalizePrefix(string? value)
    {
        return (value ?? string.Empty)
            .Trim()
            .Replace('\\', '/')
            .Trim('/');
    }

    public static bool IsSafePrefix(string? value)
    {
        var prefix = NormalizePrefix(value);
        return !string.IsNullOrWhiteSpace(prefix) && IsSafeObjectKey(prefix);
    }

    public static bool IsSafeObjectKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var key = value.Replace('\\', '/');
        if (key.StartsWith('/') || key.Contains("//", StringComparison.Ordinal))
        {
            return false;
        }

        return key.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .All(segment => segment is not "." and not ".." && !segment.Contains("..", StringComparison.Ordinal));
    }

    public static string GetR2Prefix(ImageStorageSettings settings, ImageCategory category)
    {
        return category switch
        {
            ImageCategory.Branches => NormalizePrefix(settings.BranchesPrefix),
            ImageCategory.Posts => NormalizePrefix(settings.NewsPrefix),
            ImageCategory.MenuPages => NormalizePrefix(settings.MenuPagesPrefix),
            ImageCategory.MenuGroupCovers => NormalizePrefix(settings.BrandsPrefix),
            _ => NormalizePrefix(settings.DefaultPrefix)
        };
    }

    public static string GetLocalCategorySubPath(ImageCategory category)
    {
        return category switch
        {
            ImageCategory.Branches => "branches",
            ImageCategory.Posts => "posts",
            ImageCategory.MenuPages => "menu-pages",
            ImageCategory.MenuGroupCovers => "menu-groups/covers",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unsupported image category.")
        };
    }

    public static string GetLocalCategorySubPath(ImageCategory category, string? storageScope)
    {
        var categoryPath = GetLocalCategorySubPath(category);
        var normalizedScope = NormalizeOptionalStorageScope(category, storageScope);

        return normalizedScope is null
            ? categoryPath
            : $"{categoryPath}/{normalizedScope}";
    }

    public static string BuildR2ObjectPrefix(ImageStorageSettings settings, ImageCategory category, string? storageScope)
    {
        var prefix = GetR2Prefix(settings, category);
        if (!IsSafePrefix(prefix))
        {
            throw new InvalidOperationException("ImageStorage R2 prefix configuration is invalid.");
        }

        var normalizedScope = NormalizeOptionalStorageScope(category, storageScope);
        var scopedPrefix = normalizedScope is null
            ? prefix
            : $"{prefix}/{normalizedScope}";

        if (!IsSafeObjectKey(scopedPrefix))
        {
            throw new InvalidOperationException("Image storage prefix scope is invalid.");
        }

        return scopedPrefix;
    }

    public static string? NormalizeOptionalStorageScope(ImageCategory category, string? storageScope)
    {
        if (storageScope is null)
        {
            return null;
        }

        if (category != ImageCategory.MenuPages)
        {
            throw new InvalidOperationException("Storage scope is only supported for menu page images.");
        }

        return TryNormalizeStorageScopeSegment(storageScope, out var normalizedScope)
            ? normalizedScope
            : throw new InvalidOperationException("Menu group slug is not valid for image storage.");
    }

    public static bool TryNormalizeStorageScopeSegment(string? value, out string normalizedScope)
    {
        normalizedScope = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim();
        if (candidate.StartsWith('/') ||
            candidate.EndsWith('/') ||
            candidate.Contains('/', StringComparison.Ordinal) ||
            candidate.Contains('\\', StringComparison.Ordinal) ||
            candidate.Contains("..", StringComparison.Ordinal) ||
            candidate.Contains('?') ||
            candidate.Contains('#') ||
            candidate.Any(char.IsControl) ||
            Path.IsPathRooted(candidate) ||
            Uri.TryCreate(candidate, UriKind.Absolute, out _))
        {
            return false;
        }

        candidate = candidate.ToLowerInvariant();
        if (!StorageScopeSegmentPattern.IsMatch(candidate))
        {
            return false;
        }

        normalizedScope = candidate;
        return true;
    }

    public static string GetFallbackBaseName(ImageCategory category)
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

    public static string CreateSafeFileName(string originalFileName, ImageCategory category, string extension)
    {
        var safeBaseName = NormalizeSlugInput(Path.GetFileNameWithoutExtension(originalFileName));
        if (string.IsNullOrWhiteSpace(safeBaseName))
        {
            safeBaseName = GetFallbackBaseName(category);
        }

        return $"{safeBaseName}-{Guid.NewGuid():N}{extension}";
    }

    public static string NormalizeSlugInput(string? value)
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
        normalized = normalized.Replace('\u0111', 'd');
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", string.Empty);
        normalized = Regex.Replace(normalized, @"[\s-]+", "-").Trim('-');

        return normalized;
    }
}

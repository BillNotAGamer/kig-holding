using System.Text.RegularExpressions;

namespace KIGHolding.Helpers;

public static class MediaUrlHelper
{
    private const string CloudinaryUploadMarker = "/image/upload/";
    private static readonly string[] TransformationPrefixes =
    [
        "a_",
        "ar_",
        "b_",
        "c_",
        "dpr_",
        "e_",
        "f_",
        "fl_",
        "g_",
        "h_",
        "l_",
        "o_",
        "q_",
        "r_",
        "t_",
        "u_",
        "w_",
        "x_",
        "y_",
        "z_"
    ];

    public static string GetNewsCardImageUrl(string? url)
    {
        return GetOptimizedCloudinaryImageUrl(url, 960);
    }

    public static string GetNewsFeatureImageUrl(string? url)
    {
        return GetOptimizedCloudinaryImageUrl(url, 1440);
    }

    public static string GetNewsHeroImageUrl(string? url)
    {
        return GetOptimizedCloudinaryImageUrl(url, 1600);
    }

    private static string GetOptimizedCloudinaryImageUrl(string? url, int width)
    {
        var normalizedUrl = url?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUrl) || IsSvg(normalizedUrl))
        {
            return normalizedUrl ?? string.Empty;
        }

        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out _) ||
            !TrySplitCloudinaryUrl(normalizedUrl, out var prefix, out var suffix, out var queryString))
        {
            return normalizedUrl;
        }

        var firstSegment = suffix
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstSegment) || LooksLikeTransformationSegment(firstSegment))
        {
            return normalizedUrl;
        }

        var transformation = $"f_auto,q_auto,c_limit,w_{width}";
        return $"{prefix}{transformation}/{suffix}{queryString}";
    }

    private static bool TrySplitCloudinaryUrl(string url, out string prefix, out string suffix, out string queryString)
    {
        prefix = string.Empty;
        suffix = string.Empty;
        queryString = string.Empty;

        var markerIndex = url.IndexOf(CloudinaryUploadMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return false;
        }

        var queryIndex = url.IndexOf('?', markerIndex);
        var path = queryIndex >= 0 ? url[..queryIndex] : url;
        queryString = queryIndex >= 0 ? url[queryIndex..] : string.Empty;

        var suffixStart = markerIndex + CloudinaryUploadMarker.Length;
        if (suffixStart >= path.Length)
        {
            return false;
        }

        prefix = path[..suffixStart];
        suffix = path[suffixStart..];
        return !string.IsNullOrWhiteSpace(suffix);
    }

    private static bool LooksLikeTransformationSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment) || Regex.IsMatch(segment, "^v\\d+$", RegexOptions.IgnoreCase))
        {
            return false;
        }

        return segment
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(part => TransformationPrefixes.Any(prefix =>
                part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsSvg(string url)
    {
        var queryIndex = url.IndexOf('?');
        var path = queryIndex >= 0 ? url[..queryIndex] : url;
        return path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
    }
}

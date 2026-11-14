using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace KIGHolding.Helpers;

/// <summary>
/// Builds absolute public URLs for canonical links, Open Graph tags, sitemap and robots.txt.
/// </summary>
public static class SeoUrlHelper
{
    /// <summary>
    /// Returns the public site origin: AppSettings:AppBaseUrl when configured, otherwise the
    /// request host. Behind a TLS-terminating proxy (Railway) the request scheme is http, so the
    /// X-Forwarded-Proto header is honoured.
    /// </summary>
    public static string GetBaseUrl(IConfiguration configuration, HttpRequest request)
    {
        var configuredBaseUrl = configuration["AppSettings:AppBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return configuredBaseUrl.Trim().TrimEnd('/');
        }

        var forwardedProto = request.Headers["X-Forwarded-Proto"].ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        var scheme = string.Equals(forwardedProto, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            ? Uri.UriSchemeHttps
            : request.Scheme;

        return $"{scheme}://{request.Host}".TrimEnd('/');
    }

    /// <summary>
    /// Turns a site-relative path into an absolute URL on <paramref name="baseUrl"/>.
    /// Only http(s) URLs are treated as already absolute: on Linux, Uri.TryCreate("/path",
    /// UriKind.Absolute) succeeds and yields "file:///path", which previously leaked into
    /// canonical links, og:image and the sitemap.
    /// </summary>
    public static string BuildAbsoluteUrl(string? url, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return baseUrl;
        }

        var trimmedUrl = url.Trim();

        if (IsHttpUrl(trimmedUrl))
        {
            return new Uri(trimmedUrl).ToString();
        }

        if (trimmedUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return $"https:{trimmedUrl}";
        }

        var relativeUrl = trimmedUrl.TrimStart('~');
        relativeUrl = relativeUrl.StartsWith('/') ? relativeUrl : $"/{relativeUrl}";
        return $"{baseUrl.TrimEnd('/')}{relativeUrl}";
    }

    private static bool IsHttpUrl(string url)
    {
        return (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace KIGHolding.Helpers;

/// <summary>
/// Builds absolute public URLs for canonical links, Open Graph tags, sitemap and robots.txt.
/// </summary>
public static class SeoUrlHelper
{
    /// <summary>Public production origin used when a request arrives on a platform-default host.</summary>
    public const string DefaultPublicSiteUrl = "https://www.kigholding.vn";

    /// <summary>
    /// Returns the public site origin used in canonical links, Open Graph tags and the sitemap:
    /// 1. AppSettings:CanonicalBaseUrl when configured;
    /// 2. AppSettings:AppBaseUrl when configured, unless it is a platform-default *.railway.app host;
    /// 3. <see cref="DefaultPublicSiteUrl"/> for requests that arrive on a *.railway.app host;
    /// 4. otherwise the request host, over https unless it is a local development host.
    /// </summary>
    public static string GetBaseUrl(IConfiguration configuration, HttpRequest request)
    {
        var canonicalBaseUrl = configuration["AppSettings:CanonicalBaseUrl"];
        if (!string.IsNullOrWhiteSpace(canonicalBaseUrl))
        {
            return canonicalBaseUrl.Trim().TrimEnd('/');
        }

        var configuredBaseUrl = configuration["AppSettings:AppBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl) && !IsPlatformDefaultHost(configuredBaseUrl))
        {
            return configuredBaseUrl.Trim().TrimEnd('/');
        }

        var host = request.Host.Host;
        if (string.IsNullOrWhiteSpace(host) || IsPlatformDefaultHost(host))
        {
            return DefaultPublicSiteUrl;
        }

        // Railway terminates TLS at its proxy, so request.Scheme is http in production.
        var scheme = IsLocalHost(host) ? request.Scheme : Uri.UriSchemeHttps;
        return $"{scheme}://{request.Host}".TrimEnd('/');
    }

    private static bool IsPlatformDefaultHost(string hostOrUrl)
    {
        var host = Uri.TryCreate(hostOrUrl.Trim(), UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.Host
            : hostOrUrl.Trim();

        return host.EndsWith(".railway.app", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLocalHost(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || host == "127.0.0.1"
            || host == "::1"
            || host == "[::1]";
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

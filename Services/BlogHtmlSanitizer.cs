using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Ganss.Xss;

namespace KIGHolding.Services;

public sealed class BlogHtmlSanitizer : IBlogHtmlSanitizer
{
    private static readonly string[] AllowedTags =
    [
        "p",
        "br",
        "h2",
        "h3",
        "h4",
        "strong",
        "b",
        "em",
        "i",
        "u",
        "ul",
        "ol",
        "li",
        "blockquote",
        "a",
        "img",
        "figure",
        "figcaption",
        "table",
        "thead",
        "tbody",
        "tr",
        "th",
        "td",
        "pre",
        "code",
        "hr",
        "span",
        "div"
    ];

    private static readonly string[] AllowedAttributes =
    [
        "href",
        "title",
        "target",
        "rel",
        "src",
        "alt",
        "width",
        "height",
        "loading"
    ];

    private static readonly string[] AllowedSchemes =
    [
        "http",
        "https",
        "mailto",
        "tel"
    ];

    public string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var sanitizer = CreateSanitizer();
        var sanitizedHtml = sanitizer.Sanitize(html);

        return EnforceUrlPolicy(sanitizedHtml);
    }

    public bool ContainsMeaningfulContent(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return false;
        }

        var body = ParseBody(html);
        if (body is null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(body.TextContent)
            || body.QuerySelector("img[src]") is not null
            || body.QuerySelector("hr") is not null;
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedTags.Clear();
        foreach (var tag in AllowedTags)
        {
            sanitizer.AllowedTags.Add(tag);
        }

        sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in AllowedAttributes)
        {
            sanitizer.AllowedAttributes.Add(attribute);
        }

        sanitizer.AllowedSchemes.Clear();
        foreach (var scheme in AllowedSchemes)
        {
            sanitizer.AllowedSchemes.Add(scheme);
        }

        sanitizer.UriAttributes.Clear();
        sanitizer.UriAttributes.Add("href");
        sanitizer.UriAttributes.Add("src");

        sanitizer.AllowDataAttributes = false;
        sanitizer.AllowedCssProperties.Clear();

        return sanitizer;
    }

    private static string EnforceUrlPolicy(string html)
    {
        var body = ParseBody(html);
        if (body is null)
        {
            return string.Empty;
        }

        foreach (var anchor in body.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href");
            if (!IsSafeLinkUrl(href))
            {
                anchor.RemoveAttribute("href");
            }

            if (string.Equals(anchor.GetAttribute("target"), "_blank", StringComparison.OrdinalIgnoreCase))
            {
                EnsureBlankTargetRel(anchor);
            }
        }

        foreach (var image in body.QuerySelectorAll("img[src]"))
        {
            var src = image.GetAttribute("src");
            if (!IsSafeImageUrl(src))
            {
                image.RemoveAttribute("src");
            }
        }

        return body.InnerHtml;
    }

    private static IElement? ParseBody(string html)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument($"<body>{html}</body>");
        return document.Body;
    }

    private static void EnsureBlankTargetRel(IElement anchor)
    {
        var relValues = (anchor.GetAttribute("rel") ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        relValues.Remove("opener");
        relValues.Add("noopener");
        relValues.Add("noreferrer");
        anchor.SetAttribute("rel", string.Join(' ', relValues.Order(StringComparer.OrdinalIgnoreCase)));
    }

    private static bool IsSafeLinkUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmedUrl = url.Trim();
        if (IsSafeRelativeUrl(trimmedUrl))
        {
            return true;
        }

        return Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https" or "mailto" or "tel";
    }

    private static bool IsSafeImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmedUrl = url.Trim();
        if (IsSafeRelativeUrl(trimmedUrl))
        {
            return true;
        }

        return Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps;
    }

    private static bool IsSafeRelativeUrl(string url)
    {
        if (url.StartsWith("//", StringComparison.Ordinal) ||
            url.Contains("\\", StringComparison.Ordinal))
        {
            return false;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return false;
        }

        return url.StartsWith("#", StringComparison.Ordinal)
            || url.StartsWith("/", StringComparison.Ordinal)
            || (!url.Contains(":", StringComparison.Ordinal)
                && Uri.TryCreate(url, UriKind.Relative, out _));
    }
}

using KIGHolding.Services;

namespace KIGHolding.Tests;

public sealed class BlogHtmlSanitizerTests
{
    private readonly BlogHtmlSanitizer _sanitizer = new();

    [Fact]
    public void Sanitize_PreservesAllowedArticleMarkup()
    {
        var html = "<h2>Heading</h2><p>Hello <strong>world</strong>.</p><ul><li>One</li></ul>";

        var sanitized = _sanitizer.Sanitize(html);

        Assert.Contains("<h2>Heading</h2>", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Hello <strong>world</strong>.</p>", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<ul><li>One</li></ul>", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_RemovesScriptAndPreservesSafeContent()
    {
        var sanitized = _sanitizer.Sanitize("<script>alert(1)</script><p>Safe</p>");

        Assert.DoesNotContain("<script", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert(1)", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Safe</p>", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_RemovesEventHandlers()
    {
        var sanitized = _sanitizer.Sanitize("<img src=\"https://example.com/a.jpg\" onerror=\"alert(1)\">");

        Assert.Contains("<img", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("src=\"https://example.com/a.jpg\"", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_RemovesDataAttributes()
    {
        var sanitized = _sanitizer.Sanitize("<p data-track=\"x\">Safe</p>");

        Assert.Contains("<p>Safe</p>", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data-track", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_RemovesJavaScriptUrls()
    {
        var sanitized = _sanitizer.Sanitize("<a href=\"javascript:alert(1)\">Click</a>");

        Assert.Contains(">Click</a>", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<a href=\"JaVaScRiPt:alert(1)\">Click</a>")]
    [InlineData("<a href=\" javaScript:alert(1)\">Click</a>")]
    public void Sanitize_RemovesObfuscatedJavaScriptUrls(string html)
    {
        var sanitized = _sanitizer.Sanitize(html);

        Assert.Equal("<a>Click</a>", sanitized);
        Assert.DoesNotContain("javascript:", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_RemovesProtocolRelativeUrls()
    {
        var sanitized = _sanitizer.Sanitize("<a href=\"//evil.example.com/x\">Link</a><img src=\"//evil.example.com/a.jpg\" alt=\"Image\">");

        Assert.Contains("<a>Link</a>", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<img", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("src=", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("//evil.example.com", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_PreservesSafeRelativeUrls()
    {
        var sanitized = _sanitizer.Sanitize("<a href=\"/tin-tuc/demo\">Link</a><img src=\"/media/a.jpg\" alt=\"Image\">");

        Assert.Contains("href=\"/tin-tuc/demo\"", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("src=\"/media/a.jpg\"", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_RemovesFormControls()
    {
        var sanitized = _sanitizer.Sanitize("<form><input name=\"x\"></form>");

        Assert.DoesNotContain("<form", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<input", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.False(_sanitizer.ContainsMeaningfulContent(sanitized));
    }

    [Fact]
    public void Sanitize_RemovesSvgContent()
    {
        var sanitized = _sanitizer.Sanitize("<svg><script>alert(1)</script><circle></circle></svg>");

        Assert.DoesNotContain("<svg", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<circle", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_RemovesDataImageSource()
    {
        var sanitized = _sanitizer.Sanitize("<img src=\"data:image/png;base64,AAAA\" alt=\"Image\">");

        Assert.Contains("<img", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data:image", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("src=", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_PreservesSafeHttpsImage()
    {
        var sanitized = _sanitizer.Sanitize("<img src=\"https://example.com/image.jpg\" alt=\"Image\">");

        Assert.Contains("<img", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("src=\"https://example.com/image.jpg\"", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("alt=\"Image\"", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_AddsSafeRelForBlankTargets()
    {
        var sanitized = _sanitizer.Sanitize("<a href=\"https://example.com\" target=\"_blank\">Open</a>");

        Assert.Contains("target=\"_blank\"", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rel=\"noopener noreferrer\"", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_RemovesOpenerRelForBlankTargets()
    {
        var sanitized = _sanitizer.Sanitize("<a href=\"https://example.com\" target=\"_blank\" rel=\"opener\">Example</a>");

        Assert.Equal("<a href=\"https://example.com\" target=\"_blank\" rel=\"noopener noreferrer\">Example</a>", sanitized);
        Assert.DoesNotContain("opener", GetRelTokens(sanitized), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_RemovesMixedCaseOpenerRelAndPreservesSafeTokens()
    {
        var sanitized = _sanitizer.Sanitize("<a href=\"https://example.com\" target=\"_blank\" rel=\"OpEnEr nofollow sponsored\">Example</a>");

        Assert.Equal("<a href=\"https://example.com\" target=\"_blank\" rel=\"nofollow noopener noreferrer sponsored\">Example</a>", sanitized);
        Assert.DoesNotContain("opener", GetRelTokens(sanitized), StringComparer.OrdinalIgnoreCase);
        Assert.Contains("nofollow", GetRelTokens(sanitized), StringComparer.OrdinalIgnoreCase);
        Assert.Contains("sponsored", GetRelTokens(sanitized), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_PreservesSafeRelAndPreventsDuplicateTokens()
    {
        var sanitized = _sanitizer.Sanitize("<a href=\"https://example.com\" target=\"_blank\" rel=\"nofollow noopener nofollow noreferrer opener\">Example</a>");

        Assert.Equal("<a href=\"https://example.com\" target=\"_blank\" rel=\"nofollow noopener noreferrer\">Example</a>", sanitized);
    }

    [Fact]
    public void Sanitize_IsIdempotentForRepresentativeArticleHtml()
    {
        var html = "<h2>Heading</h2><p>Hello <strong>world</strong>.</p><a href=\"https://example.com\" target=\"_blank\">Link</a><img src=\"https://example.com/image.jpg\" alt=\"Image\">";

        var sanitized = _sanitizer.Sanitize(html);
        var sanitizedAgain = _sanitizer.Sanitize(sanitized);

        Assert.Equal(sanitized, sanitizedAgain);
    }

    [Fact]
    public void ContainsMeaningfulContent_AllowsSafeImageOnlyHtml()
    {
        var sanitized = _sanitizer.Sanitize("<img src=\"https://example.com/a.jpg\" alt=\"Image\">");

        Assert.True(_sanitizer.ContainsMeaningfulContent(sanitized));
    }

    private static string[] GetRelTokens(string html)
    {
        const string marker = "rel=\"";
        var start = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        Assert.True(start >= 0, "Expected a rel attribute.");
        start += marker.Length;
        var end = html.IndexOf('"', start);
        Assert.True(end > start, "Expected a rel attribute value.");
        return html[start..end].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

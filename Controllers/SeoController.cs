using System.Text;
using System.Xml;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

public class SeoController : Controller
{
    private static readonly string[] StaticRoutes =
    [
        "/",
        "/gioi-thieu",
        "/thuc-don",
        "/dat-ban",
        "/chi-nhanh",
        "/tin-tuc",
        "/lien-he"
    ];

    private readonly IMenuService _menuService;
    private readonly INewsService _newsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SeoController> _logger;

    public SeoController(
        IMenuService menuService,
        INewsService newsService,
        IConfiguration configuration,
        ILogger<SeoController> logger)
    {
        _menuService = menuService;
        _newsService = newsService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var baseUrl = GetBaseUrl();
        var urls = StaticRoutes
            .Select(route => new SitemapUrl(BuildAbsoluteUrl(route, baseUrl), null, "weekly", "0.8"))
            .ToList();

        urls[0] = urls[0] with { ChangeFrequency = "daily", Priority = "1.0" };

        if (HasConfiguredDatabase())
        {
            var menuItems = await TryLoadAsync(
                () => _menuService.GetActiveMenuItemsAsync(null, cancellationToken),
                "sitemap menu items") ?? [];

            urls.AddRange(menuItems.Select(item => new SitemapUrl(
                BuildAbsoluteUrl($"/thuc-don/{item.Slug}", baseUrl),
                item.UpdatedAt,
                "weekly",
                "0.7")));

            var posts = await TryLoadAsync(
                () => _newsService.GetPublishedPostsAsync(null, cancellationToken),
                "sitemap posts") ?? [];

            urls.AddRange(posts.Select(post => new SitemapUrl(
                BuildAbsoluteUrl($"/tin-tuc/{post.Slug}", baseUrl),
                post.UpdatedAt,
                "weekly",
                "0.6")));
        }

        var xml = BuildSitemapXml(urls);
        return Content(xml, "application/xml; charset=utf-8");
    }

    [HttpGet("/robots.txt")]
    public IActionResult Robots()
    {
        var baseUrl = GetBaseUrl();
        var content = new StringBuilder()
            .AppendLine("User-agent: *")
            .AppendLine("Allow: /")
            .AppendLine($"Sitemap: {BuildAbsoluteUrl("/sitemap.xml", baseUrl)}")
            .ToString();

        return Content(content, "text/plain; charset=utf-8");
    }

    private async Task<T?> TryLoadAsync<T>(Func<Task<T>> loader, string dataName)
    {
        try
        {
            return await loader();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load {DataName}.", dataName);
            return default;
        }
    }

    private string GetBaseUrl()
    {
        var configuredBaseUrl = _configuration["AppSettings:AppBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return configuredBaseUrl.TrimEnd('/');
        }

        return $"{Request.Scheme}://{Request.Host}".TrimEnd('/');
    }

    private bool HasConfiguredDatabase()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        return !string.IsNullOrWhiteSpace(connectionString)
            && !connectionString.Contains("your-neon-host", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("your_username", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("your_password", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildAbsoluteUrl(string route, string baseUrl)
    {
        if (Uri.TryCreate(route, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        var relativeRoute = route.StartsWith('/') ? route : $"/{route}";
        return $"{baseUrl.TrimEnd('/')}{relativeRoute}";
    }

    private static string BuildSitemapXml(IReadOnlyList<SitemapUrl> urls)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            OmitXmlDeclaration = false
        };

        using var stringWriter = new Utf8StringWriter();
        using var writer = XmlWriter.Create(stringWriter, settings);
        writer.WriteStartDocument();
        writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

        foreach (var url in urls)
        {
            writer.WriteStartElement("url");
            writer.WriteElementString("loc", url.Location);

            if (url.LastModified.HasValue)
            {
                writer.WriteElementString("lastmod", url.LastModified.Value.UtcDateTime.ToString("yyyy-MM-dd"));
            }

            writer.WriteElementString("changefreq", url.ChangeFrequency);
            writer.WriteElementString("priority", url.Priority);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Flush();

        return stringWriter.ToString();
    }

    private sealed record SitemapUrl(string Location, DateTimeOffset? LastModified, string ChangeFrequency, string Priority);

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}

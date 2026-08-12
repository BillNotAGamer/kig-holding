using System.Globalization;
using System.Text.Json;
using KIGHolding.Models;
using KIGHolding.Models.Content;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

[Route("tin-tuc")]
public class NewsController : Controller
{
    private const int DefaultPageSize = 9;

    private readonly INewsService _newsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NewsController> _logger;

    public NewsController(
        INewsService newsService,
        IConfiguration configuration,
        ILogger<NewsController> logger)
    {
        _newsService = newsService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] string? category, [FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        var selectedCategorySlug = NewsCategories.NormalizeCategory(category);
        var postsPage = new PagedResult<PostCardViewModel>
        {
            Page = Math.Max(1, page),
            PageSize = DefaultPageSize
        };

        if (HasConfiguredDatabase())
        {
            postsPage = await TryLoadAsync(
                () => _newsService.GetPublishedPostCardsPageAsync(selectedCategorySlug, page, DefaultPageSize, cancellationToken),
                "published posts") ?? postsPage;
        }

        var selectedCategoryName = NewsCategories.GetDisplayName(selectedCategorySlug);
        var featuredPost = postsPage.Items.FirstOrDefault();
        var model = new NewsIndexViewModel
        {
            Categories = NewsCategories.All
                .Select(x => new NewsCategoryViewModel
                {
                    Slug = x.Slug,
                    Name = x.DisplayName,
                    IsActive = string.Equals(x.Slug, selectedCategorySlug, StringComparison.OrdinalIgnoreCase)
                })
                .ToList(),
            SelectedCategory = selectedCategorySlug,
            SelectedCategorySlug = selectedCategorySlug,
            SelectedCategoryName = string.IsNullOrWhiteSpace(selectedCategorySlug) ? null : selectedCategoryName,
            FeaturedPost = featuredPost,
            Posts = postsPage.Items,
            Page = postsPage.Page,
            PageSize = postsPage.PageSize,
            TotalItems = postsPage.TotalItems,
            TotalPages = postsPage.TotalPages,
            HasPreviousPage = postsPage.HasPreviousPage,
            HasNextPage = postsPage.HasNextPage,
            SeoTitle = string.IsNullOrWhiteSpace(selectedCategoryName) ? "Tin tức & ưu đãi" : $"{selectedCategoryName} - Tin tức",
            SeoDescription = string.IsNullOrWhiteSpace(selectedCategoryName)
                ? "Cập nhật món mới, khuyến mãi và câu chuyện ẩm thực Hàn Quốc tại Truyền Thuyết Champong."
                : $"Cập nhật {selectedCategoryName} mới nhất từ Truyền Thuyết Champong."
        };

        return View(model);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
    {
        if (!HasConfiguredDatabase())
        {
            return NotFound();
        }

        var normalizedSlug = NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return NotFound();
        }

        var post = await TryLoadAsync(
            () => _newsService.GetPostBySlugAsync(normalizedSlug, cancellationToken),
            "post detail");

        if (post is null)
        {
            return NotFound();
        }

        var relatedPosts = await TryLoadAsync(
            () => _newsService.GetRelatedPostCardsAsync(post.Category, post.Id, 3, cancellationToken),
            "related posts") ?? [];

        if (relatedPosts.Count < 3)
        {
            var latestPosts = await TryLoadAsync(
                () => _newsService.GetPublishedPostCardsAsync(6, cancellationToken),
                "latest fallback posts") ?? [];

            relatedPosts = relatedPosts
                .Concat(latestPosts.Where(x => x.Id != post.Id && relatedPosts.All(existing => existing.Id != x.Id)))
                .Take(3)
                .ToList();
        }

        var seoTitle = FirstNonWhiteSpace(post.SeoTitle, post.Title);
        var seoDescription = FirstNonWhiteSpace(post.SeoDescription, post.Excerpt);
        var ogTitle = FirstNonWhiteSpace(post.OgTitle, post.SeoTitle, post.Title);
        var ogDescription = FirstNonWhiteSpace(post.OgDescription, post.SeoDescription, post.Excerpt);
        var ogImageUrl = FirstNonWhiteSpace(post.OgImageUrl, post.ThumbnailUrl, "/images/placeholders/post-card.webp");
        var canonicalUrl = FirstNonWhiteSpace(post.CanonicalUrl, $"/tin-tuc/{post.Slug}");
        var articlePublishedTime = post.PublishedAt?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        var articleModifiedTime = post.UpdatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        var robotsContent = $"{(post.RobotsIndex ? "index" : "noindex")},{(post.RobotsFollow ? "follow" : "nofollow")}";
        var baseUrl = GetBaseUrl();
        var absoluteCanonicalUrl = BuildAbsoluteUrl(canonicalUrl, baseUrl);
        var absoluteOgImageUrl = BuildAbsoluteUrl(ogImageUrl, baseUrl);
        var articleJsonLd = BuildArticleJsonLd(
            seoTitle,
            seoDescription,
            absoluteCanonicalUrl,
            absoluteOgImageUrl,
            articlePublishedTime,
            articleModifiedTime);

        ViewData["Title"] = seoTitle;
        ViewData["MetaDescription"] = seoDescription;
        ViewData["CanonicalUrl"] = canonicalUrl;
        ViewData["OgTitle"] = ogTitle;
        ViewData["OgDescription"] = ogDescription;
        ViewData["OgImage"] = ogImageUrl;
        ViewData["OgType"] = "article";
        ViewData["Robots"] = robotsContent;
        ViewData["ArticlePublishedTime"] = articlePublishedTime;
        ViewData["ArticleModifiedTime"] = articleModifiedTime;

        var model = new NewsDetailViewModel
        {
            Post = post,
            CategorySlug = NewsCategories.NormalizeCategory(post.Category) ?? string.Empty,
            CategoryDisplayName = NewsCategories.GetDisplayName(post.Category),
            IsPromotion = string.Equals(NewsCategories.NormalizeCategory(post.Category), NewsCategories.KhuyenMaiUuDai, StringComparison.OrdinalIgnoreCase),
            RelatedPosts = relatedPosts,
            SeoTitle = seoTitle,
            SeoDescription = seoDescription,
            OgTitle = ogTitle,
            OgDescription = ogDescription,
            OgImageUrl = ogImageUrl,
            CanonicalUrl = canonicalUrl,
            RobotsContent = robotsContent,
            ArticlePublishedTime = articlePublishedTime,
            ArticleModifiedTime = articleModifiedTime,
            ArticleJsonLd = articleJsonLd
        };

        return View(model);
    }

    private async Task<T?> TryLoadAsync<T>(Func<Task<T>> loader, string dataName)
    {
        try
        {
            return await loader();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load {DataName} for news page.", dataName);
            return default;
        }
    }

    private bool HasConfiguredDatabase()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        return !string.IsNullOrWhiteSpace(connectionString)
            && !connectionString.Contains("your-neon-host", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("your_username", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("your_password", StringComparison.OrdinalIgnoreCase);
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

    private static string BuildAbsoluteUrl(string? url, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return baseUrl;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        var relativeUrl = url.StartsWith('/') ? url : $"/{url}";
        return $"{baseUrl.TrimEnd('/')}{relativeUrl}";
    }

    private static string FirstNonWhiteSpace(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static string BuildArticleJsonLd(
        string headline,
        string description,
        string canonicalUrl,
        string imageUrl,
        string? publishedTime,
        string modifiedTime)
    {
        var article = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Article",
            ["headline"] = headline,
            ["description"] = description,
            ["url"] = canonicalUrl,
            ["mainEntityOfPage"] = canonicalUrl,
            ["image"] = imageUrl,
            ["dateModified"] = modifiedTime
        };

        if (!string.IsNullOrWhiteSpace(publishedTime))
        {
            article["datePublished"] = publishedTime;
        }

        return JsonSerializer.Serialize(article);
    }

    private static string? NormalizeSlug(string? slug)
    {
        return string.IsNullOrWhiteSpace(slug)
            ? null
            : slug.Trim().ToLowerInvariant();
    }
}

using KIGHolding.Models.Entities;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

[Route("tin-tuc")]
public class NewsController : Controller
{
    private static readonly string[] SupportedCategories =
    [
        "Khuyến mãi",
        "Món mới",
        "Sự kiện",
        "Câu chuyện ẩm thực"
    ];

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
    public async Task<IActionResult> Index([FromQuery] string? category, CancellationToken cancellationToken)
    {
        var selectedCategory = ResolveCategory(category);
        IReadOnlyList<Post> posts = [];

        if (HasConfiguredDatabase())
        {
            posts = await TryLoadAsync(
                () => _newsService.GetPublishedPostsByCategoryAsync(selectedCategory, null, cancellationToken),
                "published posts") ?? [];
        }

        var featuredPost = posts.FirstOrDefault();
        var model = new NewsIndexViewModel
        {
            Categories = SupportedCategories
                .Select(x => new NewsCategoryViewModel
                {
                    Name = x,
                    IsActive = string.Equals(x, selectedCategory, StringComparison.OrdinalIgnoreCase)
                })
                .ToList(),
            SelectedCategory = selectedCategory,
            FeaturedPost = featuredPost is null ? null : PostCardViewModel.FromPost(featuredPost),
            Posts = posts.Select(PostCardViewModel.FromPost).ToList(),
            SeoTitle = string.IsNullOrWhiteSpace(selectedCategory) ? "Tin tức & ưu đãi" : $"{selectedCategory} - Tin tức",
            SeoDescription = string.IsNullOrWhiteSpace(selectedCategory)
                ? "Cập nhật món mới, khuyến mãi và câu chuyện ẩm thực Hàn Quốc tại Truyền Thuyết Champong."
                : $"Cập nhật {selectedCategory.ToLowerInvariant()} mới nhất từ Truyền Thuyết Champong."
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
            () => _newsService.GetRelatedPostsAsync(post.Category, post.Id, 3, cancellationToken),
            "related posts") ?? [];

        if (relatedPosts.Count < 3)
        {
            var latestPosts = await TryLoadAsync(
                () => _newsService.GetPublishedPostsAsync(6, cancellationToken),
                "latest fallback posts") ?? [];

            relatedPosts = relatedPosts
                .Concat(latestPosts.Where(x => x.Id != post.Id && relatedPosts.All(existing => existing.Id != x.Id)))
                .Take(3)
                .ToList();
        }

        var model = new NewsDetailViewModel
        {
            Post = post,
            RelatedPosts = relatedPosts.Select(PostCardViewModel.FromPost).ToList(),
            SeoTitle = string.IsNullOrWhiteSpace(post.SeoTitle) ? post.Title : post.SeoTitle,
            SeoDescription = string.IsNullOrWhiteSpace(post.SeoDescription) ? post.Excerpt : post.SeoDescription
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

    private static string? ResolveCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        var normalizedCategory = category.Trim();
        return SupportedCategories.FirstOrDefault(x => string.Equals(x, normalizedCategory, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeSlug(string? slug)
    {
        return string.IsNullOrWhiteSpace(slug)
            ? null
            : slug.Trim().ToLowerInvariant();
    }
}

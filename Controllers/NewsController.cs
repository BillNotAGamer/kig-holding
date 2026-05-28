using KIGHolding.Models.Entities;
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
        var postsPage = new PagedResult<Post>
        {
            Page = Math.Max(1, page),
            PageSize = DefaultPageSize
        };

        if (HasConfiguredDatabase())
        {
            postsPage = await TryLoadAsync(
                () => _newsService.GetPublishedPostsPageAsync(selectedCategorySlug, page, DefaultPageSize, cancellationToken),
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
            FeaturedPost = featuredPost is null ? null : PostCardViewModel.FromPost(featuredPost),
            Posts = postsPage.Items.Select(PostCardViewModel.FromPost).ToList(),
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
            CategorySlug = NewsCategories.NormalizeCategory(post.Category) ?? string.Empty,
            CategoryDisplayName = NewsCategories.GetDisplayName(post.Category),
            IsPromotion = string.Equals(NewsCategories.NormalizeCategory(post.Category), NewsCategories.KhuyenMaiUuDai, StringComparison.OrdinalIgnoreCase),
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

    private static string? NormalizeSlug(string? slug)
    {
        return string.IsNullOrWhiteSpace(slug)
            ? null
            : slug.Trim().ToLowerInvariant();
    }
}

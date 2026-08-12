using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Content;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using KIGHolding.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Areas.Admin.Controllers;

public class PostController : AdminBaseController
{
    private const int DefaultPageSize = 10;
    private const string PublishedStatus = "published";
    private const string DraftStatus = "draft";

    private readonly AppDbContext _dbContext;
    private readonly IImageStorageService _imageStorageService;
    private readonly INewsService _newsService;
    private readonly IBlogHtmlSanitizer _blogHtmlSanitizer;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private const int MaxFileSize = 2 * 1024 * 1024;

    public PostController(
        AppDbContext dbContext,
        IImageStorageService imageStorageService,
        INewsService newsService,
        IBlogHtmlSanitizer blogHtmlSanitizer)
    {
        _dbContext = dbContext;
        _imageStorageService = imageStorageService;
        _newsService = newsService;
        _blogHtmlSanitizer = blogHtmlSanitizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? category, string? status, int page = 1)
    {
        var requestedPage = Math.Max(page, 1);

        var searchQuery = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var normalizedCategory = NewsCategories.NormalizeCategory(category);
        var normalizedStatus = NormalizeStatus(status);

        IQueryable<Post> query = _dbContext.Posts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var searchPattern = $"%{searchQuery}%";
            var categoryAliases = NewsCategories.GetStorageAliases(searchQuery);

            query = categoryAliases.Count > 0
                ? query.Where(x =>
                    EF.Functions.ILike(x.Title, searchPattern) ||
                    EF.Functions.ILike(x.Slug, searchPattern) ||
                    EF.Functions.ILike(x.Excerpt, searchPattern) ||
                    EF.Functions.ILike(x.Category, searchPattern) ||
                    (x.SeoTitle != null && EF.Functions.ILike(x.SeoTitle, searchPattern)) ||
                    categoryAliases.Contains(x.Category))
                : query.Where(x =>
                    EF.Functions.ILike(x.Title, searchPattern) ||
                    EF.Functions.ILike(x.Slug, searchPattern) ||
                    EF.Functions.ILike(x.Excerpt, searchPattern) ||
                    EF.Functions.ILike(x.Category, searchPattern) ||
                    (x.SeoTitle != null && EF.Functions.ILike(x.SeoTitle, searchPattern)));
        }

        if (!string.IsNullOrWhiteSpace(normalizedCategory))
        {
            var categoryAliases = NewsCategories.GetStorageAliases(normalizedCategory);
            query = query.Where(x => categoryAliases.Contains(x.Category));
        }

        query = normalizedStatus switch
        {
            PublishedStatus => query.Where(x => x.IsPublished),
            DraftStatus => query.Where(x => !x.IsPublished),
            _ => query
        };

        var totalItems = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)DefaultPageSize));
        var currentPage = Math.Min(requestedPage, totalPages);

        var orderedQuery = query
            .OrderByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id);

        var posts = await orderedQuery
            .Skip((currentPage - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(x => new PostListItemViewModel
            {
                Id = x.Id,
                ThumbnailUrl = x.ThumbnailUrl ?? string.Empty,
                Title = x.Title,
                Slug = x.Slug,
                Category = x.Category,
                IsPublished = x.IsPublished,
                PublishedAt = x.PublishedAt,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Excerpt = x.Excerpt
            })
            .ToListAsync();

        foreach (var post in posts)
        {
            post.CategoryDisplayName = NewsCategories.GetDisplayName(post.Category);
        }

        return View(new PostIndexViewModel
        {
            Posts = posts,
            Categories = BuildCategoryFilterOptions(),
            StatusOptions = BuildStatusOptions(),
            SearchQuery = searchQuery,
            SelectedCategory = normalizedCategory,
            SelectedStatus = normalizedStatus,
            Page = currentPage,
            PageSize = DefaultPageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new PostCreateViewModel
        {
            CategoryOptions = BuildCategoryOptions(),
            Category = NewsCategories.Default.Slug,
            PublishedAt = DateTime.Now
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PostCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.CategoryOptions = BuildCategoryOptions();
            return View(model);
        }

        var normalizedCategory = ValidateAndNormalizeCategory(model.Category);
        if (normalizedCategory is null)
        {
            model.CategoryOptions = BuildCategoryOptions();
            return View(model);
        }

        var requestedSlug = NormalizeSlugInput(model.Slug);
        if (!string.IsNullOrWhiteSpace(requestedSlug) &&
            await _dbContext.Posts.AsNoTracking().AnyAsync(x => x.Slug == requestedSlug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Slug này đã tồn tại.");
            model.CategoryOptions = BuildCategoryOptions();
            return View(model);
        }

        var slug = await BuildUniqueSlugAsync(model.Slug, model.Title);
        var thumbnailUrl = string.Empty;
        if (!TryPreparePostContent(model.Content, model.ContentMode, nameof(model.Content), nameof(model.ContentMode), out var preparedContent) ||
            !TryNormalizeSeoUrls(model.CanonicalUrl, model.OgImageUrl, out var canonicalUrl, out var ogImageUrl))
        {
            model.CategoryOptions = BuildCategoryOptions();
            return View(model);
        }

        if (model.ThumbnailFile is not null && model.ThumbnailFile.Length > 0)
        {
            try
            {
                thumbnailUrl = await _imageStorageService.UploadAsync(
                    model.ThumbnailFile,
                    ImageCategory.Posts,
                    HttpContext.RequestAborted);
            }
            catch
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Không thể tải ảnh lên. Vui lòng thử lại.");
                model.CategoryOptions = BuildCategoryOptions();
                return View(model);
            }
        }

        var post = new Post
        {
            Title = model.Title.Trim(),
            Slug = slug,
            Category = normalizedCategory,
            Excerpt = model.Excerpt.Trim(),
            Content = preparedContent,
            ContentMode = model.ContentMode,
            ThumbnailUrl = thumbnailUrl,
            IsPublished = model.IsPublished,
            PublishedAt = model.IsPublished
                ? NormalizeLocalInputToUtc(model.PublishedAt) ?? DateTimeOffset.UtcNow
                : NormalizeLocalInputToUtc(model.PublishedAt),
            SeoTitle = TrimToNull(model.SeoTitle),
            SeoDescription = TrimToNull(model.SeoDescription),
            FocusKeyword = TrimToNull(model.FocusKeyword),
            CanonicalUrl = canonicalUrl,
            OgTitle = TrimToNull(model.OgTitle),
            OgDescription = TrimToNull(model.OgDescription),
            OgImageUrl = ogImageUrl,
            RobotsIndex = model.RobotsIndex,
            RobotsFollow = model.RobotsFollow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Posts.Add(post);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            await _imageStorageService.DeleteAsync(thumbnailUrl, ImageCategory.Posts, HttpContext.RequestAborted);
            throw;
        }

        _newsService.InvalidateNewsCache();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var post = await _dbContext.Posts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (post is null)
        {
            return NotFound();
        }

        return View(new PostEditViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Category = NewsCategories.NormalizeCategory(post.Category) ?? NewsCategories.Default.Slug,
            Excerpt = post.Excerpt,
            Content = post.Content,
            ContentMode = post.ContentMode,
            IsPublished = post.IsPublished,
            PublishedAt = post.PublishedAt?.LocalDateTime,
            SeoTitle = post.SeoTitle,
            SeoDescription = post.SeoDescription,
            FocusKeyword = post.FocusKeyword,
            CanonicalUrl = post.CanonicalUrl,
            OgTitle = post.OgTitle,
            OgDescription = post.OgDescription,
            OgImageUrl = post.OgImageUrl,
            RobotsIndex = post.RobotsIndex,
            RobotsFollow = post.RobotsFollow,
            ExistingThumbnailUrl = post.ThumbnailUrl,
            CategoryOptions = BuildCategoryOptions()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PostEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var post = await _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == id);
        if (post is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.ExistingThumbnailUrl = post.ThumbnailUrl;
            model.CategoryOptions = BuildCategoryOptions();
            return View(model);
        }

        var normalizedCategory = ValidateAndNormalizeCategory(model.Category);
        if (normalizedCategory is null)
        {
            model.ExistingThumbnailUrl = post.ThumbnailUrl;
            model.CategoryOptions = BuildCategoryOptions();
            return View(model);
        }

        var requestedSlug = NormalizeSlugInput(model.Slug);
        if (!string.IsNullOrWhiteSpace(requestedSlug) &&
            await _dbContext.Posts.AsNoTracking().AnyAsync(x => x.Slug == requestedSlug && x.Id != post.Id))
        {
            ModelState.AddModelError(nameof(model.Slug), "Slug này đã tồn tại.");
            model.ExistingThumbnailUrl = post.ThumbnailUrl;
            model.CategoryOptions = BuildCategoryOptions();
            return View(model);
        }

        var slug = await BuildUniqueSlugAsync(model.Slug, model.Title, post.Id);
        var previousThumbnailUrl = post.ThumbnailUrl;
        string? uploadedThumbnailUrl = null;
        if (!TryPreparePostContent(model.Content, model.ContentMode, nameof(model.Content), nameof(model.ContentMode), out var preparedContent) ||
            !TryNormalizeSeoUrls(model.CanonicalUrl, model.OgImageUrl, out var canonicalUrl, out var ogImageUrl))
        {
            model.ExistingThumbnailUrl = post.ThumbnailUrl;
            model.CategoryOptions = BuildCategoryOptions();
            return View(model);
        }

        if (model.ThumbnailFile is not null && model.ThumbnailFile.Length > 0)
        {
            try
            {
                uploadedThumbnailUrl = await _imageStorageService.UploadAsync(
                    model.ThumbnailFile,
                    ImageCategory.Posts,
                    HttpContext.RequestAborted);
            }
            catch
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Không thể tải ảnh lên. Vui lòng thử lại.");
                model.ExistingThumbnailUrl = post.ThumbnailUrl;
                model.CategoryOptions = BuildCategoryOptions();
                return View(model);
            }
        }

        post.Title = model.Title.Trim();
        post.Slug = slug;
        post.Category = normalizedCategory;
        post.Excerpt = model.Excerpt.Trim();
        post.Content = preparedContent;
        post.ContentMode = model.ContentMode;
        if (!string.IsNullOrWhiteSpace(uploadedThumbnailUrl))
        {
            post.ThumbnailUrl = uploadedThumbnailUrl;
        }

        post.IsPublished = model.IsPublished;
        post.PublishedAt = model.IsPublished
            ? NormalizeLocalInputToUtc(model.PublishedAt) ?? DateTimeOffset.UtcNow
            : NormalizeLocalInputToUtc(model.PublishedAt);
        post.SeoTitle = TrimToNull(model.SeoTitle);
        post.SeoDescription = TrimToNull(model.SeoDescription);
        post.FocusKeyword = TrimToNull(model.FocusKeyword);
        post.CanonicalUrl = canonicalUrl;
        post.OgTitle = TrimToNull(model.OgTitle);
        post.OgDescription = TrimToNull(model.OgDescription);
        post.OgImageUrl = ogImageUrl;
        post.RobotsIndex = model.RobotsIndex;
        post.RobotsFollow = model.RobotsFollow;
        post.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            await _imageStorageService.DeleteAsync(uploadedThumbnailUrl, ImageCategory.Posts, HttpContext.RequestAborted);
            throw;
        }

        _newsService.InvalidateNewsCache();
        if (!string.IsNullOrWhiteSpace(uploadedThumbnailUrl))
        {
            await _imageStorageService.DeleteAsync(previousThumbnailUrl, ImageCategory.Posts, HttpContext.RequestAborted);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, string? q, string? category, string? status, int page = 1)
    {
        var post = await _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == id);
        if (post is null)
        {
            return NotFound();
        }

        post.IsPublished = false;
        post.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        _newsService.InvalidateNewsCache();
        SetSuccessMessage("Bài viết đã được ẩn khỏi website công khai.");

        return RedirectToAction(nameof(Index), BuildIndexRouteValues(q, category, status, page));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PermanentDelete(Guid id, string? q, string? category, string? status, int page = 1)
    {
        var post = await _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == id);
        if (post is null)
        {
            return NotFound();
        }

        var thumbnailUrl = post.ThumbnailUrl;

        _dbContext.Posts.Remove(post);
        await _dbContext.SaveChangesAsync();
        _newsService.InvalidateNewsCache();

        if (!string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            await _imageStorageService.DeleteAsync(thumbnailUrl, ImageCategory.Posts, HttpContext.RequestAborted);
        }

        SetSuccessMessage("Bài viết đã được xóa vĩnh viễn.");

        return RedirectToAction(nameof(Index), BuildIndexRouteValues(q, category, status, page));
    }

    private static DateTimeOffset? NormalizeLocalInputToUtc(DateTime? value)
    {
        return value.HasValue ? NormalizeLocalInputToUtc(value.Value) : null;
    }

    private bool TryPreparePostContent(
        string? content,
        PostContentMode contentMode,
        string contentModelStateKey,
        string contentModeModelStateKey,
        out string preparedContent)
    {
        preparedContent = string.Empty;
        if (!Enum.IsDefined(contentMode))
        {
            ModelState.AddModelError(contentModeModelStateKey, "Che do noi dung khong hop le.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError(contentModelStateKey, "Vui long nhap noi dung.");
            return false;
        }

        var trimmedContent = content.Trim();
        if (trimmedContent.Length > PostEditorLimits.ContentMaxLength)
        {
            ModelState.AddModelError(contentModelStateKey, $"Noi dung khong duoc vuot qua {PostEditorLimits.ContentMaxLength:N0} ky tu.");
            return false;
        }

        if (contentMode == PostContentMode.Visual)
        {
            preparedContent = trimmedContent;
            return true;
        }

        var sanitizedContent = _blogHtmlSanitizer.Sanitize(trimmedContent).Trim();
        if (sanitizedContent.Length > PostEditorLimits.ContentMaxLength)
        {
            ModelState.AddModelError(contentModelStateKey, $"Noi dung HTML sau khi loc an toan khong duoc vuot qua {PostEditorLimits.ContentMaxLength:N0} ky tu.");
            return false;
        }

        if (!_blogHtmlSanitizer.ContainsMeaningfulContent(sanitizedContent))
        {
            ModelState.AddModelError(contentModelStateKey, "Noi dung HTML khong co noi dung hop le sau khi loc an toan.");
            return false;
        }

        preparedContent = sanitizedContent;
        return true;
    }

    private bool TryNormalizeSeoUrls(
        string? canonicalUrlInput,
        string? ogImageUrlInput,
        out string? canonicalUrl,
        out string? ogImageUrl)
    {
        canonicalUrl = TrimToNull(canonicalUrlInput);
        ogImageUrl = TrimToNull(ogImageUrlInput);

        var isValid = true;
        if (canonicalUrl is not null && !IsSafeCanonicalUrl(canonicalUrl))
        {
            ModelState.AddModelError(nameof(PostCreateViewModel.CanonicalUrl), "Canonical URL phai la URL HTTP/HTTPS hop le hoac duong dan noi bo an toan.");
            isValid = false;
        }

        if (ogImageUrl is not null && !IsSafeOgImageUrl(ogImageUrl))
        {
            ModelState.AddModelError(nameof(PostCreateViewModel.OgImageUrl), "OG Image URL phai la HTTPS hoac duong dan noi bo an toan.");
            isValid = false;
        }

        return isValid;
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsSafeCanonicalUrl(string value)
    {
        return IsSafeInternalUrl(value)
            || (Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && uri.Scheme is "http" or "https");
    }

    private static bool IsSafeOgImageUrl(string value)
    {
        return IsSafeInternalUrl(value)
            || (Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsSafeInternalUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmedValue = value.Trim();
        return trimmedValue.StartsWith("/", StringComparison.Ordinal)
            && !trimmedValue.StartsWith("//", StringComparison.Ordinal)
            && !trimmedValue.Contains("\\", StringComparison.Ordinal);
    }

    private static DateTimeOffset NormalizeLocalInputToUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return new DateTimeOffset(value, TimeSpan.Zero);
        }

        var localValue = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Local)
            : value;

        return new DateTimeOffset(localValue).ToUniversalTime();
    }

    private async Task<string> BuildUniqueSlugAsync(string? requestedSlug, string fallbackName, Guid? currentId = null)
    {
        var baseSlug = NormalizeSlugInput(string.IsNullOrWhiteSpace(requestedSlug) ? fallbackName : requestedSlug);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = Guid.NewGuid().ToString("N")[..8];
        }

        var slug = baseSlug;
        var suffix = 2;

        while (await _dbContext.Posts
                   .AsNoTracking()
                   .AnyAsync(x => x.Slug == slug && (!currentId.HasValue || x.Id != currentId.Value)))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private static IReadOnlyList<SelectListItem> BuildCategoryOptions()
    {
        return NewsCategories.All
            .Select(x => new SelectListItem
            {
                Value = x.Slug,
                Text = x.DisplayName
            })
            .ToList();
    }

    private static IReadOnlyList<PostFilterOptionViewModel> BuildCategoryFilterOptions()
    {
        return NewsCategories.All
            .Select(x => new PostFilterOptionViewModel
            {
                Value = x.Slug,
                Label = x.DisplayName
            })
            .ToList();
    }

    private static IReadOnlyList<PostFilterOptionViewModel> BuildStatusOptions()
    {
        return
        [
            new PostFilterOptionViewModel { Value = "all", Label = "Tất cả trạng thái" },
            new PostFilterOptionViewModel { Value = PublishedStatus, Label = "Đã đăng" },
            new PostFilterOptionViewModel { Value = DraftStatus, Label = "Bản nháp" }
        ];
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalizedStatus = status.Trim().ToLowerInvariant();
        return normalizedStatus is PublishedStatus or DraftStatus
            ? normalizedStatus
            : null;
    }

    private static object BuildIndexRouteValues(string? q, string? category, string? status, int page)
    {
        return new
        {
            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            category = NewsCategories.NormalizeCategory(category),
            status = NormalizeStatus(status),
            page = Math.Max(page, 1)
        };
    }

    private string? ValidateAndNormalizeCategory(string? category)
    {
        var normalizedCategory = NewsCategories.NormalizeCategory(category);
        if (!string.IsNullOrWhiteSpace(normalizedCategory))
        {
            return normalizedCategory;
        }

        ModelState.AddModelError(nameof(PostCreateViewModel.Category), "Vui lòng chọn danh mục hợp lệ.");
        return null;
    }

    private static string NormalizeSlugInput(string? value)
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
        normalized = normalized.Replace('đ', 'd');
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", string.Empty);
        normalized = Regex.Replace(normalized, @"[\s-]+", "-").Trim('-');

        return normalized;
    }
}

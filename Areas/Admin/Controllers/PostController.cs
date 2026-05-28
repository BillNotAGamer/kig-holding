using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Content;
using KIGHolding.Models.Entities;
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
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private const int MaxFileSize = 2 * 1024 * 1024;
    public PostController(AppDbContext dbContext, IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? category, string? status, int page = 1)
    {
        page = Math.Max(page, 1);

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

        query = query
            .OrderByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.CreatedAt);

        var totalItems = await query.CountAsync();
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)DefaultPageSize);
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var posts = await query
            .Skip((page - 1) * DefaultPageSize)
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
            Page = page,
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
        var thumbnailUrl = await SaveUploadedFileAsync(model.ThumbnailFile, "posts");
        if (model.ThumbnailFile is not null && model.ThumbnailFile.Length > 0 && string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            ModelState.AddModelError(nameof(model.ThumbnailFile), "Ảnh tải lên không hợp lệ hoặc vượt quá 2MB.");
            model.CategoryOptions = BuildCategoryOptions();
            return View(model);
        }

        var post = new Post
        {
            Title = model.Title.Trim(),
            Slug = slug,
            Category = normalizedCategory,
            Excerpt = model.Excerpt.Trim(),
            Content = model.Content.Trim(),
            ThumbnailUrl = thumbnailUrl,
            IsPublished = model.IsPublished,
            PublishedAt = model.IsPublished
                ? NormalizeLocalInputToUtc(model.PublishedAt) ?? DateTimeOffset.UtcNow
                : NormalizeLocalInputToUtc(model.PublishedAt),
            SeoTitle = string.IsNullOrWhiteSpace(model.SeoTitle) ? model.Title.Trim() : model.SeoTitle.Trim(),
            SeoDescription = string.IsNullOrWhiteSpace(model.SeoDescription) ? model.Excerpt.Trim() : model.SeoDescription.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync();

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
            IsPublished = post.IsPublished,
            PublishedAt = post.PublishedAt?.LocalDateTime,
            SeoTitle = post.SeoTitle,
            SeoDescription = post.SeoDescription,
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

        if (model.ThumbnailFile is not null && model.ThumbnailFile.Length > 0)
        {
            var newThumbnailUrl = await SaveUploadedFileAsync(model.ThumbnailFile, "posts");
            if (string.IsNullOrWhiteSpace(newThumbnailUrl))
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Ảnh tải lên không hợp lệ hoặc vượt quá 2MB.");
                model.ExistingThumbnailUrl = post.ThumbnailUrl;
                model.CategoryOptions = BuildCategoryOptions();
                return View(model);
            }

            DeleteUploadedFile(post.ThumbnailUrl, "posts");
            post.ThumbnailUrl = newThumbnailUrl;
        }

        post.Title = model.Title.Trim();
        post.Slug = slug;
        post.Category = normalizedCategory;
        post.Excerpt = model.Excerpt.Trim();
        post.Content = model.Content.Trim();
        post.IsPublished = model.IsPublished;
        post.PublishedAt = model.IsPublished
            ? NormalizeLocalInputToUtc(model.PublishedAt) ?? DateTimeOffset.UtcNow
            : NormalizeLocalInputToUtc(model.PublishedAt);
        post.SeoTitle = string.IsNullOrWhiteSpace(model.SeoTitle) ? model.Title.Trim() : model.SeoTitle.Trim();
        post.SeoDescription = string.IsNullOrWhiteSpace(model.SeoDescription) ? model.Excerpt.Trim() : model.SeoDescription.Trim();
        post.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

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
        return RedirectToAction(nameof(Index), new
        {
            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            category = NewsCategories.NormalizeCategory(category),
            status = NormalizeStatus(status),
            page = Math.Max(page, 1)
        });
    }

    private static DateTimeOffset? NormalizeLocalInputToUtc(DateTime? value)
    {
        return value.HasValue ? NormalizeLocalInputToUtc(value.Value) : null;
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

    private async Task<string> SaveUploadedFileAsync(IFormFile? file, string subFolder)
    {
        if (file is null || file.Length == 0)
        {
            return string.Empty;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) ||
            (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType)) ||
            file.Length > MaxFileSize)
        {
            return string.Empty;
        }

        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsFolder);

        var safeBaseName = NormalizeSlugInput(Path.GetFileNameWithoutExtension(file.FileName));
        if (string.IsNullOrWhiteSpace(safeBaseName))
        {
            safeBaseName = subFolder;
        }

        var uniqueFileName = $"{safeBaseName}-{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(fileStream);

        return $"/uploads/{subFolder}/{uniqueFileName}";
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

    private void DeleteUploadedFile(string? relativePath, string subFolder)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith($"/uploads/{subFolder}/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var trimmedPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_env.WebRootPath, trimmedPath));
        var uploadsRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads", subFolder));

        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
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

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Areas.Admin.Controllers;

public class PostController : AdminBaseController
{
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
    public async Task<IActionResult> Index()
    {
        var posts = await _dbContext.Posts
            .AsNoTracking()
            .OrderByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new PostListItemViewModel
            {
                Id = x.Id,
                ThumbnailUrl = x.ThumbnailUrl,
                Title = x.Title,
                Category = x.Category,
                IsPublished = x.IsPublished,
                PublishedAt = x.PublishedAt
            })
            .ToListAsync();

        return View(new PostIndexViewModel { Posts = posts });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new PostCreateViewModel
        {
            CategoryOptions = BuildCategoryOptions(),
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
            Category = model.Category.Trim(),
            Excerpt = model.Excerpt.Trim(),
            Content = model.Content.Trim(),
            ThumbnailUrl = thumbnailUrl,
            IsPublished = model.IsPublished,
            PublishedAt = model.IsPublished
                ? new DateTimeOffset(model.PublishedAt ?? DateTime.Now)
                : model.PublishedAt.HasValue ? new DateTimeOffset(model.PublishedAt.Value) : null,
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
            Category = post.Category,
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
        post.Category = model.Category.Trim();
        post.Excerpt = model.Excerpt.Trim();
        post.Content = model.Content.Trim();
        post.IsPublished = model.IsPublished;
        post.PublishedAt = model.IsPublished
            ? new DateTimeOffset(model.PublishedAt ?? DateTime.Now)
            : model.PublishedAt.HasValue ? new DateTimeOffset(model.PublishedAt.Value) : null;
        post.SeoTitle = string.IsNullOrWhiteSpace(model.SeoTitle) ? model.Title.Trim() : model.SeoTitle.Trim();
        post.SeoDescription = string.IsNullOrWhiteSpace(model.SeoDescription) ? model.Excerpt.Trim() : model.SeoDescription.Trim();
        post.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var post = await _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == id);
        if (post is null)
        {
            return NotFound();
        }

        post.IsPublished = false;
        post.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
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
        return
        [
            new SelectListItem { Value = string.Empty, Text = "Chọn danh mục" },
            new SelectListItem { Value = "Khuyến mãi", Text = "Khuyến mãi" },
            new SelectListItem { Value = "Món mới", Text = "Món mới" },
            new SelectListItem { Value = "Sự kiện", Text = "Sự kiện" },
            new SelectListItem { Value = "Câu chuyện ẩm thực", Text = "Câu chuyện ẩm thực" }
        ];
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

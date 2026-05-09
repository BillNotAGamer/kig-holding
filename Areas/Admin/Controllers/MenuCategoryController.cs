using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Areas.Admin.Controllers;

public class MenuCategoryController : AdminBaseController
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _env;
    private readonly IMenuService _menuService;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private const int MaxFileSize = 2 * 1024 * 1024;

    public MenuCategoryController(AppDbContext dbContext, IWebHostEnvironment env, IMenuService menuService)
    {
        _dbContext = dbContext;
        _env = env;
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await _dbContext.MenuCategories
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new MenuCategoryListItemViewModel
            {
                Id = x.Id,
                DisplayOrder = x.DisplayOrder,
                Name = x.Name,
                Slug = x.Slug,
                ThumbnailUrl = x.ThumbnailUrl,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return View(new MenuCategoryIndexViewModel { Categories = categories });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new MenuCategoryCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuCategoryCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var requestedSlug = NormalizeSlugInput(model.Slug);
        if (!string.IsNullOrWhiteSpace(requestedSlug) &&
            await _dbContext.MenuCategories.AsNoTracking().AnyAsync(x => x.Slug == requestedSlug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Slug này đã tồn tại.");
            return View(model);
        }

        var slug = await BuildUniqueSlugAsync(model.Slug, model.Name);

        var thumbnailUrl = await SaveThumbnailAsync(model.ThumbnailFile, "categories");
        if (model.ThumbnailFile is not null && model.ThumbnailFile.Length > 0 && string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            ModelState.AddModelError(nameof(model.ThumbnailFile), "Ảnh tải lên không hợp lệ hoặc vượt quá 2MB.");
            return View(model);
        }

        var category = new MenuCategory
        {
            Name = model.Name.Trim(),
            Slug = slug,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            ThumbnailUrl = thumbnailUrl,
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.MenuCategories.Add(category);
        await _dbContext.SaveChangesAsync();
        _menuService.InvalidateActiveCategoriesCache();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var category = await _dbContext.MenuCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var model = new MenuCategoryEditViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            ExistingThumbnailUrl = category.ThumbnailUrl
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, MenuCategoryEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var category = await _dbContext.MenuCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.ExistingThumbnailUrl = category.ThumbnailUrl;
            return View(model);
        }

        var requestedSlug = NormalizeSlugInput(model.Slug);
        if (!string.IsNullOrWhiteSpace(requestedSlug) &&
            await _dbContext.MenuCategories.AsNoTracking().AnyAsync(x => x.Slug == requestedSlug && x.Id != category.Id))
        {
            ModelState.AddModelError(nameof(model.Slug), "Slug này đã tồn tại.");
            model.ExistingThumbnailUrl = category.ThumbnailUrl;
            return View(model);
        }

        var slug = await BuildUniqueSlugAsync(model.Slug, model.Name, category.Id);

        if (model.ThumbnailFile is not null && model.ThumbnailFile.Length > 0)
        {
            var newThumbnailUrl = await SaveThumbnailAsync(model.ThumbnailFile, "categories");
            if (string.IsNullOrWhiteSpace(newThumbnailUrl))
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Ảnh tải lên không hợp lệ hoặc vượt quá 2MB.");
                model.ExistingThumbnailUrl = category.ThumbnailUrl;
                return View(model);
            }

            DeleteUploadedFile(category.ThumbnailUrl, "categories");
            category.ThumbnailUrl = newThumbnailUrl;
        }

        category.Name = model.Name.Trim();
        category.Slug = slug;
        category.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        category.DisplayOrder = model.DisplayOrder;
        category.IsActive = model.IsActive;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        _menuService.InvalidateActiveCategoriesCache();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _dbContext.MenuCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        category.IsActive = false;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        _menuService.InvalidateActiveCategoriesCache();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var category = await _dbContext.MenuCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        category.IsActive = !category.IsActive;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        _menuService.InvalidateActiveCategoriesCache();

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

        while (await _dbContext.MenuCategories
                   .AsNoTracking()
                   .AnyAsync(x => x.Slug == slug && (!currentId.HasValue || x.Id != currentId.Value)))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private async Task<string> SaveThumbnailAsync(IFormFile? file, string subFolder)
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

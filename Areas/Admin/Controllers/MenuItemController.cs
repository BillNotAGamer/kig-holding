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

public class MenuItemController : AdminBaseController
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

    public MenuItemController(AppDbContext dbContext, IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? categoryFilter)
    {
        var query = _dbContext.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .AsQueryable();

        if (categoryFilter.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryFilter.Value);
        }

        var items = await query
            .OrderBy(x => x.Category.DisplayOrder)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new MenuItemListItemViewModel
            {
                Id = x.Id,
                ThumbnailUrl = x.ThumbnailUrl,
                Name = x.Name,
                CategoryName = x.Category.Name,
                Price = x.Price,
                IsAvailable = x.IsAvailable
            })
            .ToListAsync();

        var model = new MenuItemIndexViewModel
        {
            CategoryFilter = categoryFilter,
            CategoryOptions = await BuildCategoryOptionsAsync(includeAllOption: true),
            Items = items
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await BuildCreateModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuItemCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await PopulateFormAsync(model);
            return View(model);
        }

        var requestedSlug = NormalizeSlugInput(model.Slug);
        if (!string.IsNullOrWhiteSpace(requestedSlug) &&
            await _dbContext.MenuItems.AsNoTracking().AnyAsync(x => x.Slug == requestedSlug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Slug này đã tồn tại.");
            model = await PopulateFormAsync(model);
            return View(model);
        }

        var slug = await BuildUniqueSlugAsync(model.Slug, model.Name);

        var thumbnailUrl = await SaveUploadedFileAsync(model.ThumbnailFile, "menu");
        if (string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            ModelState.AddModelError(nameof(model.ThumbnailFile), "Vui lòng tải lên ảnh hợp lệ (jpg, jpeg, png, webp) và không vượt quá 2MB.");
            model = await PopulateFormAsync(model);
            return View(model);
        }

        var description = string.IsNullOrWhiteSpace(model.Description)
            ? model.ShortDescription.Trim()
            : model.Description.Trim();

        var menuItem = new MenuItem
        {
            CategoryId = model.CategoryId!.Value,
            Name = model.Name.Trim(),
            KoreanName = string.IsNullOrWhiteSpace(model.KoreanName) ? null : model.KoreanName.Trim(),
            Slug = slug,
            ShortDescription = model.ShortDescription.Trim(),
            Description = description,
            Price = model.Price,
            OriginalPrice = model.OriginalPrice,
            ThumbnailUrl = thumbnailUrl,
            SpicyLevel = model.SpicyLevel,
            Calories = model.Calories,
            IsSignature = model.IsSignature,
            IsBestSeller = model.IsBestSeller,
            IsNew = model.IsNew,
            IsCombo = model.IsCombo,
            IsAvailable = model.IsAvailable,
            DisplayOrder = model.DisplayOrder,
            SeoTitle = model.Name.Trim(),
            SeoDescription = model.ShortDescription.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.MenuItems.Add(menuItem);
        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { categoryFilter = model.CategoryId!.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _dbContext.MenuItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        return View(await BuildEditModelAsync(item));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, MenuItemEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var item = await _dbContext.MenuItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.ExistingThumbnailUrl = item.ThumbnailUrl;
            model = await PopulateFormAsync(model);
            model.ExistingThumbnailUrl = item.ThumbnailUrl;
            return View(model);
        }

        var requestedSlug = NormalizeSlugInput(model.Slug);
        if (!string.IsNullOrWhiteSpace(requestedSlug) &&
            await _dbContext.MenuItems.AsNoTracking().AnyAsync(x => x.Slug == requestedSlug && x.Id != item.Id))
        {
            ModelState.AddModelError(nameof(model.Slug), "Slug này đã tồn tại.");
            model.ExistingThumbnailUrl = item.ThumbnailUrl;
            model = await PopulateFormAsync(model);
            model.ExistingThumbnailUrl = item.ThumbnailUrl;
            return View(model);
        }

        var slug = await BuildUniqueSlugAsync(model.Slug, model.Name, item.Id);

        if (model.ThumbnailFile is not null && model.ThumbnailFile.Length > 0)
        {
            var newThumbnailUrl = await SaveUploadedFileAsync(model.ThumbnailFile, "menu");
            if (string.IsNullOrWhiteSpace(newThumbnailUrl))
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Vui lòng tải lên ảnh hợp lệ (jpg, jpeg, png, webp) và không vượt quá 2MB.");
                model.ExistingThumbnailUrl = item.ThumbnailUrl;
                model = await PopulateFormAsync(model);
                model.ExistingThumbnailUrl = item.ThumbnailUrl;
                return View(model);
            }

            DeleteUploadedFile(item.ThumbnailUrl, "menu");
            item.ThumbnailUrl = newThumbnailUrl;
        }

        item.CategoryId = model.CategoryId!.Value;
        item.Name = model.Name.Trim();
        item.KoreanName = string.IsNullOrWhiteSpace(model.KoreanName) ? null : model.KoreanName.Trim();
        item.Slug = slug;
        item.ShortDescription = model.ShortDescription.Trim();
        item.Description = string.IsNullOrWhiteSpace(model.Description)
            ? model.ShortDescription.Trim()
            : model.Description.Trim();
        item.Price = model.Price;
        item.OriginalPrice = model.OriginalPrice;
        item.SpicyLevel = model.SpicyLevel;
        item.Calories = model.Calories;
        item.IsSignature = model.IsSignature;
        item.IsBestSeller = model.IsBestSeller;
        item.IsNew = model.IsNew;
        item.IsCombo = model.IsCombo;
        item.IsAvailable = model.IsAvailable;
        item.DisplayOrder = model.DisplayOrder;
        item.SeoTitle = model.Name.Trim();
        item.SeoDescription = model.ShortDescription.Trim();
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { categoryFilter = model.CategoryId!.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _dbContext.MenuItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        item.IsAvailable = false;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { categoryFilter = item.CategoryId });
    }

    private async Task<MenuItemCreateViewModel> BuildCreateModelAsync()
    {
        return new MenuItemCreateViewModel
        {
            CategoryId = null,
            IsAvailable = true,
            CategoryOptions = await BuildCategoryOptionsAsync(includeAllOption: false, includePlaceholder: true)
        };
    }

    private async Task<MenuItemEditViewModel> BuildEditModelAsync(MenuItem item)
    {
        return new MenuItemEditViewModel
        {
            Id = item.Id,
            CategoryId = item.CategoryId,
            Name = item.Name,
            KoreanName = item.KoreanName,
            Slug = item.Slug,
            Price = item.Price,
            OriginalPrice = item.OriginalPrice,
            ShortDescription = item.ShortDescription,
            Description = item.Description,
            SpicyLevel = item.SpicyLevel,
            Calories = item.Calories,
            IsSignature = item.IsSignature,
            IsBestSeller = item.IsBestSeller,
            IsNew = item.IsNew,
            IsAvailable = item.IsAvailable,
            IsCombo = item.IsCombo,
            DisplayOrder = item.DisplayOrder,
            ExistingThumbnailUrl = item.ThumbnailUrl,
            CategoryOptions = await BuildCategoryOptionsAsync(includeAllOption: false, includePlaceholder: true)
        };
    }

    private async Task<MenuItemCreateViewModel> PopulateFormAsync(MenuItemCreateViewModel model)
    {
        model.CategoryOptions = await BuildCategoryOptionsAsync(includeAllOption: false, includePlaceholder: true);
        return model;
    }

    private async Task<MenuItemEditViewModel> PopulateFormAsync(MenuItemEditViewModel model)
    {
        model.CategoryOptions = await BuildCategoryOptionsAsync(includeAllOption: false, includePlaceholder: true);
        return model;
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildCategoryOptionsAsync(bool includeAllOption, bool includePlaceholder = false)
    {
        var categories = await _dbContext.MenuCategories
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Name
            })
            .ToListAsync();

        if (!includeAllOption && !includePlaceholder)
        {
            return categories;
        }

        var options = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = includePlaceholder ? "Chọn danh mục" : "Tất cả danh mục" }
        };

        options.AddRange(categories);
        return options;
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

        while (await _dbContext.MenuItems
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

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Areas.Admin.Controllers;

public class MenuGroupController : AdminBaseController
{
    private const string PublishedStatus = "published";
    private const string HiddenStatus = "hidden";
    private const int MaxMenuPageUploadFileCount = 10;
    private const long MaxMenuPageFileSize = 50L * 1024 * 1024;
    private const long MaxMenuGroupCoverFileSize = 10L * 1024 * 1024;

    private readonly AppDbContext _dbContext;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<MenuGroupController> _logger;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public MenuGroupController(
        AppDbContext dbContext,
        IImageStorageService imageStorageService,
        ILogger<MenuGroupController> logger)
    {
        _dbContext = dbContext;
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? status)
    {
        var searchQuery = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var normalizedStatus = NormalizeStatus(status);

        IQueryable<MenuGroup> query = _dbContext.MenuGroups.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var searchPattern = $"%{searchQuery}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, searchPattern) ||
                EF.Functions.ILike(x.Slug, searchPattern) ||
                EF.Functions.ILike(x.ShortDescription, searchPattern));
        }

        query = normalizedStatus switch
        {
            PublishedStatus => query.Where(x => x.IsPublished),
            HiddenStatus => query.Where(x => !x.IsPublished),
            _ => query
        };

        var groups = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new MenuGroupListItemViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                ShortDescription = x.ShortDescription,
                CoverImageUrl = x.CoverImageUrl,
                DisplayOrder = x.DisplayOrder,
                IsPublished = x.IsPublished,
                PageImageCount = x.PageImages.Count,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return View(new MenuGroupIndexViewModel
        {
            Groups = groups,
            SearchQuery = searchQuery,
            SelectedStatus = normalizedStatus,
            StatusOptions = BuildStatusOptions(),
            TotalCount = groups.Count
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var nextDisplayOrder = await _dbContext.MenuGroups
            .AsNoTracking()
            .Select(x => (int?)x.DisplayOrder)
            .MaxAsync() ?? 0;

        return View(new MenuGroupFormViewModel
        {
            IsPublished = true,
            DisplayOrder = nextDisplayOrder + 1
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuGroupFormViewModel model, CancellationToken cancellationToken)
    {
        NormalizeForm(model);
        ValidateMenuGroupCoverImage(model.CoverImageFile);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var requestedSlug = NormalizeSlugInput(model.Slug);
        if (!string.IsNullOrWhiteSpace(requestedSlug) &&
            await _dbContext.MenuGroups.AsNoTracking().AnyAsync(x => x.Slug == requestedSlug, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Slug), "Slug này đã tồn tại.");
            return View(model);
        }

        var slug = await BuildUniqueSlugAsync(model.Slug, model.Name, cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Không thể tạo slug hợp lệ từ dữ liệu đã nhập.");
            return View(model);
        }

        string? uploadedCoverImageUrl = null;
        if (model.CoverImageFile is not null && model.CoverImageFile.Length > 0)
        {
            try
            {
                uploadedCoverImageUrl = await _imageStorageService.UploadAsync(
                    model.CoverImageFile,
                    ImageCategory.MenuGroupCovers,
                    cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                ModelState.AddModelError(nameof(model.CoverImageFile), exception.Message);
                return View(model);
            }
            catch
            {
                ModelState.AddModelError(nameof(model.CoverImageFile), "Không thể tải ảnh lên. Vui lòng thử lại.");
                return View(model);
            }
        }

        var now = DateTimeOffset.UtcNow;
        var group = new MenuGroup
        {
            Name = model.Name,
            Slug = slug,
            ShortDescription = model.ShortDescription,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description,
            CoverImageUrl = !string.IsNullOrWhiteSpace(uploadedCoverImageUrl)
                ? uploadedCoverImageUrl
                : (string.IsNullOrWhiteSpace(model.CoverImageUrl) ? null : model.CoverImageUrl),
            DisplayOrder = model.DisplayOrder,
            IsPublished = model.IsPublished,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.MenuGroups.Add(group);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _imageStorageService.DeleteAsync(uploadedCoverImageUrl, ImageCategory.MenuGroupCovers, cancellationToken);
            throw;
        }

        SetSuccessMessage("Nhóm thực đơn đã được tạo.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var group = await _dbContext.MenuGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (group is null)
        {
            return NotFound();
        }

        return View(new MenuGroupFormViewModel
        {
            Id = group.Id,
            Name = group.Name,
            Slug = group.Slug,
            ShortDescription = group.ShortDescription,
            Description = group.Description,
            CoverImageUrl = group.CoverImageUrl,
            DisplayOrder = group.DisplayOrder,
            IsPublished = group.IsPublished
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, MenuGroupFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        NormalizeForm(model);
        ValidateMenuGroupCoverImage(model.CoverImageFile);

        var group = await _dbContext.MenuGroups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (group is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var requestedSlug = NormalizeSlugInput(model.Slug);
        if (!string.IsNullOrWhiteSpace(requestedSlug) &&
            await _dbContext.MenuGroups.AsNoTracking().AnyAsync(x => x.Slug == requestedSlug && x.Id != group.Id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Slug), "Slug này đã tồn tại.");
            return View(model);
        }

        var slug = await BuildUniqueSlugAsync(model.Slug, model.Name, group.Id, cancellationToken);
        if (string.IsNullOrWhiteSpace(slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Không thể tạo slug hợp lệ từ dữ liệu đã nhập.");
            return View(model);
        }

        var previousCoverImageUrl = group.CoverImageUrl;
        string? uploadedCoverImageUrl = null;
        if (model.CoverImageFile is not null && model.CoverImageFile.Length > 0)
        {
            try
            {
                uploadedCoverImageUrl = await _imageStorageService.UploadAsync(
                    model.CoverImageFile,
                    ImageCategory.MenuGroupCovers,
                    cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                ModelState.AddModelError(nameof(model.CoverImageFile), exception.Message);
                return View(model);
            }
            catch
            {
                ModelState.AddModelError(nameof(model.CoverImageFile), "Không thể tải ảnh lên. Vui lòng thử lại.");
                return View(model);
            }
        }

        group.Name = model.Name;
        group.Slug = slug;
        group.ShortDescription = model.ShortDescription;
        group.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description;
        group.CoverImageUrl = !string.IsNullOrWhiteSpace(uploadedCoverImageUrl)
            ? uploadedCoverImageUrl
            : (string.IsNullOrWhiteSpace(model.CoverImageUrl) ? null : model.CoverImageUrl);
        group.DisplayOrder = model.DisplayOrder;
        group.IsPublished = model.IsPublished;
        group.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _imageStorageService.DeleteAsync(uploadedCoverImageUrl, ImageCategory.MenuGroupCovers, cancellationToken);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(uploadedCoverImageUrl))
        {
            await _imageStorageService.DeleteAsync(previousCoverImageUrl, ImageCategory.MenuGroupCovers, cancellationToken);
        }

        SetSuccessMessage("Nhóm thực đơn đã được cập nhật.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, string? q, string? status)
    {
        var group = await _dbContext.MenuGroups.FirstOrDefaultAsync(x => x.Id == id);
        if (group is null)
        {
            return NotFound();
        }

        group.IsPublished = false;
        group.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        SetSuccessMessage("Nhóm thực đơn đã được ẩn khỏi trạng thái công khai.");
        return RedirectToAction(nameof(Index), BuildIndexRouteValues(q, status));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PermanentDelete(Guid id, string? q, string? status, CancellationToken cancellationToken)
    {
        var group = await _dbContext.MenuGroups
            .Include(x => x.PageImages)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (group is null)
        {
            return NotFound();
        }

        if (group.IsPublished)
        {
            SetErrorMessage("Không thể xóa vĩnh viễn nhóm thực đơn đang hiển thị. Hãy ẩn nhóm trước, sau đó thực hiện xóa vĩnh viễn.");
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(q, status));
        }

        var groupName = group.Name;
        var coverImageUrl = group.CoverImageUrl;
        var pageCount = group.PageImages.Count;
        var pageImageUrls = group.PageImages
            .Select(x => x.ImageUrl)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _dbContext.MenuGroups.Remove(group);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Permanent delete failed for menu group {MenuGroupId}.", id);
            SetErrorMessage("Không thể xóa vĩnh viễn nhóm thực đơn. Vui lòng thử lại hoặc kiểm tra dữ liệu liên quan.");
            return RedirectToAction(nameof(Index), BuildIndexRouteValues(q, status));
        }

        if (!string.IsNullOrWhiteSpace(coverImageUrl))
        {
            await _imageStorageService.DeleteAsync(coverImageUrl, ImageCategory.MenuGroupCovers, CancellationToken.None);
        }

        foreach (var pageImageUrl in pageImageUrls)
        {
            await _imageStorageService.DeleteAsync(pageImageUrl, ImageCategory.MenuPages, CancellationToken.None);
        }

        SetSuccessMessage($"Nhóm thực đơn '{groupName}' và {pageCount} trang menu liên quan đã được xóa vĩnh viễn.");
        return RedirectToAction(nameof(Index), BuildIndexRouteValues(q, status));
    }

    [HttpGet("~/Admin/MenuGroup/Images/{menuGroupId:guid}", Name = "AdminMenuGroupImages")]
    public async Task<IActionResult> Images(Guid menuGroupId)
    {
        var model = await BuildImagesViewModelAsync(menuGroupId);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("~/Admin/MenuGroup/Images/{menuGroupId:guid}/Upload", Name = "AdminMenuGroupImagesUpload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(Guid menuGroupId, MenuPageImageUploadViewModel model)
    {
        var group = await _dbContext.MenuGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == menuGroupId);

        if (group is null)
        {
            return NotFound();
        }

        if (!ImageStoragePathUtilities.TryNormalizeStorageScopeSegment(group.Slug, out var menuPageStorageScope))
        {
            _logger.LogWarning(
                "Menu page upload blocked because menu group {MenuGroupId} has an invalid persisted slug.",
                menuGroupId);
            SetErrorMessage("Slug nhóm thực đơn không hợp lệ. Vui lòng cập nhật slug trước khi tải ảnh.");
            return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
        }

        var files = model.ImageFiles
            .Where(x => x is not null)
            .ToList();

        if (files.Count == 0)
        {
            SetErrorMessage("Vui lòng chọn ít nhất một ảnh hợp lệ để tải lên.");
            return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
        }

        if (files.Count > MaxMenuPageUploadFileCount)
        {
            SetErrorMessage("Mỗi lần tải lên tối đa 10 ảnh thực đơn.");
            return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
        }

        var invalidFile = files.FirstOrDefault(x => !ValidateMenuPageImageFile(x));
        if (invalidFile is not null)
        {
            SetErrorMessage("Ảnh tải lên phải là jpg, jpeg, png hoặc webp và không vượt quá 50MB mỗi file.");
            return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
        }

        var currentMaxDisplayOrder = await _dbContext.MenuPageImages
            .AsNoTracking()
            .Where(x => x.MenuGroupId == menuGroupId)
            .Select(x => (int?)x.DisplayOrder)
            .MaxAsync() ?? 0;

        var existingCount = await _dbContext.MenuPageImages
            .AsNoTracking()
            .CountAsync(x => x.MenuGroupId == menuGroupId);

        var nextDisplayOrder = currentMaxDisplayOrder + 1;
        var nextPageNumber = existingCount + 1;
        var now = DateTimeOffset.UtcNow;
        var imageEntities = new List<MenuPageImage>(files.Count);
        var uploadedImageUrls = new List<string>(files.Count);

        foreach (var file in files)
        {
            string imageUrl;

            try
            {
                imageUrl = await _imageStorageService.UploadAsync(
                    file,
                    ImageCategory.MenuPages,
                    menuPageStorageScope,
                    HttpContext.RequestAborted);
            }
            catch (InvalidOperationException exception)
            {
                SetErrorMessage(exception.Message);
                foreach (var uploadedImageUrl in uploadedImageUrls)
                {
                    await _imageStorageService.DeleteAsync(uploadedImageUrl, ImageCategory.MenuPages, HttpContext.RequestAborted);
                }

                return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
            }
            catch
            {
                SetErrorMessage("Không thể lưu một hoặc nhiều ảnh đã chọn. Vui lòng thử lại.");
                foreach (var uploadedImageUrl in uploadedImageUrls)
                {
                    await _imageStorageService.DeleteAsync(uploadedImageUrl, ImageCategory.MenuPages, HttpContext.RequestAborted);
                }

                return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
            }

            uploadedImageUrls.Add(imageUrl);
            imageEntities.Add(new MenuPageImage
            {
                MenuGroupId = menuGroupId,
                ImageUrl = imageUrl,
                AltText = BuildMenuPageAltText(group.Name, model.AltText, files.Count, nextPageNumber),
                DisplayOrder = nextDisplayOrder,
                IsPublished = model.IsPublished,
                CreatedAt = now,
                UpdatedAt = now
            });

            nextDisplayOrder += 1;
            nextPageNumber++;
        }

        _dbContext.MenuPageImages.AddRange(imageEntities);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            foreach (var uploadedImageUrl in uploadedImageUrls)
            {
                await _imageStorageService.DeleteAsync(uploadedImageUrl, ImageCategory.MenuPages, HttpContext.RequestAborted);
            }

            throw;
        }

        SetSuccessMessage($"Đã tải lên {imageEntities.Count} ảnh thực đơn.");
        return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
    }

    [HttpPost("~/Admin/MenuGroup/Images/{menuGroupId:guid}/Save", Name = "AdminMenuGroupImagesUpdate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromRoute] Guid menuGroupId, MenuPageImageUpdateViewModel model)
    {
        if (model.MenuGroupId != menuGroupId)
        {
            return BadRequest();
        }

        var image = await _dbContext.MenuPageImages
            .Include(x => x.MenuGroup)
            .FirstOrDefaultAsync(x => x.Id == model.Id && x.MenuGroupId == menuGroupId);

        if (image is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            SetErrorMessage("Dữ liệu cập nhật ảnh thực đơn không hợp lệ.");
            return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
        }

        image.AltText = string.IsNullOrWhiteSpace(model.AltText)
            ? BuildMenuPageAltText(image.MenuGroup.Name, null, 1, null)
            : model.AltText.Trim();
        image.DisplayOrder = model.DisplayOrder;
        image.IsPublished = model.IsPublished;
        image.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        SetSuccessMessage("Ảnh thực đơn đã được cập nhật.");
        return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
    }

    [HttpGet("~/Admin/MenuGroup/Images/{menuGroupId:guid}/Update")]
    public IActionResult UpdateGet(Guid menuGroupId)
    {
        return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
    }

    [HttpPost("~/Admin/MenuGroup/Images/{menuGroupId:guid}/Delete/{imageId:guid}", Name = "AdminMenuGroupImagesDelete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(Guid menuGroupId, Guid imageId)
    {
        var image = await _dbContext.MenuPageImages
            .FirstOrDefaultAsync(x => x.Id == imageId && x.MenuGroupId == menuGroupId);

        if (image is null)
        {
            return NotFound();
        }

        var imageUrl = image.ImageUrl;
        _dbContext.MenuPageImages.Remove(image);
        await _dbContext.SaveChangesAsync();

        await _imageStorageService.DeleteAsync(imageUrl, ImageCategory.MenuPages, HttpContext.RequestAborted);

        SetSuccessMessage("Ảnh thực đơn đã được xóa.");
        return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
    }

    [HttpPost("~/Admin/MenuGroup/Images/{menuGroupId:guid}/BulkDelete", Name = "AdminMenuGroupImagesBulkDelete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDelete(
        [FromRoute] Guid menuGroupId,
        [FromForm] List<Guid> selectedImageIds)
    {
        var normalizedSelectedImageIds = selectedImageIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (normalizedSelectedImageIds.Count == 0)
        {
            SetErrorMessage("Vui lòng chọn ít nhất một ảnh để xóa.");
            return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
        }

        var images = await _dbContext.MenuPageImages
            .Where(x => x.MenuGroupId == menuGroupId && normalizedSelectedImageIds.Contains(x.Id))
            .ToListAsync();

        if (images.Count == 0)
        {
            SetErrorMessage("Không tìm thấy ảnh hợp lệ để xóa.");
            return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
        }

        var imageUrls = images
            .Select(x => x.ImageUrl)
            .ToList();

        _dbContext.MenuPageImages.RemoveRange(images);
        await _dbContext.SaveChangesAsync();

        foreach (var imageUrl in imageUrls)
        {
            await _imageStorageService.DeleteAsync(imageUrl, ImageCategory.MenuPages, HttpContext.RequestAborted);
        }

        SetSuccessMessage($"Đã xóa {images.Count} ảnh thực đơn đã chọn.");
        return RedirectToRoute("AdminMenuGroupImages", new { menuGroupId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublished(Guid id, string? q, string? status)
    {
        var group = await _dbContext.MenuGroups.FirstOrDefaultAsync(x => x.Id == id);
        if (group is null)
        {
            return NotFound();
        }

        group.IsPublished = !group.IsPublished;
        group.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        SetSuccessMessage(group.IsPublished
            ? "Nhóm thực đơn đã được hiển thị công khai."
            : "Nhóm thực đơn đã được chuyển sang trạng thái ẩn.");

        return RedirectToAction(nameof(Index), BuildIndexRouteValues(q, status));
    }

    private static void NormalizeForm(MenuGroupFormViewModel model)
    {
        model.Name = model.Name.Trim();
        model.Slug = NormalizeSlugInput(model.Slug);
        model.ShortDescription = model.ShortDescription.Trim();
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        model.CoverImageUrl = string.IsNullOrWhiteSpace(model.CoverImageUrl) ? null : model.CoverImageUrl.Trim();
    }

    private async Task<string> BuildUniqueSlugAsync(
        string? requestedSlug,
        string fallbackName,
        Guid? currentId = null,
        CancellationToken cancellationToken = default)
    {
        var baseSlug = NormalizeSlugInput(string.IsNullOrWhiteSpace(requestedSlug) ? fallbackName : requestedSlug);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            return string.Empty;
        }

        var slug = baseSlug;
        var suffix = 2;

        while (await _dbContext.MenuGroups
                   .AsNoTracking()
                   .AnyAsync(x => x.Slug == slug && (!currentId.HasValue || x.Id != currentId.Value), cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalizedStatus = status.Trim().ToLowerInvariant();
        return normalizedStatus is PublishedStatus or HiddenStatus
            ? normalizedStatus
            : null;
    }

    private static object BuildIndexRouteValues(string? q, string? status)
    {
        return new
        {
            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            status = NormalizeStatus(status)
        };
    }

    private static IReadOnlyList<SelectListItem> BuildStatusOptions()
    {
        return
        [
            new SelectListItem { Value = string.Empty, Text = "Tất cả trạng thái" },
            new SelectListItem { Value = PublishedStatus, Text = "Đang hiển thị" },
            new SelectListItem { Value = HiddenStatus, Text = "Đã ẩn" }
        ];
    }

    private async Task<MenuGroupImagesViewModel?> BuildImagesViewModelAsync(Guid id)
    {
        var group = await _dbContext.MenuGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (group is null)
        {
            return null;
        }

        var images = await _dbContext.MenuPageImages
            .AsNoTracking()
            .Where(x => x.MenuGroupId == id)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.CreatedAt)
            .Select(x => new MenuPageImageListItemViewModel
            {
                Id = x.Id,
                MenuGroupId = x.MenuGroupId,
                ImageUrl = x.ImageUrl,
                AltText = x.AltText,
                DisplayOrder = x.DisplayOrder,
                IsPublished = x.IsPublished,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new MenuGroupImagesViewModel
        {
            MenuGroupId = group.Id,
            MenuGroupName = group.Name,
            MenuGroupSlug = group.Slug,
            Images = images,
            Upload = new MenuPageImageUploadViewModel
            {
                MenuGroupId = group.Id,
                IsPublished = true
            }
        };
    }

    private static bool ValidateMenuPageImageFile(IFormFile file)
    {
        if (file.Length <= 0 || file.Length > MaxMenuPageFileSize)
        {
            return false;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(file.ContentType) && AllowedContentTypes.Contains(file.ContentType);
    }

    private void ValidateMenuGroupCoverImage(IFormFile? file)
    {
        if (file is null)
        {
            return;
        }

        if (file.Length <= 0)
        {
            ModelState.AddModelError(nameof(MenuGroupFormViewModel.CoverImageFile), "Vui lòng chọn một ảnh bìa hợp lệ.");
            return;
        }

        if (file.Length > MaxMenuGroupCoverFileSize)
        {
            ModelState.AddModelError(nameof(MenuGroupFormViewModel.CoverImageFile), "Ảnh bìa tải lên không được vượt quá 10MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(MenuGroupFormViewModel.CoverImageFile), "Chỉ chấp nhận file .jpg, .jpeg, .png hoặc .webp.");
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType))
        {
            ModelState.AddModelError(nameof(MenuGroupFormViewModel.CoverImageFile), "Định dạng ảnh tải lên không hợp lệ.");
        }
    }

    private static string BuildMenuPageAltText(string groupName, string? commonAltText, int fileCount, int? pageNumber)
    {
        var prefix = string.IsNullOrWhiteSpace(commonAltText)
            ? $"{groupName} - Trang thực đơn"
            : commonAltText.Trim();

        if (pageNumber.HasValue && (fileCount > 1 || string.IsNullOrWhiteSpace(commonAltText)))
        {
            return $"{prefix} - Trang {pageNumber.Value}";
        }

        return prefix;
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

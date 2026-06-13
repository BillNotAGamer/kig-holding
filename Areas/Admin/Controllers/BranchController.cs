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

public class BranchController : AdminBaseController
{
    private readonly AppDbContext _dbContext;
    private readonly IBranchService _branchService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<BranchController> _logger;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private const int MaxFileSize = 2 * 1024 * 1024;

    public BranchController(
        AppDbContext dbContext,
        IBranchService branchService,
        IImageStorageService imageStorageService,
        ILogger<BranchController> logger)
    {
        _dbContext = dbContext;
        _branchService = branchService;
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var branches = await _dbContext.Branches
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return View(branches);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new BranchCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BranchCreateViewModel model)
    {
        await ValidateBranchModelAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var thumbnailUrl = string.Empty;
        if (model.ThumbnailFile is not null && model.ThumbnailFile.Length > 0)
        {
            try
            {
                thumbnailUrl = await _imageStorageService.UploadAsync(
                    model.ThumbnailFile,
                    ImageCategory.Branches,
                    HttpContext.RequestAborted);
            }
            catch
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Không thể tải ảnh lên. Vui lòng thử lại.");
                return View(model);
            }
        }

        var branch = new Branch
        {
            Name = model.Name.Trim(),
            Slug = await BuildUniqueSlugAsync(model.Slug, model.Name),
            Address = model.Address.Trim(),
            District = model.District.Trim(),
            City = model.City.Trim(),
            Hotline = model.Hotline.Trim(),
            Email = model.Email.Trim(),
            OpeningTime = model.OpeningTime,
            ClosingTime = model.ClosingTime,
            Capacity = model.Capacity,
            AreaSquareMeters = model.AreaSquareMeters,
            NumberOfFloors = model.NumberOfFloors,
            Description = model.Description.Trim(),
            GoogleMapUrl = model.GoogleMapUrl.Trim(),
            ThumbnailUrl = thumbnailUrl,
            IsActive = model.IsActive,
            DisplayOrder = model.DisplayOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Branches.Add(branch);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            await _imageStorageService.DeleteAsync(thumbnailUrl, ImageCategory.Branches, HttpContext.RequestAborted);
            throw;
        }

        _branchService.InvalidateActiveBranchesCache();

        return RedirectToAdminIndex();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var branch = await _dbContext.Branches.FindAsync(id);
        if (branch is null)
        {
            return NotFound();
        }

        var model = new BranchEditViewModel
        {
            Id = branch.Id,
            Name = branch.Name,
            Slug = branch.Slug,
            Address = branch.Address,
            District = branch.District,
            City = branch.City,
            Hotline = branch.Hotline,
            Email = branch.Email,
            OpeningTime = branch.OpeningTime,
            ClosingTime = branch.ClosingTime,
            Capacity = branch.Capacity,
            AreaSquareMeters = branch.AreaSquareMeters,
            NumberOfFloors = branch.NumberOfFloors,
            Description = branch.Description,
            GoogleMapUrl = branch.GoogleMapUrl,
            ExistingThumbnailUrl = branch.ThumbnailUrl,
            IsActive = branch.IsActive,
            DisplayOrder = branch.DisplayOrder
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, BranchEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var branch = await _dbContext.Branches.FindAsync(id);
        if (branch is null)
        {
            return NotFound();
        }

        await ValidateBranchModelAsync(model, branch.Id);

        if (!ModelState.IsValid)
        {
            model.ExistingThumbnailUrl = branch.ThumbnailUrl;
            return View(model);
        }

        var previousThumbnailUrl = branch.ThumbnailUrl;
        string? uploadedThumbnailUrl = null;
        if (model.ThumbnailFile is not null && model.ThumbnailFile.Length > 0)
        {
            try
            {
                uploadedThumbnailUrl = await _imageStorageService.UploadAsync(
                    model.ThumbnailFile,
                    ImageCategory.Branches,
                    HttpContext.RequestAborted);
            }
            catch
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Không thể tải ảnh lên. Vui lòng thử lại.");
                model.ExistingThumbnailUrl = branch.ThumbnailUrl;
                return View(model);
            }
        }

        branch.Name = model.Name.Trim();
        branch.Slug = await BuildUniqueSlugAsync(model.Slug, model.Name, branch.Id);
        branch.Address = model.Address.Trim();
        branch.District = model.District.Trim();
        branch.City = model.City.Trim();
        branch.Hotline = model.Hotline.Trim();
        branch.Email = model.Email.Trim();
        branch.OpeningTime = model.OpeningTime;
        branch.ClosingTime = model.ClosingTime;
        branch.Capacity = model.Capacity;
        branch.AreaSquareMeters = model.AreaSquareMeters;
        branch.NumberOfFloors = model.NumberOfFloors;
        branch.Description = model.Description.Trim();
        branch.GoogleMapUrl = model.GoogleMapUrl.Trim();
        if (!string.IsNullOrWhiteSpace(uploadedThumbnailUrl))
        {
            branch.ThumbnailUrl = uploadedThumbnailUrl;
        }

        branch.IsActive = model.IsActive;
        branch.DisplayOrder = model.DisplayOrder;
        branch.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            await _imageStorageService.DeleteAsync(uploadedThumbnailUrl, ImageCategory.Branches, HttpContext.RequestAborted);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(uploadedThumbnailUrl))
        {
            await _imageStorageService.DeleteAsync(previousThumbnailUrl, ImageCategory.Branches, HttpContext.RequestAborted);
        }

        _branchService.InvalidateActiveBranchesCache();

        return RedirectToAdminIndex();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var branch = await _dbContext.Branches.FindAsync(id);
        if (branch is null)
        {
            return NotFound();
        }

        branch.IsActive = false;
        branch.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        _branchService.InvalidateActiveBranchesCache();
        SetSuccessMessage("Chi nhánh đã được ẩn khỏi website công khai.");

        return RedirectToAdminIndex();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PermanentDelete(Guid id, CancellationToken cancellationToken)
    {
        var branch = await _dbContext.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (branch is null)
        {
            return NotFound();
        }

        var hasReservations = await _dbContext.Reservations
            .AsNoTracking()
            .AnyAsync(x => x.BranchId == id, cancellationToken);

        if (hasReservations)
        {
            SetErrorMessage("Không thể xóa vĩnh viễn chi nhánh vì đang có dữ liệu đặt bàn liên quan. Hãy sử dụng Xóa mềm để bảo toàn lịch sử vận hành.");
            return RedirectToAdminIndex();
        }

        var branchName = branch.Name;
        var thumbnailUrl = branch.ThumbnailUrl;

        _dbContext.Branches.Remove(branch);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Permanent delete failed for branch {BranchId}.", id);
            SetErrorMessage("Không thể xóa vĩnh viễn chi nhánh vì vẫn còn dữ liệu liên quan. Hãy sử dụng Xóa mềm.");
            return RedirectToAdminIndex();
        }

        _branchService.InvalidateActiveBranchesCache();

        if (!string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            await _imageStorageService.DeleteAsync(thumbnailUrl, ImageCategory.Branches, CancellationToken.None);
        }

        SetSuccessMessage($"Chi nhánh '{branchName}' đã được xóa vĩnh viễn.");

        return RedirectToAdminIndex();
    }

    private async Task ValidateBranchModelAsync(BranchCreateViewModel model, Guid? currentBranchId = null)
    {
        if (model.OpeningTime >= model.ClosingTime)
        {
            ModelState.AddModelError(nameof(model.ClosingTime), "Giờ đóng cửa phải sau giờ mở cửa.");
        }

        if (model.ThumbnailFile is not null && model.ThumbnailFile.Length > 0)
        {
            var extension = Path.GetExtension(model.ThumbnailFile.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Chỉ chấp nhận file .jpg, .jpeg, .png hoặc .webp.");
            }

            if (!string.IsNullOrWhiteSpace(model.ThumbnailFile.ContentType) &&
                !AllowedContentTypes.Contains(model.ThumbnailFile.ContentType))
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Định dạng ảnh tải lên không hợp lệ.");
            }

            if (model.ThumbnailFile.Length > MaxFileSize)
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Ảnh tải lên không được vượt quá 2MB.");
            }
        }

        var isManualSlug = !string.IsNullOrWhiteSpace(model.Slug);
        var normalizedSlug = NormalizeSlugInput(isManualSlug ? model.Slug : model.Name);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Không thể tạo slug hợp lệ. Vui lòng nhập lại tên hoặc slug.");
            return;
        }

        if (isManualSlug)
        {
            var slugExists = await _dbContext.Branches
                .AsNoTracking()
                .AnyAsync(x => x.Slug == normalizedSlug && (!currentBranchId.HasValue || x.Id != currentBranchId.Value));

            if (slugExists)
            {
                ModelState.AddModelError(nameof(model.Slug), "Slug này đã tồn tại. Vui lòng chọn slug khác.");
            }
        }
    }

    private async Task<string> BuildUniqueSlugAsync(string? requestedSlug, string fallbackName, Guid? currentBranchId = null)
    {
        var baseSlug = NormalizeSlugInput(string.IsNullOrWhiteSpace(requestedSlug) ? fallbackName : requestedSlug);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = Guid.NewGuid().ToString("N")[..8];
        }

        var slug = baseSlug;
        var suffix = 2;

        while (await _dbContext.Branches
                   .AsNoTracking()
                   .AnyAsync(x => x.Slug == slug && (!currentBranchId.HasValue || x.Id != currentBranchId.Value)))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private RedirectToActionResult RedirectToAdminIndex()
    {
        return RedirectToAction(
            nameof(Index),
            "Branch",
            new { area = "Admin" })!;
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
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
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
